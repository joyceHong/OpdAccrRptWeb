using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public interface IC5ReportResultCache
{
    bool TryGet(IReadOnlyDictionary<string, string?> normalizedFilters, out C5CachedResult result);
    void Set(IReadOnlyDictionary<string, string?> normalizedFilters, C5CachedResult result);
}

public sealed record C5CachedResult(
    C5ValidatedRequest Request,
    IReadOnlyList<C5ReportRow> Rows,
    IReadOnlyList<C5QueryId> QueryIds);

public sealed class C5ReportResultCache : IC5ReportResultCache, IDisposable
{
    internal const int MaximumCachedRows = 100_000;
    internal static readonly TimeSpan EntryLifetime = TimeSpan.FromMinutes(3);
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = MaximumCachedRows });

    public bool TryGet(IReadOnlyDictionary<string, string?> normalizedFilters,
        out C5CachedResult result) => _cache.TryGetValue(CreateKey(normalizedFilters), out result!);

    public void Set(IReadOnlyDictionary<string, string?> normalizedFilters, C5CachedResult result)
    {
        if (result.Rows.Count > MaximumCachedRows) return;
        _cache.Set(CreateKey(normalizedFilters), result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = EntryLifetime,
            Size = Math.Max(1, result.Rows.Count)
        });
    }

    internal static string CreateKey(IReadOnlyDictionary<string, string?> normalizedFilters) =>
        ReportTotalCountCache.CreateCacheKey("C5Result", normalizedFilters);

    public void Dispose() => _cache.Dispose();
}
