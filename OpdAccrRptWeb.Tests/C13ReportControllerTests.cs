using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C13ReportControllerTests
{
    [Fact]
    public void GetReportData_ValidCondition_DefaultsPagingAndUsesTypedDispatch()
    {
        var service = new CapturingReportService();
        ReportController controller = Create(service);

        IActionResult action = controller.GetReportData(new()
        {
            ReportCode = "C13", StartDate = "2026-09-14", EndDate = "2026-09-15"
        });

        Assert.IsType<OkObjectResult>(action);
        Assert.Equal(typeof(C13HighRiskEmergencyReportViewModel), service.ResultType);
        Assert.Equal(1, service.Condition!.PageNumber);
        Assert.Equal(10, service.Condition.PageSize);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl);
        Assert.Equal("no-cache", controller.Response.Headers.Pragma);
    }

    [Theory]
    [InlineData(null, "2026-09-15")]
    [InlineData("2026-02-30", "2026-09-15")]
    [InlineData("2026-09-16", "2026-09-15")]
    public void GetReportData_InvalidDate_ReturnsBadRequestBeforeDispatch(string? start, string? end)
    {
        var service = new CapturingReportService();

        IActionResult action = Create(service).GetReportData(new()
        {
            ReportCode = "C13", StartDate = start, EndDate = end
        });

        Assert.IsType<BadRequestObjectResult>(action);
        Assert.Null(service.ResultType);
    }

    [Fact]
    public void GetReportData_UnexpectedFailure_DoesNotLogOrReturnSensitiveMessage()
    {
        const string sentinel = "SELECT PATIENT-NAME NATIONAL-ID";
        var logger = new CapturingLogger<ReportController>();
        ReportController controller = Create(new ThrowingReportService(new InvalidOperationException(sentinel)), logger: logger);

        var action = Assert.IsType<ObjectResult>(controller.GetReportData(new()
        {
            ReportCode = "C13", StartDate = "2026-09-14", EndDate = "2026-09-15"
        }));
        var problem = Assert.IsType<ProblemDetails>(action.Value);

        Assert.Equal(500, action.StatusCode);
        Assert.DoesNotContain(sentinel, problem.Title, StringComparison.Ordinal);
        Assert.All(logger.Entries, entry =>
        {
            Assert.Null(entry.Exception);
            Assert.DoesNotContain(sentinel, entry.Message, StringComparison.Ordinal);
        });
    }

    [Fact]
    public void PreviewC13_ReturnsNoStorePartialWithCompleteSnapshot()
    {
        var previewService = new FakePreviewService
        {
            Result = new C13PreviewViewModel
            {
                StartDate = "115/09/14", EndDate = "115/09/15",
                GeneratedAt = "115/09/15  08:35:22", GeneratedBy = "",
                Rows = [new() { PatientName = "測試" }]
            }
        };
        ReportController controller = Create(new CapturingReportService(), previewService);

        var action = Assert.IsType<PartialViewResult>(controller.PreviewC13(new()
        {
            StartDate = "2026-09-14", EndDate = "2026-09-15"
        }));

        Assert.Equal("_C13HighRiskEmergencyPreview", action.ViewName);
        Assert.Equal("private, no-store", controller.Response.Headers.CacheControl);
        Assert.Equal("no-cache", controller.Response.Headers.Pragma);
        Assert.Equal(1, previewService.Calls);
    }

    private static ReportController Create(
        IReportService service,
        IC13HighRiskEmergencyReportService? preview = null,
        CapturingLogger<ReportController>? logger = null) => new(
            new FakeReportCatalogService(), service, new FakeReportExportService(),
            logger ?? new CapturingLogger<ReportController>(), c13ReportService: preview)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { TraceIdentifier = "c13-trace" }
            }
        };

    private sealed class CapturingReportService : IReportService
    {
        public Type? ResultType { get; private set; }
        public SearchReportCondition? Condition { get; private set; }
        public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition)
        {
            ResultType = typeof(T);
            Condition = searchCondition;
            return new() { Columns = [], Data = [], TotalCount = 0, TotalPages = 0 };
        }
    }

    private sealed class ThrowingReportService(Exception exception) : IReportService
    {
        public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition) => throw exception;
    }

    private sealed class FakePreviewService : IC13HighRiskEmergencyReportService
    {
        public required C13PreviewViewModel Result { get; init; }
        public int Calls { get; private set; }
        public C13PreviewViewModel CreatePreview(SearchReportCondition condition, string generatedBy, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Result;
        }
    }
}
