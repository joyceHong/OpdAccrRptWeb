using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C24ReportControllerTests
{
    [Fact]
    public void ValidRequest_NormalizesMrnAndDispatches()
    {
        var service = new CountingService();
        var condition = Valid();
        condition.MedicalRecordNo = " ab123 ";

        Assert.IsType<OkObjectResult>(Controller(service).GetReportData(condition));

        Assert.Equal("AB123", condition.MedicalRecordNo);
        Assert.Equal(1, service.Calls);
    }

    [Theory]
    [InlineData("Inpatient", "Emergency", "Accounting")]
    [InlineData("Inpatient", "NonEmergency", "Billing")]
    [InlineData("Unknown", "All", "Accounting")]
    [InlineData("OpdEr", "All", "Unknown")]
    public void InvalidMatrix_Returns400WithoutDispatch(string source, string roomScope, string mode)
    {
        var service = new CountingService();
        var condition = Valid();
        condition.Source = source;
        condition.RoomScope = roomScope;
        condition.Mode = mode;

        Assert.IsType<BadRequestObjectResult>(Controller(service).GetReportData(condition));
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public void RebuildAndOversizeMrnAreRejected()
    {
        var service = new CountingService();
        var rebuild = Valid();
        rebuild.ForceRebuild = true;
        var mrn = Valid();
        mrn.MedicalRecordNo = "12345678901";

        Assert.IsType<BadRequestObjectResult>(Controller(service).GetReportData(rebuild));
        Assert.IsType<BadRequestObjectResult>(Controller(service).GetReportData(mrn));
        Assert.Equal(0, service.Calls);
    }

    private static SearchReportCondition Valid() => new()
    {
        ReportCode = "C24", StartDate = "2026-09-01", EndDate = "2026-09-03",
        Source = C24Sources.OpdEr, Mode = C24Modes.Accounting,
        RoomScope = C24RoomScopes.Emergency
    };

    private static ReportController Controller(IReportService service) => new(
        new Catalog(), service, new Export(), NullLogger<ReportController>.Instance);

    private sealed class CountingService : IReportService
    {
        public int Calls { get; private set; }
        public ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition condition)
        {
            Calls++;
            return new ReportDataAndColumns<T> { Data = [] };
        }
    }

    private sealed class Catalog : IReportCatalogService
    {
        public ReportIndexViewModel GetReportIndex() => throw new NotSupportedException();
    }

    private sealed class Export : IReportExportService
    {
        public ReportExportDispatchResult Dispatch(SearchReportCondition condition) => throw new NotSupportedException();
        public ReportExportJob? GetJob(Guid jobId) => null;
        public ReportExportDownloadResult GetDownload(Guid jobId) => new(null, null);
    }
}
