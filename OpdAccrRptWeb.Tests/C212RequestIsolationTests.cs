using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C212RequestIsolationTests
{
    [Fact]
    public async Task MixedC212C211C28_TwentyOneConcurrentCallsKeepRequestRowsIsolated()
    {
        Task<string>[] calls = Enumerable.Range(1, 21).Select(index => (index % 3) switch
        {
            0 => RunC212Async(index),
            1 => RunC211Async(index),
            _ => Task.Run(() => RunC28(index))
        }).ToArray();

        string[] results = await Task.WhenAll(calls);

        Assert.Equal(21, results.Length);
        Assert.Equal(21, results.Distinct(StringComparer.Ordinal).Count());
        Assert.All(Enumerable.Range(1, 21), index =>
            Assert.Contains($"-{index}", results[index - 1], StringComparison.Ordinal));
    }

    private static async Task<string> RunC212Async(int index)
    {
        var service = new C212BoneBankBalanceReportService(
            new C212Repository(index), new C212PreserveDecimalAmountPolicy(), TimeProvider.System);
        var result = await service.CreateAsync(
            new(new DateOnly(2026, 9, 11)), $"u-{index}", $"trace-{index}");
        return $"C212-{result.Data!.Single().MedicalRecordNo}";
    }

    private static async Task<string> RunC211Async(int index)
    {
        var service = new C211ContractBalanceReportService(new C211Repository(index), TimeProvider.System);
        var result = await service.CreateAsync(new SearchReportCondition
        {
            EndDate = "2026-09-11", EncounterSource = C211Sources.Outpatient
        }, $"u-{index}");
        return $"C211-{result.Data!.Single().MedicalRecordNo}";
    }

    private static string RunC28(int index)
    {
        var service = new ReportService(
            new FakeHealthCenterRepository(), new FakeReferralMemberRepository(),
            new FakeSafeNeedleRepository(), new PassthroughReportTotalCountCache(),
            new CapturingLogger<ReportService>(),
            inpatientReceivableBalanceRepository: new C28Repository(index));
        var result = service.ReportDataAndColumns<InpatientReceivableBalanceReportViewModel>(
            new SearchReportCondition
            {
                ReportCode = "C28", EndDate = "2026-09-11", PageNumber = 1, PageSize = 10
            });
        return $"C28-{result.Data!.Single().MedicalRecordNumber}";
    }

    private sealed class C212Repository(int index) : IC212BoneBankBalanceRepository
    {
        public Task<IReadOnlyList<C212RawRow>> GetRowsAsync(
            string endDateRoc, string monthFirstDayRoc, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<C212RawRow>>(
                [new(C212RowKind.Movement, "1150911", index.ToString(), $"P-{index}", index)]);

        public Task<DateTime> GetOracleNowAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new DateTime(2026, 9, 11, 12, 0, 0));
    }

    private sealed class C211Repository(int index) : IC211ContractBalanceRepository
    {
        public Task<IReadOnlyList<C211Row>> GetRowsAsync(
            string source, DateOnly asOfDate, string? contractCode, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<C211Row>>(
                [new("AA", index.ToString(), "1150911", "", "", 1, index, 0)]);

        public Task<IReadOnlyList<C211ContractChoice>> GetContractChoicesAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<C211ContractChoice>>([]);
    }

    private sealed class C28Repository(int index) : IInpatientReceivableBalanceRepository
    {
        public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
            ModelDescriptionsHelper.GetPropertyDescriptions<InpatientReceivableBalanceReportViewModel>();
        public int GetCount(SearchReportCondition searchCondition) => 1;
        public List<InpatientReceivableBalanceReportViewModel> GetPage(SearchReportCondition searchCondition) =>
            [new() { MedicalRecordNumber = index.ToString() }];
    }
}
