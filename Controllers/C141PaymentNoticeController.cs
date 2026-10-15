using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Controllers;

[Route("Report/C141/Notice")]
[Authorize]
public sealed class C141PaymentNoticeController(
    IC141PaymentNoticeService service,
    IC141EntryDateTicketService entryDateTickets,
    IC141RazorViewRenderer viewRenderer,
    ILogger<C141PaymentNoticeController> logger) : Controller
{
    private const int MaximumCasesPerRequest = 500;
    private const string DocumentViewPath = "~/Views/Report/_C141PaymentNoticeDocument.cshtml";
    private const string PageViewPath = "~/Views/Report/C141PaymentNoticePreview.cshtml";

    [HttpPost("Preview")]
    public async Task<IActionResult> Preview(
        [FromBody] C141NoticePreviewRequest request,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        string? loginName = User.Identity?.IsAuthenticated == true ? User.Identity.Name : null;
        if (string.IsNullOrWhiteSpace(loginName))
            return Unauthorized();
        string userId = loginName[(loginName.LastIndexOf('\\') + 1)..];
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();
        if (userId.Length > 10)
            return Invalid("登入者代碼超過通知紀錄欄位長度，無法產生通知單。");
        if (request.Cases is null || request.Cases.Count == 0)
            return Invalid("請至少選取一筆 C141 個案。");
        if (request.Cases.Count > MaximumCasesPerRequest)
            return Invalid($"每次最多可處理 {MaximumCasesPerRequest} 筆 C141 個案。");
        if (request.Cases.Any(key => !IsValid(key)))
            return Invalid("C141 個案四鍵格式無效。");
        if (request.Cases.Distinct().Count() != request.Cases.Count)
            return Invalid("C141 個案清單不得包含重複四鍵。");
        if (!entryDateTickets.TryRead(request.EntryDateTicket, loginName, out DateOnly entryDate))
            return Invalid("C141 頁面進入日期已失效，請重新載入報表後再試。");

        string entryDateRoc = entryDate.ToDateTime(TimeOnly.MinValue).ToRocDateString();
        var results = new List<C141NoticePreviewItem>(request.Cases.Count);
        foreach (C141AdmissionKey key in request.Cases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            C141PaymentNoticeDocument? document;
            try
            {
                document = service.Calculate(key, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogFailure(exception, "讀取通知資料");
                results.Add(Failure(key, "通知資料讀取或部分負擔計算失敗，未產生通知單。"));
                continue;
            }

            if (document is null)
            {
                results.Add(Failure(key, "找不到目前的 C141 個案，未產生通知單。"));
                continue;
            }
            if (!document.PartialPaymentCalculationAvailable)
            {
                results.Add(Failure(key, "健保身分 30 的部分負擔計算尚未完成，未產生通知單。"));
                continue;
            }

            try
            {
                service.RecordBeforeDocument(document, userId, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogFailure(exception, "寫入通知紀錄");
                results.Add(Failure(key, "通知日期或交易紀錄寫入失敗，未產生文件。"));
                continue;
            }

            string documentHtml;
            try
            {
                documentHtml = await viewRenderer.RenderToStringAsync(
                    ControllerContext,
                    DocumentViewPath,
                    document,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogFailure(exception, "產生通知文件");
                results.Add(Failure(key, "通知紀錄已寫入，但文件產生失敗；請先查核，不要自動重送。"));
                continue;
            }

            try
            {
                service.RecordAfterDocument(key, entryDateRoc, cancellationToken);
                results.Add(new(key, "成功", "通知文件已產生。", documentHtml));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                LogFailure(exception, "更新文件產生後日期");
                results.Add(new(key, "部分完成", "文件已產生，但通知日期覆寫失敗；請先查核，不要自動重送。", documentHtml));
            }
        }

        try
        {
            string pageHtml = await viewRenderer.RenderToStringAsync(
                ControllerContext,
                PageViewPath,
                new C141NoticePreviewPage(results),
                cancellationToken);
            return Content(pageHtml, "text/html; charset=utf-8");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            LogFailure(exception, "產生預覽頁");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ProblemDetails { Title = "C141 通知單頁面產生失敗，請先查核已寫入的逐案紀錄。", Status = 500 });
        }
    }

    private static C141NoticePreviewItem Failure(C141AdmissionKey key, string message) =>
        new(key, "失敗", message, null);

    private static bool IsValid(C141AdmissionKey key) =>
        key is not null
        && IsAsciiWithin(key.AdmissionDate, 7)
        && IsAsciiWithin(key.AdmissionTime, 1)
        && IsAsciiWithin(key.AdmissionRoom, 6)
        && key.AdmissionNumber >= 0m
        && decimal.Truncate(key.AdmissionNumber) == key.AdmissionNumber
        && key.AdmissionNumber <= 9999m;

    private static bool IsAsciiWithin(string? value, int maximumLength) =>
        !string.IsNullOrWhiteSpace(value)
        && value == value.Trim()
        && value.Length <= maximumLength
        && value.All(char.IsAscii);

    private BadRequestObjectResult Invalid(string title) =>
        BadRequest(new ProblemDetails { Title = title, Status = StatusCodes.Status400BadRequest });

    private void LogFailure(Exception exception, string operation) =>
        logger.LogError(
            "C141 {Operation}失敗。ExceptionType={ExceptionType}; TraceId={TraceId}",
            operation,
            exception.GetType().Name,
            HttpContext.TraceIdentifier);
}
