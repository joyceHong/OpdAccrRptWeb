using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class M3ReportRunStoreTests
{
    [Fact]
    public void Save_IsolatesActorAndCopiesRows()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var store = new M3ReportRunStore(cache, TimeProvider.System);
        var rows = new List<M3OpdEmergencyDailyReportRow> { Row("0201", 1) };
        string id = store.Save("alice", new(2026, 9, 24), rows, Kpis());
        rows.Clear();
        Assert.True(store.TryGet(id, "alice", out M3OpdEmergencyDailyReportSnapshot snapshot));
        Assert.Single(snapshot.Rows);
        Assert.False(store.TryGet(id, "bob", out _));
        Assert.False(store.TryGet("bad", "alice", out _));
        Assert.NotEqual(0, snapshot.NumericChecksum);
    }

    internal static M3OpdEmergencyDailyReportRow Row(string id="0450", long value=1) => new(id,"名稱",
        value,value,value,value,value,value,value,value,value,value,
        value,value,value,value,value,value,value,value,value,value,value,value);
    internal static M3NineKpis Kpis() => new(1,2,3,4,5,6,7,8,-1);
}
