using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed record C9CachedResult(C9ValidatedRequest Request, IReadOnlyList<C9ReportRow> Rows);

public interface IC9ReportResultCache
{
    bool TryGet(string actor, C9ValidatedRequest request, out C9CachedResult result);
    void Set(string actor, C9ValidatedRequest request, C9CachedResult result);
}

public sealed class C9ReportResultCache : IC9ReportResultCache, IDisposable
{
    private readonly MemoryCache _cache = new(new MemoryCacheOptions { SizeLimit = 100000 });

    public bool TryGet(string actor, C9ValidatedRequest request, out C9CachedResult result) =>
        _cache.TryGetValue(Key(actor, request), out result!);

    public void Set(string actor, C9ValidatedRequest request, C9CachedResult result)
    {
        if (result.Rows.Count > 100000) return;
        _cache.Set(Key(actor, request), result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(3),
            Size = Math.Max(1, result.Rows.Count)
        });
    }

    private static string Key(string actor, C9ValidatedRequest request) =>
        $"C9Result:{C9Sql.ResourceId}:{actor.Trim().ToUpperInvariant()}:{request.StartDate:yyyyMMdd}:{request.EndDate:yyyyMMdd}";

    public void Dispose() => _cache.Dispose();
}
