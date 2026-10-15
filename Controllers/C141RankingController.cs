using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Controllers;

[Route("Report/C141")]
[Authorize]
public sealed class C141RankingController(
    IC141RankingService service,
    IC141EntryDateTicketService entryDateTickets,
    ILogger<C141RankingController> logger) : Controller
{
    [HttpGet("Today")]
    public IActionResult Today(CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        try
        {
            DateOnly oracleToday = service.GetOracleToday(cancellationToken);
            string? entryDateTicket = User.Identity?.IsAuthenticated == true
                && !string.IsNullOrWhiteSpace(User.Identity.Name)
                    ? entryDateTickets.Create(oracleToday, User.Identity.Name)
                    : null;
            return Ok(new
            {
                accountingDate = oracleToday.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                entryDateTicket
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(499);
        }
        catch (OracleException exception)
        {
            LogDatabaseFailure(exception, "取得 Oracle 日期");
            return DatabaseUnavailable();
        }
    }

    [HttpPost("Query")]
    public IActionResult Query([FromBody] C141QueryRequest request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (!DateOnly.TryParseExact(
                request.AccountingDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateOnly accountingDate))
            return Invalid("請輸入有效的 C141 帳務日。");

        if (accountingDate < new DateOnly(1912, 1, 1))
            return Invalid("C141 帳務日不可早於 1912-01-01。");

        if (request.PageNumber <= 0)
            return Invalid("頁碼必須大於零。");

        if (request.PageSize is not (10 or 30 or 50))
            return Invalid("每頁筆數僅接受 10、30 或 50。");

        try
        {
            DateOnly oracleToday = service.GetOracleToday(cancellationToken);
            if (accountingDate > oracleToday)
                return Invalid("C141 帳務日不可晚於 Oracle 今日。");

            var search = new C141RankingSearch(
                request.MedicalRecordNumber,
                request.BedNumber,
                request.MinimumNetAmount)
            {
                AccountingDateRoc = DateTimeExtensions.ToRocDateString(
                    accountingDate.ToDateTime(TimeOnly.MinValue)),
                OracleToday = oracleToday,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            int totalCount = service.GetCount(search, cancellationToken);
            int totalPages = totalCount == 0
                ? 0
                : (int)Math.Ceiling(totalCount / (double)request.PageSize);
            int pageNumber = totalPages == 0 ? 1 : Math.Min(request.PageNumber, totalPages);
            IReadOnlyList<C141RankingRow> page = service.GetPage(
                search,
                (pageNumber - 1) * request.PageSize,
                request.PageSize,
                cancellationToken);

            return Ok(new
            {
                columns = C141RankingColumns.Visible,
                data = page,
                totalCount,
                totalPages,
                pageNumber
            });
        }
        catch (ArgumentException exception)
        {
            return Invalid(exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(499);
        }
        catch (OracleException exception)
        {
            LogDatabaseFailure(exception, "查詢排行榜");
            return DatabaseUnavailable();
        }
    }

    private BadRequestObjectResult Invalid(string title) =>
        BadRequest(new ProblemDetails { Title = title, Status = StatusCodes.Status400BadRequest });

    private ObjectResult DatabaseUnavailable() =>
        StatusCode(StatusCodes.Status503ServiceUnavailable,
            new ProblemDetails { Title = "C141 資料庫查詢失敗，請稍後再試。", Status = StatusCodes.Status503ServiceUnavailable });

    private void LogDatabaseFailure(OracleException exception, string operation) =>
        logger.LogError(exception, "C141 {Operation}失敗。TraceId: {TraceId}", operation, HttpContext.TraceIdentifier);
}
