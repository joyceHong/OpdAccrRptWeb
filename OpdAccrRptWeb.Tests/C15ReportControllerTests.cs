using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C15ReportControllerTests
{
    [Fact]
    public void C15_IsCataloguedRoutedAndRegisteredForDependencyInjection()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        string catalog = File.ReadAllText(Path.Combine(root, "Services", "ReportCatalogService.cs"));
        string program = File.ReadAllText(Path.Combine(root, "Program.cs"));
        string router = File.ReadAllText(Path.Combine(root, "wwwroot", "js", "report-app.js"));

        Assert.Contains("Report(\"C15\", \"社工輔助器具保證金明細表\")", catalog);
        Assert.Contains("AddScoped<IC15AssistiveDeviceDepositDetailRepository, C15AssistiveDeviceDepositDetailRepository>()", program);
        Assert.Contains("AddSingleton<IC15LegacyReducer, C15LegacyReducer>()", program);
        Assert.Contains("C15: window.ReportComponents.ReportTemplate", router);
    }

    [Fact]
    public void GetReportData_ValidRangeDispatchesC15WithPagingAndNoStore()
    {
        var service = new C15CapturingReportService();
        ReportController controller = CreateController(service);

        IActionResult action = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C15",
            StartDate = "2026-09-01",
            EndDate = "2026-09-16"
        });

        Assert.IsType<OkObjectResult>(action);
        Assert.Equal(1, service.Calls);
        Assert.Equal(1, service.Condition!.PageNumber);
        Assert.Equal(10, service.Condition.PageSize);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl);
        Assert.Equal("no-cache", controller.Response.Headers.Pragma);
    }

    [Fact]
    public void GetReportData_C15CompletePreviewAcceptsTotalCountAsPageSize()
    {
        var service = new C15CapturingReportService();
        ReportController controller = CreateController(service);

        IActionResult action = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C15",
            StartDate = "2026-09-01",
            EndDate = "2026-09-16",
            PageNumber = 1,
            PageSize = 137
        });

        Assert.IsType<OkObjectResult>(action);
        Assert.Equal(137, service.Condition!.PageSize);
    }

    [Theory]
    [InlineData(null, "2026-09-16")]
    [InlineData("2026-02-30", "2026-09-16")]
    [InlineData("2026-09-16", "2026-09-01")]
    public void GetReportData_InvalidRangeDoesNotDispatch(string? start, string? end)
    {
        var service = new C15CapturingReportService();
        ReportController controller = CreateController(service);

        IActionResult action = controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C15",
            StartDate = start,
            EndDate = end
        });

        Assert.IsType<BadRequestObjectResult>(action);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public void GetReportData_UnexpectedFailureReturnsSafeProblemAndSafeLog()
    {
        const string sensitive = "MR123 Patient SQL :StartDate provider-secret";
        var logger = new CapturingLogger<ReportController>();
        ReportController controller = CreateController(
            new C15CapturingReportService { Failure = new InvalidOperationException(sensitive) }, logger);

        ObjectResult action = Assert.IsType<ObjectResult>(controller.GetReportData(new SearchReportCondition
        {
            ReportCode = "C15",
            StartDate = "2026-09-01",
            EndDate = "2026-09-16"
        }));
        var problem = Assert.IsType<ProblemDetails>(action.Value);

        Assert.Equal(500, action.StatusCode);
        Assert.DoesNotContain(sensitive, problem.Title);
        Assert.DoesNotContain(sensitive, string.Join(" ", logger.Entries.Select(entry => entry.Message)));
    }

    private static ReportController CreateController(
        IReportService service,
        CapturingLogger<ReportController>? logger = null) => new(
            new FakeReportCatalogService(),
            service,
            new FakeReportExportService(),
            logger ?? new CapturingLogger<ReportController>())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "c15-trace" }
            }
        };

    private sealed class C15CapturingReportService : IReportService
    {
        public int Calls { get; private set; }
        public SearchReportCondition? Condition { get; private set; }
        public Exception? Failure { get; init; }

        public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition) => new();

        public Task<ReportDataAndColumns<C15AssistiveDeviceDepositDetailReportViewModel>> ReportC15Async(
            SearchReportCondition searchCondition,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            Condition = searchCondition;
            if (Failure is not null) throw Failure;
            return Task.FromResult(new ReportDataAndColumns<C15AssistiveDeviceDepositDetailReportViewModel>
            {
                Data = [], Columns = [], TotalCount = 0, PageNumber = 1, PageSize = 10, TotalPages = 0
            });
        }
    }
}
