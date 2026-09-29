using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public sealed class M1ReportRunStore(IMemoryCache cache, TimeProvider timeProvider) : IM1ReportRunStore
{
    internal const int MaximumRows = 20_000;
    internal static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    private readonly ConcurrentDictionary<string, byte> _knownKeys = new(StringComparer.Ordinal);

    public string Save(string actor, DateOnly reportDate, IReadOnlyList<M1DoctorDailyReportRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        if (rows.Count == 0) throw new ArgumentException("M1 空結果不建立快照。", nameof(rows));
        if (rows.Count > MaximumRows)
            throw new InvalidOperationException($"M1 查詢結果超過 {MaximumRows:N0} 筆上限，請洽系統管理人員。");

        string runId;
        do runId = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        while (!_knownKeys.TryAdd(runId, 0));

        var snapshot = new M1DoctorDailyReportSnapshot(
            runId, actor, reportDate, timeProvider.GetUtcNow(), rows.ToArray());
        cache.Set(runId, snapshot, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Lifetime,
            Size = Math.Max(1, rows.Count)
        }.RegisterPostEvictionCallback((key, _, _, _) =>
            _knownKeys.TryRemove(Convert.ToString(key) ?? string.Empty, out _)));
        return runId;
    }

    public bool TryGet(string runId, string actor, out M1DoctorDailyReportSnapshot snapshot)
    {
        snapshot = null!;
        if (string.IsNullOrWhiteSpace(runId) || runId.Length != 48 ||
            !runId.All(char.IsAsciiHexDigit)) return false;
        if (!cache.TryGetValue(runId, out M1DoctorDailyReportSnapshot? stored) ||
            stored is null || !string.Equals(stored.Actor, actor, StringComparison.Ordinal)) return false;
        snapshot = stored;
        return true;
    }
}

