using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C143ReportControllerTests
{
    [Theory]
    [InlineData("OpdEr", "Difference")]
    [InlineData("OpdEr", "All")]
    [InlineData("Inpatient", "Difference")]
    [InlineData("Inpatient", "All")]
    public void GetReportData_ValidC143_ReturnsPagedEnvelopeAndNoStore(string source, string reportType)
    {
        var service = new C143Service();
        ReportController controller = Create(service);

        var action = Assert.IsType<OkObjectResult>(controller.GetReportData(Condition(source, reportType)));
        var result = Assert.IsType<ReportDataAndColumns<C143AccountingBalanceDebtReportViewModel>>(action.Value);

        Assert.Empty(result.Data!);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(1, service.Calls);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl);
    }

    [Theory]
    [InlineData("Other", "Difference", "2026-09-01", "2026-09-14", 1, 10)]
    [InlineData("OpdEr", "Other", "2026-09-01", "2026-09-14", 1, 10)]
    [InlineData("OpdEr", "Difference", "bad", "2026-09-14", 1, 10)]
    [InlineData("OpdEr", "Difference", "2026-09-15", "2026-09-14", 1, 10)]
    [InlineData("OpdEr", "Difference", "2026-09-01", "2026-09-14", 0, 10)]
    [InlineData("OpdEr", "Difference", "2026-09-01", "2026-09-14", 1, 20)]
    public void GetReportData_InvalidC143_Returns400BeforeService(
        string source, string reportType, string start, string end, int page, int size)
    {
        var service = new C143Service();

        IActionResult action = Create(service).GetReportData(new SearchReportCondition
        {
            ReportCode = "C143", Source = source, ReportType = reportType,
            StartDate = start, EndDate = end, PageNumber = page, PageSize = size
        });

        Assert.IsType<BadRequestObjectResult>(action);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public void GetReportData_CancelledC143_Returns499()
    {
        var action = Assert.IsType<StatusCodeResult>(Create(
            new C143Service { Failure = new OperationCanceledException() }).GetReportData(
            Condition("OpdEr", "Difference")));
        Assert.Equal(499, action.StatusCode);
    }

    [Fact]
    public void GetReportData_UnexpectedC143Failure_IsSafe()
    {
        const string sentinel = "MRN-SECRET SELECT * FROM GenDebtTbl";
        var logger = new CapturingLogger<ReportController>();
        var action = Assert.IsType<ObjectResult>(Create(
            new C143Service { Failure = new InvalidOperationException(sentinel) }, logger).GetReportData(
            Condition("OpdEr", "Difference")));
        var problem = Assert.IsType<ProblemDetails>(action.Value);

        Assert.Equal(500, action.StatusCode);
        Assert.DoesNotContain(sentinel, problem.Title, StringComparison.Ordinal);
        Assert.All(logger.Entries, entry => Assert.DoesNotContain(sentinel, entry.Message, StringComparison.Ordinal));
    }

    private static SearchReportCondition Condition(string source, string reportType) => new()
    {
        ReportCode = "C143", StartDate = "2026-09-01", EndDate = "2026-09-14",
        Source = source, ReportType = reportType, PageNumber = 1, PageSize = 10
    };

    private static ReportController Create(IReportService service, CapturingLogger<ReportController>? logger = null) =>
        new(new FakeReportCatalogService(), service, new FakeReportExportService(),
            logger ?? new CapturingLogger<ReportController>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "c143-trace" }
            }
        };

    private sealed class C143Service : IReportService
    {
        public int Calls { get; private set; }
        public Exception? Failure { get; init; }
        public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition condition) => new();
        public Task<ReportDataAndColumns<C143AccountingBalanceDebtReportViewModel>> ReportC143Async(
            SearchReportCondition condition, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Failure is not null) throw Failure;
            return Task.FromResult(new ReportDataAndColumns<C143AccountingBalanceDebtReportViewModel>
                { Columns = [], Data = [], TotalCount = 0, PageNumber = 1, PageSize = 10, TotalPages = 0 });
        }
    }
}
