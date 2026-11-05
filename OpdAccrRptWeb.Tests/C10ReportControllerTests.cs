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

    [Fact]
    public async Task Preview_GroupsAllRowsByVisitAndIgnoresQueryPagination()
    {
        var service = new CountingService
        {
            ResultRows =
            [
                new() { VisitDate = "1151102", VisitTime = "1", VisitRoom = "T10O01", VisitNumber = 9911, ItemName1 = "處置費" },
                new() { VisitDate = "1151102", VisitTime = "1", VisitRoom = "T10O01", VisitNumber = 9911, ItemName1 = "藥品費" },
                new() { VisitDate = "1151104", VisitTime = "1", VisitRoom = "T10O02", VisitNumber = 9913, ItemName1 = "處置費" }
            ]
        };
        SearchReportCondition condition = Valid();
        condition.StartDate = "2026-11-02";
        condition.EndDate = "2026-11-04";
        condition.PageNumber = 2;
        condition.PageSize = 10;

        var response = Assert.IsType<PartialViewResult>(await Controller(service).PreviewC10(condition));
        var preview = Assert.IsType<C10PreviewViewModel>(response.Model);

        Assert.Equal("_C10ReceivableDetailPreview", response.ViewName);
        Assert.Equal(1, service.Calls);
        Assert.Equal(1, service.LastCondition!.PageNumber);
        Assert.Equal(int.MaxValue, service.LastCondition.PageSize);
        Assert.Equal(2, preview.Pages.Count);
        Assert.Equal(2, preview.Pages[0].Rows.Count);
        Assert.Single(preview.Pages[1].Rows);
        Assert.Equal("115/11/02 ～ 115/11/04", preview.DateRange);
    }

    [Fact]
    public async Task Preview_RejectsInvalidDatesBeforeReadingPatientData()
    {
        var service = new CountingService();
        SearchReportCondition condition = Valid();
        condition.EndDate = "2026-08-31";

        Assert.IsType<BadRequestObjectResult>(await Controller(service).PreviewC10(condition));
        Assert.Equal(0, service.Calls);
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
        public List<C10ReceivableDetailRow> ResultRows { get; init; } = [];
        public SearchReportCondition? LastCondition { get; private set; }

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
            LastCondition = condition;
            if (Failure is not null) throw Failure;
            return Task.FromResult(new ReportDataAndColumns<C10ReceivableDetailRow>
            {
                Data = ResultRows,
                TotalCount = ResultRows.Count,
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

        public ReportExportDispatchResult Dispatch(SearchReportCondition condition, string? ownerIdentity = null)
        {
            Condition = condition;
            return new ReportExportDispatchResult([1], "C10.xlsx", null);
        }

        public ReportExportJob? GetJob(Guid jobId, string? requesterIdentity = null) => null;

        public ReportExportDownloadResult GetDownload(Guid jobId, string? requesterIdentity = null) => new(null, null);
    }
}
