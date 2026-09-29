using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public sealed class M2ReportRunStore(IMemoryCache cache, TimeProvider timeProvider) : IM2ReportRunStore
{
    internal const int MaximumRows = 20_000;
    internal static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    private readonly ConcurrentDictionary<string, byte> _knownKeys = new(StringComparer.Ordinal);

    public string Save(string actor, DateOnly reportMonth, M2CalculationBasis calculationBasis,
        M2VisitScope visitScope, M2TimeSlot timeSlot, IReadOnlyList<M2DoctorMonthlyReportRow> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        if (rows.Count == 0) throw new ArgumentException("M2 空結果不建立快照。", nameof(rows));
        if (rows.Count > MaximumRows)
            throw new InvalidOperationException($"M2 查詢結果超過 {MaximumRows:N0} 筆上限，請洽系統管理人員。");
        string runId;
        do runId = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        while (!_knownKeys.TryAdd(runId, 0));
        M2DoctorMonthlyReportRow[] copy = rows.Select(row => M2DoctorMonthlyReportRow.Create(
            row.SectionNo, row.SectionName, row.DoctorNo, row.DoctorName, row.DailyCounts)).ToArray();
        long checksum = copy.SelectMany(row => row.DailyCounts).Aggregate(0L, checked((sum, value) => sum + value));
        var snapshot = new M2DoctorMonthlyReportSnapshot(runId, actor, reportMonth,
            calculationBasis, visitScope, timeSlot, timeProvider.GetUtcNow(), copy, checksum);
        cache.Set(runId, snapshot, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Lifetime,
            Size = Math.Max(1, copy.Length)
        }.RegisterPostEvictionCallback((key, _, _, _) =>
            _knownKeys.TryRemove(Convert.ToString(key) ?? string.Empty, out _)));
        return runId;
    }

    public bool TryGet(string runId, string actor, out M2DoctorMonthlyReportSnapshot snapshot)
    {
        snapshot = null!;
        if (string.IsNullOrWhiteSpace(runId) || runId.Length != 48 || !runId.All(char.IsAsciiHexDigit))
            return false;
        if (!cache.TryGetValue(runId, out M2DoctorMonthlyReportSnapshot? stored) || stored is null ||
            !string.Equals(stored.Actor, actor, StringComparison.Ordinal)) return false;
        snapshot = stored;
        return true;
    }
}
