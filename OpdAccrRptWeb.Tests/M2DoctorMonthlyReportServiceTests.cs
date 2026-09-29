using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class M2DoctorMonthlyReportServiceTests
{
    [Fact]
    public void Transform_AggregatesMergesDropsZeroAndKeepsLegacyMissingTarget()
    {
        IReadOnlyList<M2DoctorMonthlyReportRow> rows = M2DoctorMonthlyReportService.Transform([
            Source("0450", "D1", 1, 2), Source("0420", "D1", 1, 3),
            Source("0420", "D2", 1, 9), Source("0221", "D3", 2, 4),
            Source("0227", "D3", 2, 5), Source("0300", "D4", 1, 2), Source("0300", "D4", 2, -2)
        ]);
        Assert.Collection(rows,
            row => { Assert.Equal("0220", row.SectionNo); Assert.Equal(9, row.DailyCounts[1]); },
            row => { Assert.Equal("0450", row.SectionNo); Assert.Equal(5, row.DailyCounts[0]); });
        Assert.DoesNotContain(rows, row => row.DoctorNo is "D2" or "D4");
    }

    [Theory]
    [InlineData("2025-02", 28)]
    [InlineData("2024-02", 29)]
    [InlineData("2026-04", 30)]
    [InlineData("2026-05", 31)]
    public async Task ActualVisit_QueriesOnlyValidDaysAndSavesAfterAllSucceed(string month, int days)
    {
        using var fixture = CreateFixture();
        M2DoctorMonthlyReportSnapshot? snapshot = await fixture.Service.GenerateAsync(
            new(month, M2CalculationBasis.ActualVisit), "alice");
        Assert.NotNull(snapshot);
        Assert.Equal(days, fixture.Repository.ActualDays.Count);
        Assert.Equal(Enumerable.Range(1, days), fixture.Repository.ActualDays);
        Assert.True(fixture.Store.TryGet(snapshot.RunId, "alice", out _));
    }

    [Fact]
    public async Task InvalidInputsDoNotCallRepositoryAndPagingReusesSnapshot()
    {
        using var fixture = CreateFixture();
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.QueryAsync(
            new("bad"), "alice"));
        Assert.Equal(0, fixture.Repository.Calls);
        M2DoctorMonthlyPagedResponse first = await fixture.Service.QueryAsync(new("2026-08"), "alice");
        M2DoctorMonthlyPagedResponse second = await fixture.Service.QueryAsync(
            new(null, RunId: first.RunId), "alice");
        Assert.Equal(1, fixture.Repository.Calls);
        Assert.Equal(first.TotalCount, second.TotalCount);
        await Assert.ThrowsAsync<M2ReportRunNotFoundException>(() => fixture.Service.QueryAsync(
            new(null, RunId: first.RunId), "bob"));
    }

    [Fact]
    public async Task ActualVisitFailureDoesNotSavePartialSnapshot()
    {
        using var fixture = CreateFixture(failDay: 3);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.GenerateAsync(
            new("2026-08", M2CalculationBasis.ActualVisit), "alice"));
        Assert.Equal([1, 2, 3], fixture.Repository.ActualDays);
        Assert.Equal(0, fixture.Store.SaveCalls);
    }

    private static Fixture CreateFixture(int? failDay = null)
    {
        var repository = new Repository(failDay);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var inner = new M2ReportRunStore(cache, TimeProvider.System);
        var store = new Store(inner);
        return new(new M2DoctorMonthlyReportService(repository, store), repository, store, cache);
    }

    private static M2DoctorMonthlySourceRow Source(string section, string doctor, int day, int count) =>
        new(section, $"科別{section}", doctor, $"醫師{doctor}", day, count);

    private sealed class Repository(int? failDay) : IM2DoctorMonthlyReportRepository
    {
        public int Calls { get; private set; }
        public List<int> ActualDays { get; } = [];
        public Task<IReadOnlyList<M2DoctorMonthlySourceRow>> QueryStatisticsAsync(string rocMonth,
            M2VisitScope visitScope, M2TimeSlot timeSlot, CancellationToken cancellationToken = default)
        { Calls++; return Task.FromResult<IReadOnlyList<M2DoctorMonthlySourceRow>>([Source("0450", "D1", 1, 1)]); }
        public Task<IReadOnlyList<M2DoctorMonthlySourceRow>> QueryActualVisitDayAsync(string rocMonth,
            int day, M2VisitScope visitScope, M2TimeSlot timeSlot, CancellationToken cancellationToken = default)
        {
            Calls++; ActualDays.Add(day);
            if (day == failDay) throw new InvalidOperationException("oracle failure");
            return Task.FromResult<IReadOnlyList<M2DoctorMonthlySourceRow>>(
                day == 1 ? [Source("0450", "D1", 1, 1)] : []);
        }
    }

    private sealed class Store(IM2ReportRunStore inner) : IM2ReportRunStore
    {
        public int SaveCalls { get; private set; }
        public string Save(string actor, DateOnly reportMonth, M2CalculationBasis calculationBasis,
            M2VisitScope visitScope, M2TimeSlot timeSlot, IReadOnlyList<M2DoctorMonthlyReportRow> rows)
        { SaveCalls++; return inner.Save(actor, reportMonth, calculationBasis, visitScope, timeSlot, rows); }
        public bool TryGet(string runId, string actor, out M2DoctorMonthlyReportSnapshot snapshot) =>
            inner.TryGet(runId, actor, out snapshot);
    }

    private sealed record Fixture(M2DoctorMonthlyReportService Service, Repository Repository,
        Store Store, IDisposable Cache) : IDisposable { public void Dispose() => Cache.Dispose(); }
}
