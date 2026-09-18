using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
[Route("reports/c4")]
public sealed class C4MaterialReportController(IC4MaterialReportService reportService,
    IOrganizationUnitCodeService organizationUnitCodeService,
    IC4MaterialReportRenderer renderer, ILogger<C4MaterialReportController> logger) : Controller
{
    [HttpGet("")]
    [AllowAnonymous]
    public IActionResult Index() => Redirect("/Report/C4");

    [HttpPost("query")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Query([FromBody] C4MaterialReportRequest request,
        CancellationToken cancellationToken)
    {
        try { return Ok((await reportService.QueryAsync(request, cancellationToken)).Page); }
        catch (ArgumentException exception) { return BadRequest(exception.Message); }
    }

    [HttpGet("organization-units")]
    public async Task<IActionResult> SearchOrganizationUnits([FromQuery] string query,
        CancellationToken cancellationToken) => Ok(await organizationUnitCodeService.SearchAsync(
            query, includeSections: true, includePlaces: true, activePlaceOnly: true,
            limit: 20, cancellationToken));

    [HttpGet("organization-units/resolve")]
    public async Task<IActionResult> ResolveOrganizationUnit([FromQuery] string newCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(newCode)) return BadRequest("請輸入科別／部門新代碼。");
        try
        {
            OrganizationUnitMapping? mapping = await organizationUnitCodeService.ResolveLegacyCodeAsync(
                newCode.Trim().ToUpperInvariant(), activePlaceOnly: true, cancellationToken);
            return mapping is null
                ? BadRequest("查無對應的科別／部門舊代碼。")
                : Ok(mapping);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
    }

    [HttpPost("preview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview([FromBody] C4MaterialReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            C4MaterialPreviewViewModel? model = await reportService.CreatePreviewAsync(request,
                HttpContext?.User.Identity?.Name ?? "anonymous", cancellationToken);
            return model is null ? NotFound("查無此筆資料") : Ok(model);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "C4 preview failed. TraceId={TraceId}", HttpContext.TraceIdentifier);
            return Problem("無法建立 C4 預覽。");
        }
    }

    [HttpPost("export/pdf")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ExportPdf([FromForm] C4MaterialReportRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            C4MaterialPreviewViewModel? model = await reportService.CreatePreviewAsync(request,
                HttpContext?.User.Identity?.Name ?? "anonymous", cancellationToken);
            if (model is null) return NotFound("查無此筆資料");
            return File(renderer.RenderPdf(model), "application/pdf", "C4-material-consignment.pdf");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "C4 PDF failed. TraceId={TraceId}", HttpContext.TraceIdentifier);
            return Problem("無法匯出 C4 PDF。");
        }
    }
}
