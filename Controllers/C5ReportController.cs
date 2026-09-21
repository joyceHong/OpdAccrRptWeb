using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
[Route("reports/c5")]
public sealed class C5ReportController(IC5ReportService reportService,
    IOrganizationUnitCodeService organizationUnitCodeService, IC5PatientAccessAuditWriter auditWriter) : Controller
{
    [HttpGet("")]
    [AllowAnonymous]
    public IActionResult Index() => Redirect("/Report/C5");

    [HttpPost("query")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Query([FromBody] C5ReportRequest request, CancellationToken token)
    {
        if (request.DetailType == C5DetailType.PatientDetail &&
            !(User.HasClaim("permission", "C5.PatientDetail") || User.IsInRole("C5.PatientDetail")))
            return Forbid();
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
        if (request.DetailType == C5DetailType.PatientDetail &&
            !(User.HasClaim("permission", "C5.PatientDetail") || User.IsInRole("C5.PatientDetail")))
            return Forbid();
        try
        {
            var result = await reportService.QueryAsync(request with { PageNumber = 1, PageSize = 50 }, token);
            return result.AllRows.Count == 0 ? NotFound("查無此筆資料") : Ok(new
            {
                title = "批價數量查詢表（新站版面）", detailType = result.Request.DetailType,
                rows = result.AllRows
            });
        }
        catch (ArgumentException exception) { return BadRequest(exception.Message); }
    }
}
