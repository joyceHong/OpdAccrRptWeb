using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C212BoneBankBalanceReportServiceTests
{
    [Fact]
    public async Task CreateResultAsync_UsesOnlyEndDateAndConvertsEveryRowBeforeTotaling()
    {
        var repository = new FakeRepository
        {
            Rows =
            [
                new(C212RowKind.OpeningBalance, "", "", "期初餘額", 10.4m),
                new(C212RowKind.Movement, "1150911", "A001", "王小明", 0.4m)
            ]
        };
        var policy = new RecordingPolicy();
        var service = new C212BoneBankBalanceReportService(
            repository, policy, new FixedTimeProvider());

        C212ReportResult result = await service.CreateResultAsync(
            new C212Query(new DateOnly(2026, 9, 11)),
            "tester", "trace-1");

        Assert.Equal("1150911", repository.EndDateRoc);
        Assert.Equal("1150901", repository.MonthFirstDayRoc);
        Assert.Equal(new[] { 10.4m, 0.4m }, policy.Values);
        Assert.Equal(10m, result.TotalAmount);
        Assert.Equal(2, result.Rows.Count);
        Assert.Equal(0m, result.Rows[1].Amount);
        Assert.Equal(C212DataStatus.Unknown, result.DataStatus);
        Assert.Equal("tester", result.GeneratedBy);
        Assert.Equal("trace-1", result.CorrelationId);
        Assert.Equal("115/09/11  13:14:15", result.ReportProcessDateTimeRoc);
    }

    [Fact]
    public async Task CreateResultAsync_EndDateDeterminesRepositoryRange()
    {
        var repository = new FakeRepository();
        var service = new C212BoneBankBalanceReportService(
            repository, new C212PreserveDecimalAmountPolicy(), new FixedTimeProvider());

        await service.CreateResultAsync(new(new DateOnly(2026, 9, 11)), "u", "1");

        Assert.Equal("1150911", repository.EndDateRoc);
        Assert.Equal("1150901", repository.MonthFirstDayRoc);
    }

    [Fact]
    public async Task CreateAsync_EmptyRowsStillReturnsMetadataAndEmptyData()
    {
        var service = new C212BoneBankBalanceReportService(
            new FakeRepository(), new C212PreserveDecimalAmountPolicy(), new FixedTimeProvider());

        var report = await service.CreateAsync(
            new(new DateOnly(2026, 9, 11)), "user", "trace");

        Assert.Empty(report.Data!);
        var summary = Assert.IsType<C212ReportSummary>(report.Summary);
        Assert.Equal("目前無法確認資料完整性", summary.DataStatusMessage);
    }

    private sealed class FakeRepository : IC212BoneBankBalanceRepository
    {
        public IReadOnlyList<C212RawRow> Rows { get; init; } = [];
        public string? EndDateRoc { get; private set; }
        public string? MonthFirstDayRoc { get; private set; }

        public Task<IReadOnlyList<C212RawRow>> GetRowsAsync(
            string endDateRoc, string monthFirstDayRoc, CancellationToken cancellationToken = default)
        {
            EndDateRoc = endDateRoc;
            MonthFirstDayRoc = monthFirstDayRoc;
            return Task.FromResult(Rows);
        }

        public Task<DateTime> GetOracleNowAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new DateTime(2026, 9, 11, 13, 14, 15));
    }

    private sealed class RecordingPolicy : IC212AmountCompatibilityPolicy
    {
        public List<decimal> Values { get; } = [];
        public decimal Convert(decimal rawOracleAmount)
        {
            Values.Add(rawOracleAmount);
            return decimal.Truncate(rawOracleAmount);
        }
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 11, 5, 14, 15, TimeSpan.Zero);
    }
}
