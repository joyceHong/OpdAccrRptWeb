using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class M2ReportRunStoreTests
{
    [Fact]
    public void SaveCopiesRowsAndIsolatesOwner()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new M2ReportRunStore(cache, TimeProvider.System);
        int[] counts = new int[31]; counts[0] = 2;
        string id = store.Save("alice", new(2026, 8, 1), M2CalculationBasis.Statistics,
            M2VisitScope.All, M2TimeSlot.All,
            [M2DoctorMonthlyReportRow.Create("0450", "急診", "D1", "醫師", counts)]);
        counts[0] = 99;
        Assert.Equal(48, id.Length);
        Assert.True(store.TryGet(id, "alice", out M2DoctorMonthlyReportSnapshot snapshot));
        Assert.Equal(2, snapshot.Rows[0].DailyCounts[0]);
        Assert.Equal(2, snapshot.NumericChecksum);
        Assert.False(store.TryGet(id, "bob", out _));
        Assert.False(store.TryGet("invalid", "alice", out _));
    }
}
