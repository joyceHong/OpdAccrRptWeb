using System.Text;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;

namespace OpdAccrRptWeb.Services;

public sealed class ReportTotalCountCache : IReportTotalCountCache
{
    internal static readonly TimeSpan EntryLifetime = TimeSpan.FromMinutes(3);

    private readonly IMemoryCache _memoryCache;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, byte>> _keysByReport = new();

    public ReportTotalCountCache(IMemoryCache memoryCache)
    {
        _memoryCache = memoryCache;
    }

    public int GetOrCreate(
        string reportCode,
        IReadOnlyDictionary<string, string?> normalizedFilters,
        Func<int> countFactory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reportCode);
        ArgumentNullException.ThrowIfNull(normalizedFilters);
        ArgumentNullException.ThrowIfNull(countFactory);

        var cacheKey = CreateCacheKey(reportCode, normalizedFilters);
        if (_memoryCache.TryGetValue(cacheKey, out int totalCount))
        {
            return totalCount;
        }

        totalCount = countFactory();
        if (totalCount < 0)
        {
            throw new InvalidOperationException("報表總筆數不可為負數。");
        }

        _memoryCache.Set(cacheKey, totalCount, CreateEntryOptions());
        _keysByReport.GetOrAdd(reportCode, _ => new ConcurrentDictionary<string, byte>())[cacheKey] = 0;
        return totalCount;
    }

    public void Invalidate(string reportCode)
    {
        if (!_keysByReport.TryRemove(reportCode, out var keys))
        {
            return;
        }
        foreach (var key in keys.Keys)
        {
            _memoryCache.Remove(key);
        }
    }

    internal static MemoryCacheEntryOptions CreateEntryOptions() => new()
    {
        AbsoluteExpirationRelativeToNow = EntryLifetime
    };

    internal static string CreateCacheKey(
        string reportCode,
        IReadOnlyDictionary<string, string?> normalizedFilters)
    {
        var builder = new StringBuilder("ReportTotalCount|");
        AppendComponent(builder, reportCode);

        foreach (var filter in normalizedFilters.OrderBy(item => item.Key, StringComparer.Ordinal))
        {
            AppendComponent(builder, filter.Key);
            AppendComponent(builder, filter.Value ?? string.Empty);
        }

        return builder.ToString();
    }

    private static void AppendComponent(StringBuilder builder, string value)
    {
        builder.Append(value.Length).Append(':').Append(value).Append('|');
    }
}
