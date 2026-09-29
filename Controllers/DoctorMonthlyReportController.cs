using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
[Route("medical-statistics/doctor-monthly")]
public sealed class DoctorMonthlyReportController(
    IM2DoctorMonthlyReportService service,
    IM2DoctorMonthlyReportRenderer renderer,
    IM2PatientAccessAuditWriter auditWriter,
    IReportCatalogService catalog,
    TimeProvider timeProvider,
    ILogger<DoctorMonthlyReportController> logger) : Controller
{
    private static readonly TimeZoneInfo TaipeiTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    [HttpGet("")]
    public IActionResult Index()
    {
        ReportIndexViewModel source = catalog.GetReportIndex();
        DateTime localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TaipeiTimeZone).DateTime;
        string today = DateOnly.FromDateTime(localNow).ToString("yyyy-MM-dd");
        return View("~/Views/Report/Index.cshtml", new ReportIndexViewModel
        {
            Categories = source.Categories,
            DefaultStartDate = today,
            DefaultEndDate = today,
            C21RebuildEnabled = source.C21RebuildEnabled,
            C23RebuildEnabled = source.C23RebuildEnabled
        });
    }

    [HttpPost("query"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Query([FromBody] M2DoctorMonthlyReportRequest request,
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAt = timeProvider.GetUtcNow(); string actor = Actor();
        try
        {
            M2DoctorMonthlyPagedResponse result = await service.QueryAsync(request, actor, cancellationToken);
            long checksum = SnapshotChecksum(result.RunId, actor);
            await AuditAsync(actor, request, startedAt, result.TotalCount, checksum, result.RunId,
                "Query", null, "Success", cancellationToken);
            NoStore(); return Ok(result);
        }
        catch (M2ReportRunNotFoundException exception)
        {
            await AuditAsync(actor, request, startedAt, 0, 0, request.RunId, "Query", null,
                "NotFound", CancellationToken.None); return NotFound(exception.Message);
        }
        catch (ArgumentException exception)
        {
            await AuditAsync(actor, request, startedAt, 0, 0, request.RunId, "Query", null,
                "Invalid", CancellationToken.None); return BadRequest(exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "M2 query failed. CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            await AuditAsync(actor, request, startedAt, 0, 0, request.RunId, "Query", null,
                "Failed", CancellationToken.None); return Failure("M2 報表查詢失敗");
        }
    }

    [HttpPost("preview"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview([FromForm] string runId, CancellationToken cancellationToken)
    {
        DateTimeOffset startedAt = timeProvider.GetUtcNow(); string actor = Actor();
        var request = new M2DoctorMonthlyReportRequest(null, RunId: runId);
        try
        {
            if (!service.TryGetRun(runId, actor, out M2DoctorMonthlyReportSnapshot snapshot))
                throw new M2ReportRunNotFoundException();
            if (snapshot.Rows.Count == 0) return NotFound("查無任何資料");
            await AuditSnapshotAsync(snapshot, startedAt, "Preview", "html", "Success", cancellationToken);
            NoStore(); return View("~/Views/DoctorMonthlyReport/Preview.cshtml", ToPreview(snapshot, actor));
        }
        catch (M2ReportRunNotFoundException exception)
        {
            await AuditAsync(actor, request, startedAt, 0, 0, runId, "Preview", "html",
                "NotFound", CancellationToken.None); return NotFound(exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "M2 preview failed. CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            await AuditAsync(actor, request, startedAt, 0, 0, runId, "Preview", "html",
                "Failed", CancellationToken.None); return Failure("M2 報表預覽失敗");
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] string runId, [FromQuery] string format,
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAt = timeProvider.GetUtcNow(); string actor = Actor();
        string normalized = (format ?? string.Empty).Trim().ToLowerInvariant();
        if (normalized != "xlsx")
        {
            await AuditAsync(actor, new(null, RunId: runId), startedAt, 0, 0, runId,
                "Export", normalized, "Invalid", CancellationToken.None);
            return BadRequest("M2 匯出格式只允許 xlsx。");
        }
        try
        {
            if (!service.TryGetRun(runId, actor, out M2DoctorMonthlyReportSnapshot snapshot))
                throw new M2ReportRunNotFoundException();
            if (snapshot.Rows.Count == 0) return NotFound("查無任何資料");
            M2RenderedFile file = renderer.RenderXlsx(snapshot, actor);
            await AuditSnapshotAsync(snapshot, startedAt, "Export", normalized, "Success", cancellationToken);
            NoStore(); return File(file.Content, file.ContentType, file.FileName);
        }
        catch (M2ReportRunNotFoundException exception)
        {
            await AuditAsync(actor, new(null, RunId: runId), startedAt, 0, 0, runId,
                "Export", normalized, "NotFound", CancellationToken.None);
            return NotFound(exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception)
        {
            logger.LogError(exception, "M2 export failed. CorrelationId={CorrelationId}", HttpContext.TraceIdentifier);
            await AuditAsync(actor, new(null, RunId: runId), startedAt, 0, 0, runId,
                "Export", normalized, "Failed", CancellationToken.None);
            return Failure("M2 報表匯出失敗");
        }
    }

    private M2DoctorMonthlyPreviewViewModel ToPreview(M2DoctorMonthlyReportSnapshot snapshot, string actor) =>
        new("醫師看診人數月表", snapshot.ReportMonth.ToString("yyyy-MM"),
            snapshot.CalculationBasis.ToString(), snapshot.VisitScope.ToString(), snapshot.TimeSlot.ToString(),
            TimeZoneInfo.ConvertTime(snapshot.GeneratedAt, TaipeiTimeZone).ToString("yyyy-MM-dd HH:mm:ss"),
            "OpdAccrRptWeb.M2", actor, snapshot.Rows);

    private long SnapshotChecksum(string? runId, string actor) => !string.IsNullOrWhiteSpace(runId) &&
        service.TryGetRun(runId, actor, out M2DoctorMonthlyReportSnapshot snapshot) ? snapshot.NumericChecksum : 0;
    private Task AuditSnapshotAsync(M2DoctorMonthlyReportSnapshot snapshot, DateTimeOffset startedAt,
        string operation, string format, string outcome, CancellationToken cancellationToken) => auditWriter.WriteAsync(new(
            snapshot.Actor, snapshot.ReportMonth.ToString("yyyy-MM"), snapshot.CalculationBasis,
            snapshot.VisitScope, snapshot.TimeSlot, startedAt, timeProvider.GetUtcNow(), snapshot.Rows.Count,
            snapshot.NumericChecksum, snapshot.RunId, operation, format, outcome, HttpContext.TraceIdentifier), cancellationToken);
    private Task AuditAsync(string actor, M2DoctorMonthlyReportRequest request, DateTimeOffset startedAt,
        int rowCount, long checksum, string? runId, string operation, string? format, string outcome,
        CancellationToken cancellationToken) => auditWriter.WriteAsync(new(actor, request.ReportMonth,
            request.CalculationBasis, request.VisitScope, request.TimeSlot, startedAt, timeProvider.GetUtcNow(),
            rowCount, checksum, runId, operation, format, outcome, HttpContext.TraceIdentifier), cancellationToken);
    private ObjectResult Failure(string title)
    {
        var details = new ProblemDetails { Status = 500, Title = $"{title}，請提供追蹤碼給系統管理人員。" };
        details.Extensions["traceId"] = HttpContext.TraceIdentifier; return StatusCode(500, details);
    }
    private string Actor() => User.Identity?.Name ?? string.Empty;
    private void NoStore() { Response.Headers.CacheControl = "private, no-store"; Response.Headers.Pragma = "no-cache"; }
}
