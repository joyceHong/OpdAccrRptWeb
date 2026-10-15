using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Controllers;

[Route("Report/C142")]
public sealed class C142DebtReportController(
    IC142DebtReportService service,
    IOrganizationUnitCodeService organizationUnitCodeService,
    IReportCatalogService reportCatalogService,
    ILogger<C142DebtReportController> logger) : Controller
{
    [AllowAnonymous]
    [HttpGet("")]
    public IActionResult Index()
    {
        ReportIndexViewModel catalog = reportCatalogService.GetReportIndex();
        ReportCategoryViewModel category = catalog.Categories
            .Select(item => new ReportCategoryViewModel
            {
                Key = item.Key,
                Name = item.Name,
                Groups = item.Groups.Select(group => new ReportGroupViewModel
                {
                    Name = group.Name,
                    Reports = group.Reports.Where(report => report.Code == "C142").ToList()
                }).Where(group => group.Reports.Count > 0).ToList()
            })
            .Single(item => item.Groups.Count > 0);
        return View("~/Views/Report/Index.cshtml", new ReportIndexViewModel
        {
            Categories = [category],
            DefaultStartDate = catalog.DefaultStartDate,
            DefaultEndDate = catalog.DefaultEndDate
        });
    }

    [AllowAnonymous]
    [HttpGet("SectionOptions")]
    public async Task<IActionResult> SectionOptions([FromQuery] string? query, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        if (string.IsNullOrWhiteSpace(query)) return Ok(Array.Empty<object>());
        try
        {
            return Ok(await organizationUnitCodeService.SearchAsync(query, includeSections: true,
                includePlaces: false, activePlaceOnly: false, limit: 20,
                cancellationToken: cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return StatusCode(499); }
        catch (OracleException exception)
        {
            logger.LogError(exception, "C142 科別候選查詢失敗。TraceId: {TraceId}", HttpContext.TraceIdentifier);
            return StatusCode(503, new ProblemDetails { Title = "無法載入科別清單，請稍後再試。", Status = 503 });
        }
    }

    [AllowAnonymous]
    [HttpPost("Query")]
    public async Task<IActionResult> Query([FromBody] C142Request request, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "private, no-store";
        try
        {
            if (!string.IsNullOrWhiteSpace(request.LegacySectionCode)
                && string.IsNullOrWhiteSpace(request.SectionCode))
                throw new ArgumentException("科別新舊代碼不完整，請重新選擇科別。");
            if (!string.IsNullOrWhiteSpace(request.SectionCode))
            {
                if (request.SectionCode.Trim().Length > 10)
                    throw new ArgumentException("科別新代碼不可超過 10 碼。");
                if (!string.IsNullOrWhiteSpace(request.LegacySectionCode)
                    && request.LegacySectionCode.Trim().Length > 10)
                    throw new ArgumentException("科別舊代碼不可超過 10 碼。");
                var mapping = string.IsNullOrWhiteSpace(request.LegacySectionCode)
                    ? await organizationUnitCodeService.ResolveLegacySectionCodeAsync(
                        request.SectionCode, cancellationToken)
                    : await organizationUnitCodeService.ResolveSectionMappingAsync(
                        request.SectionCode, request.LegacySectionCode, cancellationToken);
                if (mapping is null)
                    throw new ArgumentException("查無對應的科別代碼，或新舊碼不一致。");
                request = request with { SectionCode = mapping.LegacyCode, LegacySectionCode = null };
            }
            return Ok(service.Query(request, Owner, cancellationToken));
        }
        catch (ArgumentException exception) { return BadRequest(new ProblemDetails { Title = exception.Message, Status = 400 }); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { return StatusCode(499); }
        catch (OracleException exception)
        {
            logger.LogError(exception, "C142 查詢失敗。TraceId: {TraceId}", HttpContext.TraceIdentifier);
            return StatusCode(503, new ProblemDetails { Title = "資料庫查詢失敗，請稍後再試。", Status = 503 });
        }
    }

    [AllowAnonymous]
    [HttpPost("Export")]
    public IActionResult Export([FromBody] C142ExportRequest request)
    {
        Response.Headers.CacheControl = "private, no-store";
        try
        {
            byte[] workbook = service.Export(request, Owner, HttpContext.RequestAborted);
            return File(workbook, ReportExportService.ExcelContentType, $"C142-{request.Mode}.xlsx");
        }
        catch (ArgumentException exception) { return BadRequest(new ProblemDetails { Title = exception.Message, Status = 400 }); }
        catch (OperationCanceledException) { return StatusCode(499); }
        catch (OracleException exception)
        {
            logger.LogError(exception, "C142 Excel 匯出失敗。TraceId: {TraceId}", HttpContext.TraceIdentifier);
            return StatusCode(503, new ProblemDetails { Title = "資料庫查詢失敗，請稍後再試。", Status = 503 });
        }
    }

    private string Owner => User.Identity?.Name ?? $"anonymous:{HttpContext.Connection.RemoteIpAddress}";
}
