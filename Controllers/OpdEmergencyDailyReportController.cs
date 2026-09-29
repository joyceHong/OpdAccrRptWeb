using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
[Route("medical-statistics/opd-emergency-daily")]
public sealed class OpdEmergencyDailyReportController(
    IM3OpdEmergencyDailyReportService service,
    IM3OpdEmergencyDailyReportRenderer renderer,
    IM3PatientAccessAuditWriter auditWriter,
    IReportCatalogService catalog,
    TimeProvider timeProvider,
    ILogger<OpdEmergencyDailyReportController> logger) : Controller
{
    private static readonly TimeZoneInfo TaipeiTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    [HttpGet("")]
    public IActionResult Index()
    {
        ReportIndexViewModel source = catalog.GetReportIndex();
        string yesterday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TaipeiTimeZone).DateTime)
            .AddDays(-1).ToString("yyyy-MM-dd");
        return View("~/Views/Report/Index.cshtml", new ReportIndexViewModel
        {
            Categories = source.Categories, DefaultStartDate = yesterday, DefaultEndDate = yesterday,
            C21RebuildEnabled = source.C21RebuildEnabled, C23RebuildEnabled = source.C23RebuildEnabled
        });
    }

    [HttpPost("query"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Query([FromBody] M3OpdEmergencyDailyReportRequest request,
        CancellationToken cancellationToken)
    {
        DateTimeOffset started = timeProvider.GetUtcNow(); string actor = Actor();
        try
        {
            M3OpdEmergencyDailyPagedResponse result = await service.QueryAsync(request, actor, cancellationToken);
            M3OpdEmergencyDailyReportSnapshot? snapshot = Snapshot(result.RunId, actor);
            await Audit(actor, request.ReportDate ?? snapshot?.ReportDate, started, snapshot, "Query", null,
                "Success", null, cancellationToken); NoStore(); return Ok(result);
        }
        catch (M3ReportRunNotFoundException exception)
        { await Audit(actor, request.ReportDate, started, null, "Query", null, "NotFound", "Snapshot", CancellationToken.None); return NotFound(exception.Message); }
        catch (ArgumentException exception)
        { await Audit(actor, request.ReportDate, started, null, "Query", null, "Invalid", "Validation", CancellationToken.None); return BadRequest(exception.Message); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "M3 query failed. CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            await Audit(actor, request.ReportDate, started, null, "Query", null, "Failed", "Generation", CancellationToken.None);
            return Failure("M3 報表查詢失敗");
        }
    }

    [HttpGet("preview")]
    public async Task<IActionResult> Preview([FromQuery] string runId, CancellationToken cancellationToken)
    {
        DateTimeOffset started = timeProvider.GetUtcNow(); string actor = Actor();
        try
        {
            if (!service.TryGetRun(runId, actor, out M3OpdEmergencyDailyReportSnapshot snapshot))
                throw new M3ReportRunNotFoundException();
            var model = new M3OpdEmergencyDailyPreviewViewModel("亞東紀念醫院門急診日報表",
                snapshot.ReportDate.ToString("yyyy-MM-dd"), M3OpdEmergencyDailyReportRenderer.Weekday(snapshot.ReportDate),
                TimeZoneInfo.ConvertTime(snapshot.GeneratedAt, TaipeiTimeZone).ToString("yyyy-MM-dd HH:mm:ss"),
                "OpdAccrRptWeb.M3", actor, snapshot.Rows, snapshot.NineKpis);
            await Audit(actor, snapshot.ReportDate, started, snapshot, "Preview", "html", "Success", null, cancellationToken);
            NoStore(); return View("~/Views/OpdEmergencyDailyReport/Preview.cshtml", model);
        }
        catch (M3ReportRunNotFoundException exception)
        { await Audit(actor, null, started, null, "Preview", "html", "NotFound", "Snapshot", CancellationToken.None); return NotFound(exception.Message); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "M3 preview failed. CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            await Audit(actor, null, started, null, "Preview", "html", "Failed", "Renderer", CancellationToken.None);
            return Failure("M3 報表預覽失敗");
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string runId, [FromQuery] string format,
        CancellationToken cancellationToken)
    {
        DateTimeOffset started = timeProvider.GetUtcNow(); string actor = Actor();
        string normalized = (format ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized != "xlsx")
        { await Audit(actor, null, started, null, "Export", normalized, "Invalid", "Validation", CancellationToken.None); return BadRequest("M3 匯出格式只允許 xlsx。"); }
        try
        {
            if (!service.TryGetRun(runId, actor, out M3OpdEmergencyDailyReportSnapshot snapshot))
                throw new M3ReportRunNotFoundException();
            M3RenderedFile file = renderer.RenderXlsx(snapshot, actor);
            await Audit(actor, snapshot.ReportDate, started, snapshot, "Export", normalized, "Success", null, cancellationToken);
            NoStore(); return File(file.Content, file.ContentType, file.FileName);
        }
        catch (M3ReportRunNotFoundException exception)
        { await Audit(actor, null, started, null, "Export", normalized, "NotFound", "Snapshot", CancellationToken.None); return NotFound(exception.Message); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "M3 export failed. CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            await Audit(actor, null, started, null, "Export", normalized, "Failed", "Renderer", CancellationToken.None);
            return Failure("M3 報表匯出失敗");
        }
    }

    private M3OpdEmergencyDailyReportSnapshot? Snapshot(string? runId, string actor) =>
        !string.IsNullOrWhiteSpace(runId) && service.TryGetRun(runId, actor, out M3OpdEmergencyDailyReportSnapshot snapshot) ? snapshot : null;
    private Task Audit(string actor, DateOnly? date, DateTimeOffset started, M3OpdEmergencyDailyReportSnapshot? snapshot,
        string operation, string? format, string outcome, string? failureStage, CancellationToken token) =>
        auditWriter.WriteAsync(new(actor, date, started, timeProvider.GetUtcNow(), snapshot?.Rows.Count ?? 0,
            snapshot?.NumericChecksum ?? 0, snapshot?.SourceWatermark, snapshot?.RunId, operation, format,
            outcome, failureStage, HttpContext.TraceIdentifier), token);
    private string Actor() => User.Identity?.Name ?? string.Empty;
    private void NoStore() { Response.Headers.CacheControl = "private, no-store"; Response.Headers.Pragma = "no-cache"; }
    private ObjectResult Failure(string title)
    { var details = new ProblemDetails { Status = 500, Title = $"{title}，請提供追蹤碼給系統管理人員。" }; details.Extensions["traceId"] = HttpContext.TraceIdentifier; return StatusCode(500, details); }
}
