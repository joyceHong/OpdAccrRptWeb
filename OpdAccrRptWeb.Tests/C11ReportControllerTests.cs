using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C11ReportControllerTests
{
    [Theory]
    [InlineData("1130101abc", "1130131xyz", null, "OpdEr")]
    [InlineData(" 1130101 ", " 1130131 ", "Inpatient", "Inpatient")]
    public void GetReportData_CompatibleInput_NormalizesAndDispatches(
        string start, string end, string? source, string expectedSource)
    {
        var service = new CapturingC11Service();
        ReportController controller = Create(service);
        var result = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C11", StartDate = start, EndDate = end, Source = source
        });
        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(1, service.Calls);
        Assert.Equal(expectedSource, service.LastCondition!.Source);
        Assert.Equal(start.Trim(), service.LastCondition.StartDate);
    }

    [Theory]
    [InlineData(null, "1130131")]
    [InlineData("", "1130131")]
    [InlineData("1140101", "1131231")]
    public void GetReportData_InvalidInput_ReturnsBadRequestWithoutDispatch(string? start, string end)
    {
        var service = new CapturingC11Service();
        IActionResult result = Create(service).GetReportData(new SearchReportCondition
        {
            ReportCode = "C11", StartDate = start, EndDate = end
        });
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal(0, service.Calls);
    }

    private static ReportController Create(IReportService service)
    {
        var controller = new ReportController(new FakeReportCatalogService(), service,
            new FakeReportExportService(), new CapturingLogger<ReportController>());
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private sealed class CapturingC11Service : IReportService
    {
        public int Calls { get; private set; }
        public SearchReportCondition? LastCondition { get; private set; }
        public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition) => new();
        public Task<C11ReceivablesCollectionReportViewModel> ReportC11Async(SearchReportCondition searchCondition, string generatedBy, CancellationToken cancellationToken = default)
        {
            Calls++; LastCondition = searchCondition;
            return Task.FromResult(new C11ReceivablesCollectionReportViewModel());
        }
    }
}
