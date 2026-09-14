using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C10ReportControllerTests
{
    [Theory]
    [InlineData("2026-09-01", "2026-09-01", "OpdEr", "All", " ab12 ", true)]
    [InlineData("2026-09-02", "2026-09-01", "OpdEr", "All", null, false)]
    [InlineData("invalid", "2026-09-01", "OpdEr", "All", null, false)]
    [InlineData("2026-09-01", "2026-09-01", "Inpatient", "Emergency", null, false)]
    public void ValidationMatrix_NormalizesOrRejectsBeforeService(
        string start,
        string end,
        string source,
        string scope,
        string? medicalRecord,
        bool accepted)
    {
        var service = new CountingService();
        var condition = new SearchReportCondition
        {
            ReportCode = "C10",
            StartDate = start,
            EndDate = end,
            Source = source,
            RoomScope = scope,
            MedicalRecordNo = medicalRecord
        };

        IActionResult result = Controller(service).GetReportData(condition);

        if (accepted)
        {
            Assert.IsType<OkObjectResult>(result);
            Assert.Equal("AB12", condition.MedicalRecordNo);
            Assert.Equal(1, service.Calls);
            Assert.Equal(1, condition.PageNumber);
            Assert.Equal(10, condition.PageSize);
        }
        else
        {
            Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(0, service.Calls);
        }
    }

    [Fact]
    public void BlankValues_DefaultScopeAndNormalizeMedicalRecordToNull()
    {
        var service = new CountingService();
        var condition = Valid();
        condition.RoomScope = " ";
        condition.MedicalRecordNo = " ";

        Assert.IsType<OkObjectResult>(Controller(service).GetReportData(condition));

        Assert.Equal(C10RoomScopes.All, condition.RoomScope);
        Assert.Null(condition.MedicalRecordNo);
    }

    [Fact]
    public void EmptyResult_ReturnsSuccessfulZeroPage()
    {
        var service = new CountingService();

        var response = Assert.IsType<OkObjectResult>(Controller(service).GetReportData(Valid()));
        var report = Assert.IsType<ReportDataAndColumns<C10ReceivableDetailRow>>(response.Value);

        Assert.Empty(report.Data!);
        Assert.Equal(0, report.TotalCount);
        Assert.Equal(0, report.TotalPages);
    }

    [Fact]
    public void ServiceFailure_ReturnsSafeProblemWithoutPartialData()
    {
        var service = new CountingService { Failure = new InvalidOperationException("AB12 secret") };

        var response = Assert.IsType<ObjectResult>(Controller(service).GetReportData(Valid()));
        var problem = Assert.IsType<ProblemDetails>(response.Value);

        Assert.Equal(StatusCodes.Status500InternalServerError, response.StatusCode);
        Assert.DoesNotContain("AB12", problem.Title, StringComparison.Ordinal);
    }

    [Fact]
    public void Export_UsesSameNormalizedC10Condition()
    {
        var export = new Export();
        var controller = Controller(new CountingService(), export);
        var condition = Valid();
        condition.MedicalRecordNo = " ab12 ";

        Assert.IsType<FileContentResult>(controller.Export(condition));

        Assert.Equal("AB12", export.Condition!.MedicalRecordNo);
        Assert.Equal(C10Sources.OpdEr, export.Condition.Source);
        Assert.Equal(C10RoomScopes.All, export.Condition.RoomScope);
    }

    private static SearchReportCondition Valid() => new()
    {
        ReportCode = "C10",
        StartDate = "2026-09-01",
        EndDate = "2026-09-01",
        Source = C10Sources.OpdEr,
        RoomScope = C10RoomScopes.All
    };

    private static ReportController Controller(IReportService service, Export? export = null)
    {
        var controller = new ReportController(
            new Catalog(), service, export ?? new Export(), NullLogger<ReportController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        return controller;
    }

    private sealed class CountingService : IReportService
    {
        public int Calls { get; private set; }
        public Exception? Failure { get; init; }

        public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition condition)
        {
            Calls++;
            return new ReportDataAndColumns<T> { Data = [] };
        }

        public Task<ReportDataAndColumns<C10ReceivableDetailRow>> ReportC10Async(
            SearchReportCondition condition,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Failure is not null) throw Failure;
            return Task.FromResult(new ReportDataAndColumns<C10ReceivableDetailRow>
            {
                Data = [],
                TotalCount = 0,
                PageNumber = condition.PageNumber,
                PageSize = condition.PageSize,
                TotalPages = 0
            });
        }
    }

    private sealed class Catalog : IReportCatalogService
    {
        public ReportIndexViewModel GetReportIndex() => throw new NotSupportedException();
    }

    private sealed class Export : IReportExportService
    {
        public SearchReportCondition? Condition { get; private set; }

        public ReportExportDispatchResult Dispatch(SearchReportCondition condition)
        {
            Condition = condition;
            return new ReportExportDispatchResult([1], "C10.xlsx", null);
        }

        public ReportExportJob? GetJob(Guid jobId) => null;

        public ReportExportDownloadResult GetDownload(Guid jobId) => new(null, null);
    }
}
