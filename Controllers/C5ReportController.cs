using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
[Route("reports/c5")]
public sealed class C5ReportController(IC5ReportService reportService,
    IOrganizationUnitCodeService organizationUnitCodeService, IC5PatientAccessAuditWriter auditWriter,
    ILogger<C5ReportController> logger) : Controller
{
    [HttpGet("")]
    [AllowAnonymous]
    public IActionResult Index() => Redirect("/Report/C5");

    [HttpPost("query")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Query([FromBody] C5ReportRequest request, CancellationToken token)
    {
        try
        {
            var started = System.Diagnostics.Stopwatch.StartNew();
            var result = await reportService.QueryAsync(request, token);
            if (result.Request.DetailType == C5DetailType.PatientDetail)
                await auditWriter.WriteAsync(new(User.Identity?.Name ?? string.Empty, DateTimeOffset.Now,
                    result.Request.DataSource, result.Request.Start, result.Request.End,
                    result.Request.LegacySectionCode, result.Request.RoomNo, result.Request.ChargeCode,
                    result.Request.InsuranceIdentityCode, result.AllRows.Count, started.ElapsedMilliseconds,
                    result.QueryIds), token);
            return Ok(result.Page);
        }
        catch (ArgumentException exception) { return BadRequest(exception.Message); }
    }

    [HttpGet("organization-units")]
    public async Task<IActionResult> Search([FromQuery] string query, CancellationToken token) =>
        Ok(await organizationUnitCodeService.SearchAsync(query, true, true, true, 20, token));

    [HttpPost("preview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview([FromBody] C5ReportRequest request, CancellationToken token)
    {
        try
        {
            var started = System.Diagnostics.Stopwatch.StartNew();
            var result = await reportService.QueryAsync(request with { PageNumber = 1, PageSize = 50 }, token);
            if (result.Request.DetailType == C5DetailType.PatientDetail)
                await auditWriter.WriteAsync(new(User.Identity?.Name ?? string.Empty, DateTimeOffset.Now,
                    result.Request.DataSource, result.Request.Start, result.Request.End,
                    result.Request.LegacySectionCode, result.Request.RoomNo, result.Request.ChargeCode,
                    result.Request.InsuranceIdentityCode, result.AllRows.Count, started.ElapsedMilliseconds,
                    result.QueryIds), token);
            string sourceName = result.Request.DataSource == C5DataSource.Inpatient ? "住院" : "門急診";
            string detailName = result.Request.DetailType switch
            {
                C5DetailType.Daily => "每日表",
                C5DetailType.PatientDetail => "明細表",
                _ => "彙總表"
            };
            return result.AllRows.Count == 0 ? NotFound("查無此筆資料") : Ok(new
            {
                title = $"亞東紀念醫院{sourceName}批價數量{detailName}",
                reportName = result.Request.DetailType == C5DetailType.PatientDetail
                    ? $"{sourceName}各科批價查詢表" : $"{sourceName}各科批價{detailName}",
                startDate = result.Request.RocStartDate,
                endDate = result.Request.RocEndDate,
                generatedAt = $"{DateTime.Now.Year - 1911:000}/{DateTime.Now:MM/dd HH:mm:ss}",
                preparedBy = User.Identity?.Name ?? string.Empty,
                dataSource = result.Request.DataSource,
                encounterType = result.Request.EncounterType,
                detailType = result.Request.DetailType,
                rows = result.AllRows
            });
        }
        catch (ArgumentException exception) { return BadRequest(exception.Message); }
        catch (OracleException exception)
        {
            logger.LogError(exception, "C5 preview database query failed. TraceId={TraceId} ErrorNumber={ErrorNumber}",
                HttpContext.TraceIdentifier, exception.Number);
            return StatusCode(500, $"C5 預覽資料查詢失敗，請提供追蹤碼 {HttpContext.TraceIdentifier} 給系統管理員。");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "C5 preview failed. TraceId={TraceId}", HttpContext.TraceIdentifier);
            return StatusCode(500, $"C5 預覽建立失敗，請提供追蹤碼 {HttpContext.TraceIdentifier} 給系統管理員。");
        }
    }
}
