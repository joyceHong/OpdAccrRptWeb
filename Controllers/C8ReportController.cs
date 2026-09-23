using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
[Route("reports/c8")]
public sealed class C8ReportController(IC8ReportService service, IC8PatientAccessAuditWriter audit,
    TimeProvider timeProvider, ILogger<C8ReportController> logger) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => Redirect("/Report/C8");

    [HttpPost("query")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Query([FromBody] C8ReportRequest request, CancellationToken token)
    {
        var timer = Stopwatch.StartNew();
        string actor = User.Identity?.Name ?? string.Empty;
        try
        {
            C8ReportResult result = await service.QueryAsync(request, actor, token);
            await WriteAudit(result, actor, timer, "Success", token);
            Response.Headers.CacheControl = "private, no-store";
            Response.Headers.Pragma = "no-cache";
            return Ok(result.Page);
        }
        catch (ArgumentException exception) { return BadRequest(exception.Message); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "C8 query failed. CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            return Failure();
        }
    }

    [HttpPost("preview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview([FromForm] C8ReportRequest request, CancellationToken token)
    {
        var timer = Stopwatch.StartNew();
        string actor = User.Identity?.Name ?? string.Empty;
        try
        {
            C8ReportResult result = await service.QueryAsync(request with { PageNumber = 1 }, actor, token);
            if (result.AllRows.Count == 0) return NotFound("查無此筆資料");
            await WriteAudit(result, actor, timer, "Preview", token);
            DateTimeOffset now = timeProvider.GetLocalNow();
            return View("~/Views/C8/Preview.cshtml", new C8PreviewViewModel(actor,
                FormatRoc(result.Request.StartDate), FormatRoc(result.Request.EndDate),
                $"{now.Year - 1911:000}/{now:MM/dd  HH:mm:ss}", result.AllRows));
        }
        catch (ArgumentException exception) { return BadRequest(exception.Message); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "C8 preview failed. CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            return Failure();
        }
    }

    private Task WriteAudit(C8ReportResult result, string actor, Stopwatch timer, string outcome,
        CancellationToken token) => audit.WriteAsync(new(actor, result.Request.StartDate,
            result.Request.EndDate, result.AllRows.Count, timer.ElapsedMilliseconds,
            result.QueryId, outcome, HttpContext.TraceIdentifier), token);

    private ObjectResult Failure()
    {
        var details = new ProblemDetails { Status = 500,
            Title = "C8 報表處理失敗，請提供追蹤碼給系統管理人員。" };
        details.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(500, details);
    }
    private static string FormatRoc(DateOnly date) => $"{date.Year - 1911:000}/{date:MM/dd}";
}
