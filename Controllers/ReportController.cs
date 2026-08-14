using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
using static OpdAccrRptWeb.Help.ModelDescriptionsHelper;

namespace OpdAccrRptWeb.Controllers;

public sealed class ReportController : Controller
{
    private readonly IReportCatalogService _reportCatalogService;
    private readonly IHealthCenterRepository _healthCenterRepository;
    private readonly IReportService _reportService;

    public ReportController(IReportCatalogService reportCatalogService, IReportService reportService, IHealthCenterRepository healthCenterRepository)
    {
        _reportCatalogService = reportCatalogService;
        _healthCenterRepository = healthCenterRepository;
        _reportService = reportService;
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
        switch (searchCondition.ReportCode)
        {
            case "C171":
                return Ok(_reportService.ReportDataAndColumns<HelthCenterDetailViewModel>(searchCondition));
            case "C172":
                return Ok(_reportService.ReportDataAndColumns<HelthCenterCountViewModel>(searchCondition));
            default:
                return Ok(null);                
        }
    }
}
