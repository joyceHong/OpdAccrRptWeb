using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportTotalCountCacheTests
{
    [Fact]
    public void GetOrCreate_SameNormalizedFilters_ReusesCountAcrossPageInputs()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new ReportTotalCountCache(memoryCache);
        var calls = 0;
        var filters = new Dictionary<string, string?>
        {
            ["StartDate"] = "1150801",
            ["EndDate"] = "1150831"
        };

        var first = cache.GetOrCreate("C174", filters, () => { calls++; return 28; });
        var second = cache.GetOrCreate("C174", filters, () => { calls++; return 99; });

        Assert.Equal(28, first);
        Assert.Equal(28, second);
        Assert.Equal(1, calls);
        Assert.Equal(TimeSpan.FromMinutes(3), ReportTotalCountCache.EntryLifetime);
        Assert.Equal(
            TimeSpan.FromMinutes(3),
            ReportTotalCountCache.CreateEntryOptions().AbsoluteExpirationRelativeToNow);
    }

    [Fact]
    public void GetOrCreate_DifferentReportOrFilter_ComputesIndependentCounts()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new ReportTotalCountCache(memoryCache);
        var calls = 0;

        int Count() => ++calls;

        cache.GetOrCreate("C174", new Dictionary<string, string?> { ["StartDate"] = "1150801" }, Count);
        cache.GetOrCreate("C174", new Dictionary<string, string?> { ["StartDate"] = "1150802" }, Count);
        cache.GetOrCreate("C171", new Dictionary<string, string?> { ["StartDate"] = "1150801" }, Count);

        Assert.Equal(3, calls);
    }

    [Fact]
    public void CreateCacheKey_IsOrderIndependentAndDelimiterSafe()
    {
        var first = ReportTotalCountCache.CreateCacheKey(
            "C174",
            new Dictionary<string, string?> { ["A"] = "x|B:1", ["B"] = "y" });
        var reordered = ReportTotalCountCache.CreateCacheKey(
            "C174",
            new Dictionary<string, string?> { ["B"] = "y", ["A"] = "x|B:1" });
        var different = ReportTotalCountCache.CreateCacheKey(
            "C174",
            new Dictionary<string, string?> { ["A"] = "x", ["B"] = "1|B:y" });

        Assert.Equal(first, reordered);
        Assert.NotEqual(first, different);
    }

    [Fact]
    public void GetOrCreate_FailedFactory_DoesNotCacheFailure()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new ReportTotalCountCache(memoryCache);
        var calls = 0;
        var filters = new Dictionary<string, string?>();

        Assert.Throws<InvalidOperationException>(() => cache.GetOrCreate("C174", filters, () =>
        {
            calls++;
            throw new InvalidOperationException("failed");
        }));

        var result = cache.GetOrCreate("C174", filters, () => { calls++; return 7; });

        Assert.Equal(7, result);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void GetOrCreate_NegativeCount_IsRejectedAndNotCached()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new ReportTotalCountCache(memoryCache);
        var filters = new Dictionary<string, string?>();

        Assert.Throws<InvalidOperationException>(() => cache.GetOrCreate("C174", filters, () => -1));
        Assert.Equal(0, cache.GetOrCreate("C174", filters, () => 0));
    }

    [Fact]
    public void Invalidate_RemovesOnlySpecifiedReportEntries()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var cache = new ReportTotalCountCache(memoryCache);
        var c21Calls = 0;
        cache.GetOrCreate("C21", new Dictionary<string, string?>(), () => ++c21Calls);
        cache.GetOrCreate("C22", new Dictionary<string, string?>(), () => 8);

        cache.Invalidate("C21");

        Assert.Equal(2, cache.GetOrCreate("C21", new Dictionary<string, string?>(), () => ++c21Calls));
        Assert.Equal(8, cache.GetOrCreate("C22", new Dictionary<string, string?>(), () => 9));
    }
}
