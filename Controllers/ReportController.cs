using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Controllers;

public sealed class ReportController : Controller
{
    private readonly IReportCatalogService _reportCatalogService;
    private readonly IReportService _reportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        IReportCatalogService reportCatalogService,
        IReportService reportService,
        ILogger<ReportController> logger)
    {
        _reportCatalogService = reportCatalogService;
        _reportService = reportService;
        _logger = logger;
    }

    [HttpGet("/")]
    public IActionResult Root()
    {
        return Redirect("/Report");
    }

    [HttpGet("Report/{reportCode?}")]
    public IActionResult Index(string? reportCode = null)
    {
        ReportIndexViewModel viewModel = _reportCatalogService.GetReportIndex();
        return View(viewModel);
    }

    [HttpPost("Report/GetReportData")]
    public IActionResult GetReportData([FromBody] SearchReportCondition searchCondition)
    {
        if (searchCondition.ReportCode == "C171")
        {
            searchCondition.PageNumber ??= 1;
            searchCondition.PageSize ??= 10;

            if (searchCondition.PageNumber <= 0)
            {
                return BadRequest("頁碼必須大於零。");
            }

            if (searchCondition.PageSize is not (10 or 30 or 50))
            {
                return BadRequest("每頁筆數僅接受 10、30 或 50。");
            }
        }

        try
        {
            return searchCondition.ReportCode switch
            {
                "C171" => Ok(_reportService.ReportDataAndColumns<HealthCenterDetailViewModel>(searchCondition)),
                "C172" => Ok(_reportService.ReportDataAndColumns<HealthCenterCountViewModel>(searchCondition)),
                "C173" => Ok(_reportService.ReportDataAndColumns<HealthCheckupVisits>(searchCondition)),
                "C174" => Ok(_reportService.ReportDataAndColumns<HealthCenterContractBillingReport>(searchCondition)),
                _ => Ok(null)
            };
        }
        catch (Exception exception)
        {
            var traceId = HttpContext.TraceIdentifier;
            _logger.LogError(
                exception,
                "報表查詢失敗。TraceId: {TraceId}, ReportCode: {ReportCode}, StartDate: {StartDate}, EndDate: {EndDate}, PageNumber: {PageNumber}, PageSize: {PageSize}",
                traceId,
                searchCondition.ReportCode,
                searchCondition.StartDate,
                searchCondition.EndDate,
                searchCondition.PageNumber,
                searchCondition.PageSize);

            var problemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "查詢報表時發生錯誤，請提供追蹤碼給系統管理人員。"
            };
            problemDetails.Extensions["traceId"] = traceId;
            return StatusCode(StatusCodes.Status500InternalServerError, problemDetails);
        }
    }
}
