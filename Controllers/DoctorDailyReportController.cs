using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
[Route("medical-statistics/doctor-daily")]
public sealed class DoctorDailyReportController(
    IM1DoctorDailyReportService service,
    IM1DoctorDailyReportRenderer renderer,
    IM1PatientAccessAuditWriter auditWriter,
    IReportCatalogService catalog,
    TimeProvider timeProvider,
    ILogger<DoctorDailyReportController> logger) : Controller
{
    private static readonly TimeZoneInfo TaipeiTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Asia/Taipei");

    [HttpGet("")]
    public IActionResult Index()
    {
        ReportIndexViewModel source = catalog.GetReportIndex();
        DateOnly yesterday = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(
            timeProvider.GetUtcNow(), TaipeiTimeZone).DateTime).AddDays(-1);
        return View("~/Views/Report/Index.cshtml", new ReportIndexViewModel
        {
            Categories = source.Categories,
            DefaultStartDate = yesterday.ToString("yyyy-MM-dd"),
            DefaultEndDate = yesterday.ToString("yyyy-MM-dd"),
            C21RebuildEnabled = source.C21RebuildEnabled,
            C23RebuildEnabled = source.C23RebuildEnabled
        });
    }

    [HttpPost("query"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Query(
        [FromBody] M1DoctorDailyReportRequest request,
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAt = timeProvider.GetUtcNow();
        string actor = Actor();
        try
        {
            M1DoctorDailyPagedResponse result = await service.QueryAsync(
                request, actor, cancellationToken);
            await AuditAsync(actor, request.ReportDate, startedAt, result.TotalCount, result.RunId,
                "Query", null, "Success", cancellationToken);
            NoStore();
            return Ok(result);
        }
        catch (M1FutureDateConfirmationRequiredException exception)
        {
            await AuditAsync(actor, request.ReportDate, startedAt, 0, request.RunId,
                "Query", null, "ConfirmationRequired", CancellationToken.None);
            return ConfirmationRequired(exception);
        }
        catch (M1ReportRunNotFoundException exception)
        {
            await AuditAsync(actor, request.ReportDate, startedAt, 0, request.RunId,
                "Query", null, "NotFound", CancellationToken.None);
            return NotFound(exception.Message);
        }
        catch (ArgumentException exception)
        {
            await AuditAsync(actor, request.ReportDate, startedAt, 0, request.RunId,
                "Query", null, "Invalid", CancellationToken.None);
            return BadRequest(exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "M1 query failed. CorrelationId={CorrelationId}",
                HttpContext.TraceIdentifier);
            await AuditAsync(actor, request.ReportDate, startedAt, 0, request.RunId,
                "Query", null, "Failed", CancellationToken.None);
            return Failure("M1 報表查詢失敗");
        }
    }

    [HttpPost("preview"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview(
        [FromForm] M1DoctorDailyReportRequest request,
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAt = timeProvider.GetUtcNow();
        string actor = Actor();
        try
        {
            M1DoctorDailyReportSnapshot? snapshot;
            if (!string.IsNullOrWhiteSpace(request.RunId))
            {
                if (!service.TryGetRun(request.RunId, actor, out snapshot!))
                    throw new M1ReportRunNotFoundException();
            }
            else
            {
                snapshot = await service.GenerateAsync(request, actor, cancellationToken);
            }
            if (snapshot is null)
            {
                await AuditAsync(actor, request.ReportDate, startedAt, 0, null,
                    "Preview", "html", "Empty", cancellationToken);
                return NotFound("查無任何資料");
            }

            M1DoctorDailyPreviewViewModel model = ToPreview(snapshot, actor);
            await AuditAsync(actor, snapshot.ReportDate, startedAt, snapshot.Rows.Count,
                snapshot.RunId, "Preview", "html", "Success", cancellationToken);
            NoStore();
            return View("~/Views/DoctorDailyReport/Preview.cshtml", model);
        }
        catch (M1FutureDateConfirmationRequiredException exception)
        {
            await AuditAsync(actor, request.ReportDate, startedAt, 0, request.RunId,
                "Preview", "html", "ConfirmationRequired", CancellationToken.None);
            return ConfirmationRequired(exception);
        }
        catch (M1ReportRunNotFoundException exception)
        {
            await AuditAsync(actor, request.ReportDate, startedAt, 0, request.RunId,
                "Preview", "html", "NotFound", CancellationToken.None);
            return NotFound(exception.Message);
        }
        catch (ArgumentException exception)
        {
            await AuditAsync(actor, request.ReportDate, startedAt, 0, request.RunId,
                "Preview", "html", "Invalid", CancellationToken.None);
            return BadRequest(exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "M1 preview failed. CorrelationId={CorrelationId}",
                HttpContext.TraceIdentifier);
            await AuditAsync(actor, request.ReportDate, startedAt, 0, request.RunId,
                "Preview", "html", "Failed", CancellationToken.None);
            return Failure("M1 報表預覽失敗");
        }
    }

    [HttpGet("export")]
    public async Task<IActionResult> Export(
        [FromQuery] string runId,
        [FromQuery] string format,
        CancellationToken cancellationToken)
    {
        DateTimeOffset startedAt = timeProvider.GetUtcNow();
        string actor = Actor();
        string normalizedFormat = (format ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedFormat != "xlsx")
        {
            await AuditAsync(actor, null, startedAt, 0, runId, "Export", normalizedFormat,
                "Invalid", CancellationToken.None);
            return BadRequest("M1 匯出格式只允許 xlsx。");
        }
        try
        {
            if (!service.TryGetRun(runId, actor, out M1DoctorDailyReportSnapshot snapshot))
                throw new M1ReportRunNotFoundException();
            if (snapshot.Rows.Count == 0) return NotFound("查無任何資料");

            M1RenderedFile file = renderer.RenderXlsx(snapshot, actor);
            await AuditAsync(actor, snapshot.ReportDate, startedAt, snapshot.Rows.Count,
                snapshot.RunId, "Export", normalizedFormat, "Success", cancellationToken);
            NoStore();
            return File(file.Content, file.ContentType, file.FileName);
        }
        catch (M1ReportRunNotFoundException exception)
        {
            await AuditAsync(actor, null, startedAt, 0, runId, "Export", normalizedFormat,
                "NotFound", CancellationToken.None);
            return NotFound(exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "M1 export failed. Format={Format} CorrelationId={CorrelationId}",
                normalizedFormat, HttpContext.TraceIdentifier);
            await AuditAsync(actor, null, startedAt, 0, runId, "Export", normalizedFormat,
                "Failed", CancellationToken.None);
            return Failure("M1 報表匯出失敗");
        }
    }

    private M1DoctorDailyPreviewViewModel ToPreview(
        M1DoctorDailyReportSnapshot snapshot,
        string actor)
    {
        DateTimeOffset localGeneratedAt = TimeZoneInfo.ConvertTime(snapshot.GeneratedAt, TaipeiTimeZone);
        return new("醫師看診人數日表", snapshot.ReportDate.ToString("yyyy-MM-dd"),
            localGeneratedAt.ToString("yyyy-MM-dd HH:mm:ss"), "OpdAccrRptWeb.M1", actor,
            snapshot.Rows);
    }

    private ObjectResult ConfirmationRequired(M1FutureDateConfirmationRequiredException exception)
    {
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = exception.Message
        };
        details.Extensions["code"] = M1FutureDateConfirmationRequiredException.ErrorCode;
        details.Extensions["reportDate"] = exception.ReportDate.ToString("yyyy-MM-dd");
        return Conflict(details);
    }

    private ObjectResult Failure(string title)
    {
        var details = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = $"{title}，請提供追蹤碼給系統管理人員。"
        };
        details.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(StatusCodes.Status500InternalServerError, details);
    }

    private Task AuditAsync(string actor, DateOnly? reportDate, DateTimeOffset startedAt,
        int rowCount, string? runId, string operation, string? format, string outcome,
        CancellationToken cancellationToken) => auditWriter.WriteAsync(new(
            actor, reportDate, startedAt, timeProvider.GetUtcNow(), rowCount, runId,
            operation, format, outcome, HttpContext.TraceIdentifier), cancellationToken);

    private string Actor() => User.Identity?.Name ?? string.Empty;

    private void NoStore()
    {
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.Pragma = "no-cache";
    }
}
