using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;
public sealed record C7CachedResult(C7ValidatedRequest Request, IReadOnlyList<C7ReportRow> Rows, IReadOnlyList<string> QueryIds);
public interface IC7ReportResultCache { bool TryGet(IReadOnlyDictionary<string,string?> f,out C7CachedResult r); void Set(IReadOnlyDictionary<string,string?> f,C7CachedResult r); }
public sealed class C7ReportResultCache : IC7ReportResultCache, IDisposable
{
    private readonly MemoryCache _cache=new(new MemoryCacheOptions{SizeLimit=100000});
    public bool TryGet(IReadOnlyDictionary<string,string?> f,out C7CachedResult r)=>_cache.TryGetValue(ReportTotalCountCache.CreateCacheKey("C7Result",f),out r!);
    public void Set(IReadOnlyDictionary<string,string?> f,C7CachedResult r){if(r.Rows.Count<=100000)_cache.Set(ReportTotalCountCache.CreateCacheKey("C7Result",f),r,new MemoryCacheEntryOptions{AbsoluteExpirationRelativeToNow=TimeSpan.FromMinutes(3),Size=Math.Max(1,r.Rows.Count)});}
    public void Dispose()=>_cache.Dispose();
}
