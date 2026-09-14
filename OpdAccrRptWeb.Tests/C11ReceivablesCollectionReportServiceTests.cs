using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C11ReceivablesCollectionReportServiceTests
{
    [Fact]
    public async Task CreateAsync_CrossYearExistingStartRow_IsOverwrittenInRepositoryOrder()
    {
        var repository = new FakeC11Repository
        {
            BaseRows = [new("113", "R", "門診", 100m), new("114", "R", "門診", 200m)],
            PeriodRows = [new("113", "R", "門診", 30m), new("114", "R", "門診", 40m)],
            PatientCount = 2m
        };
        var service = new C11ReceivablesCollectionReportService(repository, TimeProvider.System);

        C11ReceivablesCollectionReportViewModel report = await service.CreateAsync(new()
        {
            StartDate = "1131231", EndDate = "1140101", Source = C10Sources.OpdEr
        }, "tester");

        C11ReportRow startYear = report.Groups.Single().Rows.Single(row => row.Year == "113");
        Assert.Equal("114", startYear.PeriodKey);
        Assert.Equal(40f, startYear.Within30DaysAmount);
        Assert.Equal(60f, startYear.Over30DaysAmount);
        Assert.Equal(2, repository.PatientCountCalls);
        Assert.Equal("OpdAccRpt-06", report.ProgramNo);
    }

    [Fact]
    public async Task CreateAsync_MissingStartYear_AppendsDuplicateLaterYear()
    {
        var repository = new FakeC11Repository
        {
            BaseRows = [new("114", "I", "住院", 200m)],
            PeriodRows = [new("114", "I", "住院", 40m)],
            PatientCount = 3m
        };
        var service = new C11ReceivablesCollectionReportService(repository, TimeProvider.System);
        var report = await service.CreateAsync(new()
        {
            StartDate = "1131231", EndDate = "1140101", Source = C10Sources.Inpatient
        }, "tester");
        Assert.Equal(2, report.Groups.Single().Rows.Count(row => row.Year == "114"));
        Assert.Equal("住院應收帳款催收款月報表", report.Title);
    }

    [Fact]
    public async Task CreateAsync_EmptyRows_ReturnsEmptyPreviewModel()
    {
        var service = new C11ReceivablesCollectionReportService(new FakeC11Repository(), TimeProvider.System);
        var report = await service.CreateAsync(new()
        {
            StartDate = "1130101", EndDate = "1130131", Source = C10Sources.OpdEr
        }, "");
        Assert.Empty(report.Groups);
    }

    private sealed class FakeC11Repository : IC11ReceivablesCollectionRepository
    {
        public IReadOnlyList<C11AggregateRow> BaseRows { get; init; } = [];
        public IReadOnlyList<C11AggregateRow> PeriodRows { get; init; } = [];
        public decimal PatientCount { get; init; }
        public int PatientCountCalls { get; private set; }
        public Task<IReadOnlyList<C11AggregateRow>> QueryOutstandingToEndAsync(string source, string endDate, CancellationToken cancellationToken = default) => Task.FromResult(BaseRows);
        public Task<IReadOnlyList<C11AggregateRow>> QueryPeriodOutstandingAsync(string source, string startDate, string endDate, CancellationToken cancellationToken = default) => Task.FromResult(PeriodRows);
        public Task<C11PatientCountRow?> QueryPeriodPatientCountAsync(string source, string startDate, string endDate, string roomTypeName, CancellationToken cancellationToken = default) { PatientCountCalls++; return Task.FromResult<C11PatientCountRow?>(new(roomTypeName, PatientCount)); }
    }
}
