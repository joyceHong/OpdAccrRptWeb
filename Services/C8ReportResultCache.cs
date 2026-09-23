using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public sealed record C8CachedResult(C8ValidatedRequest Request, IReadOnlyList<C8ReportRow> Rows);
public interface IC8ReportResultCache
{
    bool TryGet(string actor, C8ValidatedRequest request, out C8CachedResult result);
    void Set(string actor, C8ValidatedRequest request, C8CachedResult result);
}

public sealed class C8ReportResultCache : IC8ReportResultCache, IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 100000 });

    public bool TryGet(string actor, C8ValidatedRequest request, out C8CachedResult result) =>
        _cache.TryGetValue(Key(actor, request), out result!);

    public void Set(string actor, C8ValidatedRequest request, C8CachedResult result)
    {
        if (result.Rows.Count > 100000) return;
        _cache.Set(Key(actor, request), result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3),
            Size = Math.Max(1, result.Rows.Count)
        });
    }

    private static string Key(string actor, C8ValidatedRequest request) =>
        $"C8Result:{actor.Trim().ToUpperInvariant()}:{request.RocStartDate}:{request.RocEndDate}";
    public void Dispose() => _cache.Dispose();
}
