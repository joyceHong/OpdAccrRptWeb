using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class M1ReportRunStoreTests
{
    [Fact]
    public void Save_IsolatesOwnerAndCopiesRows()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new M1ReportRunStore(cache, TimeProvider.System);
        var mutable = new List<M1DoctorDailyReportRow> { Row("D1") };

        string runId = store.Save("alice", new(2026, 9, 23), mutable);
        mutable.Clear();

        Assert.Equal(48, runId.Length);
        Assert.True(store.TryGet(runId, "alice", out M1DoctorDailyReportSnapshot snapshot));
        Assert.Single(snapshot.Rows);
        Assert.False(store.TryGet(runId, "bob", out _));
        Assert.False(store.TryGet("not-a-run", "alice", out _));
    }

    [Fact]
    public void Save_RejectsEmptyAndOversizedResults()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new M1ReportRunStore(cache, TimeProvider.System);
        Assert.Throws<ArgumentException>(() => store.Save("alice", new(2026, 9, 23), []));
        Assert.Throws<InvalidOperationException>(() => store.Save("alice", new(2026, 9, 23),
            Enumerable.Repeat(Row("D1"), M1ReportRunStore.MaximumRows + 1).ToArray()));
    }

    private static M1DoctorDailyReportRow Row(string doctorNo) =>
        M1DoctorDailyReportRow.Create("0450", "急診", doctorNo, "王醫師", "R", 1, 2, 1, 1, 1, 0);
}

