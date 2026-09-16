using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C144ReportControllerTests
{
    [Theory]
    [InlineData("OpdEr")]
    [InlineData("Inpatient")]
    public void GetReportData_ValidC144_ReturnsEmptyEnvelopeAndNoStore(string source)
    {
        var service = new FakeService();
        ReportController controller = Create(service);
        var action = Assert.IsType<OkObjectResult>(controller.GetReportData(Condition(source)));
        var result = Assert.IsType<ReportDataAndColumns<C144DebtDetailReportViewModel>>(action.Value);
        Assert.Empty(result.Data!);
        Assert.Equal(31, result.Columns!.Count);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl);
        Assert.Equal(1, service.Calls);
    }

    [Theory]
    [InlineData("Other", "2026-09-01", "2026-09-16", 1, 10)]
    [InlineData("OpdEr", "bad", "2026-09-16", 1, 10)]
    [InlineData("OpdEr", "2026-09-17", "2026-09-16", 1, 10)]
    [InlineData("OpdEr", "2026-09-01", "2026-09-16", 0, 10)]
    [InlineData("OpdEr", "2026-09-01", "2026-09-16", 1, 20)]
    public void GetReportData_InvalidC144_Returns400BeforeService(
        string source, string start, string end, int page, int size)
    {
        var service = new FakeService();
        IActionResult action = Create(service).GetReportData(new SearchReportCondition
        {
            ReportCode = "C144", Source = source, StartDate = start, EndDate = end,
            PageNumber = page, PageSize = size
        });
        Assert.IsType<BadRequestObjectResult>(action);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public void GetReportData_CancelledC144_Returns499()
    {
        var action = Assert.IsType<StatusCodeResult>(Create(
            new FakeService { Failure = new OperationCanceledException() })
            .GetReportData(Condition("OpdEr")));
        Assert.Equal(499, action.StatusCode);
    }

    [Fact]
    public void GetReportData_UnexpectedC144Failure_DoesNotExposeSensitiveMessage()
    {
        const string sentinel = "MRN-SECRET SELECT * FROM GenDebtTbl Password=secret";
        var logger = new CapturingLogger<ReportController>();
        ReportController controller = Create(new FakeService
        {
            Failure = new InvalidOperationException(sentinel)
        }, logger: logger);
        var action = Assert.IsType<ObjectResult>(controller.GetReportData(Condition("OpdEr")));
        var problem = Assert.IsType<ProblemDetails>(action.Value);
        Assert.Equal(500, action.StatusCode);
        Assert.DoesNotContain(sentinel, problem.Title, StringComparison.Ordinal);
        Assert.All(logger.Entries, entry =>
            Assert.DoesNotContain(sentinel, entry.Message, StringComparison.Ordinal));
    }

    [Fact]
    public void Export_ValidC144_ReturnsWorkbookWithNoStore()
    {
        var export = new FakeExportService();
        ReportController controller = Create(new FakeService(), export);
        var action = Assert.IsType<FileContentResult>(controller.Export(Condition("Inpatient")));
        Assert.Equal([1, 2, 3], action.FileContents);
        Assert.Equal("C144.xlsx", action.FileDownloadName);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl);
        Assert.Equal("Inpatient", export.Condition!.Source);
    }

    private static SearchReportCondition Condition(string source) => new()
    {
        ReportCode = "C144", Source = source, StartDate = "2026-09-01",
        EndDate = "2026-09-16", PageNumber = 1, PageSize = 10
    };

    private static ReportController Create(
        IReportService service,
        IReportExportService? export = null,
        CapturingLogger<ReportController>? logger = null) => new(
        new FakeReportCatalogService(), service, export ?? new FakeReportExportService(),
        logger ?? new CapturingLogger<ReportController>())
    {
        ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { TraceIdentifier = "c144-trace" }
        }
    };

    private sealed class FakeService : IReportService
    {
        public int Calls { get; private set; }
        public Exception? Failure { get; init; }
        public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition) => new();
        public Task<ReportDataAndColumns<C144DebtDetailReportViewModel>> ReportC144Async(
            SearchReportCondition searchCondition, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Failure is not null) throw Failure;
            return Task.FromResult(new ReportDataAndColumns<C144DebtDetailReportViewModel>
            {
                Columns = C144DebtDetailReportService.GetColumns(), Data = [], TotalCount = 0,
                PageNumber = 1, PageSize = 10, TotalPages = 0
            });
        }
    }

    private sealed class FakeExportService : IReportExportService
    {
        public SearchReportCondition? Condition { get; private set; }
        public ReportExportDispatchResult Dispatch(SearchReportCondition searchReportCondition)
        {
            Condition = searchReportCondition;
            return new ReportExportDispatchResult([1, 2, 3], "C144.xlsx", null);
        }
        public ReportExportJob? GetJob(Guid jobId) => null;
        public ReportExportDownloadResult GetDownload(Guid jobId) => new(null, null);
    }
}
