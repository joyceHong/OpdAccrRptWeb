using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniExcelLibs;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Controllers;

[Route("Report/C141")]
[Authorize]
public sealed class C141OperationsController(
    IC141NoticeHistoryRepository historyRepository,
    IC141RankingService rankingService,
    IC141RecalculationService recalculationService,
    ILogger<C141OperationsController> logger) : Controller
{
    [HttpPost("NoticeHistory/Query")]
    public IActionResult QueryNoticeHistory([FromBody] C141NoticeHistoryRequest request,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!TryDateRange(request, out DateOnly start, out DateOnly end))
            return Invalid("請輸入有效的通知起迄日期，且起日不得晚於迄日。");
        try
        {
            IReadOnlyList<C141NoticeHistoryRow> rows = historyRepository.Query(
                ToRoc(start), ToRoc(end), cancellationToken);
            return Ok(new { data = rows, totalCount = rows.Count });
        }
        catch (OracleException exception)
        {
            logger.LogError(exception, "C141 通知查詢失敗。TraceId: {TraceId}", HttpContext.TraceIdentifier);
            return DatabaseUnavailable();
        }
    }

    [HttpPost("NoticeHistory/Export")]
    public IActionResult ExportNoticeHistory([FromBody] C141NoticeHistoryRequest request,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!TryDateRange(request, out DateOnly start, out DateOnly end))
            return Invalid("請輸入有效的通知起迄日期，且起日不得晚於迄日。");
        try
        {
            IReadOnlyList<C141NoticeHistoryRow> rows = historyRepository.Query(
                ToRoc(start), ToRoc(end), cancellationToken);
            var output = rows.Select(row => new Dictionary<string, object?>
            {
                ["病歷號"] = row.MedicalRecordNumber,
                ["通知日期"] = row.NoticeDate,
                ["通知方式"] = row.NoticeMethod,
                ["繳款日期"] = row.PaymentDate,
                ["通知金額"] = row.NoticeAmount,
                ["現金金額"] = row.CashAmount,
                ["信用卡金額"] = row.CreditCardAmount
            }).ToList();
            if (output.Count == 0)
                output.Add(new Dictionary<string, object?>
                {
                    ["病歷號"] = null, ["通知日期"] = null, ["通知方式"] = null,
                    ["繳款日期"] = null, ["通知金額"] = null,
                    ["現金金額"] = null, ["信用卡金額"] = null
                });
            using var stream = new MemoryStream();
            MiniExcel.SaveAs(stream, output, sheetName: "繳款通知查詢");
            return File(stream.ToArray(), ReportExportService.ExcelContentType,
                $"C141-繳款通知查詢-{start:yyyyMMdd}-{end:yyyyMMdd}.xlsx");
        }
        catch (OracleException exception)
        {
            logger.LogError(exception, "C141 通知查詢匯出失敗。TraceId: {TraceId}", HttpContext.TraceIdentifier);
            return DatabaseUnavailable();
        }
    }

    [HttpPost("Recalculate")]
    public IActionResult Recalculate([FromBody] C141RecalculationRequest request,
        CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!DateOnly.TryParseExact(request.AccountingDate, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly accountingDate)
            || accountingDate < new DateOnly(1912, 1, 1))
            return Invalid("請輸入有效的 C141 帳務日。");
        try
        {
            DateOnly oracleToday = rankingService.GetOracleToday(cancellationToken);
            if (accountingDate > oracleToday)
                return Invalid("C141 帳務日不可晚於 Oracle 今日。");
            C141RecalculationResult result = recalculationService.Recalculate(
                ToRoc(accountingDate), ToRoc(oracleToday), cancellationToken);
            return Ok(result);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(499);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "C141 重新排行無法完成。TraceId: {TraceId}",
                HttpContext.TraceIdentifier);
            return StatusCode(500, new ProblemDetails
            {
                Title = "重新排行未完成，部分個案可能已更新；請查核後再執行。",
                Status = 500
            });
        }
    }

    private static bool TryDateRange(C141NoticeHistoryRequest request,
        out DateOnly start, out DateOnly end)
    {
        bool startValid = DateOnly.TryParseExact(request.StartDate, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out start);
        bool endValid = DateOnly.TryParseExact(request.EndDate, "yyyy-MM-dd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out end);
        return startValid && endValid && start >= new DateOnly(1912, 1, 1) && start <= end;
    }

    private static string ToRoc(DateOnly date) => date.ToDateTime(TimeOnly.MinValue).ToRocDateString();

    private BadRequestObjectResult Invalid(string title) =>
        BadRequest(new ProblemDetails { Title = title, Status = 400 });

    private ObjectResult DatabaseUnavailable() =>
        StatusCode(503, new ProblemDetails { Title = "C141 資料庫查詢失敗，請稍後再試。", Status = 503 });
}
