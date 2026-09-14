using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C211ContractBalanceReportServiceTests
{
    [Fact]
    public async Task ReportService_DelegatesC211WithoutPagingOrMutableSharedRows()
    {
        var c211 = new StubC211Service();
        var service = new ReportService(
            new FakeHealthCenterRepository(), new FakeReferralMemberRepository(),
            new FakeSafeNeedleRepository(), new PassthroughReportTotalCountCache(),
            new CapturingLogger<ReportService>(), c211ReportService: c211);
        var condition = new SearchReportCondition
        {
            ReportCode = "C211", EndDate = "2026-08-31", EncounterSource = "I", ContractCode = "TT"
        };

        var result = await service.ReportC211Async(condition, "user-1", CancellationToken.None);

        Assert.Empty(result.Data!);
        Assert.Same(condition, c211.Condition);
        Assert.Equal("user-1", c211.UserId);
        Assert.Null(condition.PageNumber);
        Assert.Null(condition.PageSize);
    }

    [Fact]
    public async Task CreateAsync_PreservesRowsAndBuildsGroupsAndGrandTotals()
    {
        var repository = new StubRepository(
        [
            new("AA", " 123 ", "1150830", "0800", "A001", 1, 100m, -100m),
            new("AA", "123", "1150830", "0900", "A001", 2, 2.5m, 3.25m),
            new("TT", "456", "1150831", "1000", "A002", 1, -1m, 4m)
        ]);
        var service = new C211ContractBalanceReportService(
            repository, new FixedTimeProvider(new DateTimeOffset(2026, 9, 11, 4, 5, 6, TimeSpan.Zero)));

        var result = await service.CreateAsync(new SearchReportCondition
        {
            EndDate = "2026-08-31", EncounterSource = "O"
        }, "tester");

        Assert.Equal(3, result.Data!.Count);
        var summary = Assert.IsType<C211ReportSummary>(result.Summary);
        Assert.Equal("合約單位餘額明細表(門急)", summary.Title);
        Assert.Equal("資料日期： ~ 115/08/31", summary.ReportDate);
        Assert.Equal("tester", summary.EditUser);
        Assert.Equal(101.5m, summary.SelfGrandTotal);
        Assert.Equal(-92.75m, summary.ClaimGrandTotal);
        Assert.Collection(summary.Groups,
            group => Assert.Equal(("AA", 102.5m, -96.75m), (group.ContractCode, group.SelfAmount, group.ClaimAmount)),
            group => Assert.Equal(("TT", -1m, 4m), (group.ContractCode, group.SelfAmount, group.ClaimAmount)));
    }

    [Fact]
    public void BuildGroups_ThrowsInsteadOfSilentlyOverflowing() =>
        Assert.Throws<OverflowException>(() => C211ContractBalanceReportService.BuildGroups(
        [
            new("AA", "1", "1150101", "", "", 1, decimal.MaxValue, 0),
            new("AA", "2", "1150101", "", "", 2, 1, 0)
        ]));

    private sealed class StubRepository(IReadOnlyList<C211Row> rows) : IC211ContractBalanceRepository
    {
        public Task<IReadOnlyList<C211Row>> GetRowsAsync(string source, DateOnly asOfDate,
            string? contractCode, CancellationToken cancellationToken = default) => Task.FromResult(rows);
        public Task<IReadOnlyList<C211ContractChoice>> GetContractChoicesAsync(
            CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<C211ContractChoice>>([]);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubC211Service : IC211ContractBalanceReportService
    {
        public SearchReportCondition? Condition { get; private set; }
        public string? UserId { get; private set; }
        public Task<ReportDataAndColumns<C211ContractBalanceReportViewModel>> CreateAsync(
            SearchReportCondition condition, string userId, CancellationToken cancellationToken = default)
        {
            Condition = condition;
            UserId = userId;
            return Task.FromResult(new ReportDataAndColumns<C211ContractBalanceReportViewModel>
            {
                Columns = [], Data = []
            });
        }
    }
}
