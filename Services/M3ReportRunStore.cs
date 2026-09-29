using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public sealed class M3ReportRunStore(IMemoryCache cache, TimeProvider timeProvider) : IM3ReportRunStore
{
    internal const int MaximumRows = 20_000;
    internal static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);
    private readonly ConcurrentDictionary<string, byte> _knownKeys = new(StringComparer.Ordinal);

    public string Save(string actor, DateOnly reportDate,
        IReadOnlyList<M3OpdEmergencyDailyReportRow> rows, M3NineKpis nineKpis,
        string? sourceWatermark = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        if (rows.Count > MaximumRows)
            throw new InvalidOperationException($"M3 查詢結果超過 {MaximumRows:N0} 筆上限，請洽系統管理人員。");
        string runId;
        do runId = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();
        while (!_knownKeys.TryAdd(runId, 0));
        M3OpdEmergencyDailyReportRow[] copy = rows.ToArray();
        long checksum = copy.Aggregate(0L, (sum, row) => checked(sum + RowChecksum(row)));
        checksum = checked(checksum + KpiChecksum(nineKpis));
        var snapshot = new M3OpdEmergencyDailyReportSnapshot(runId, actor, reportDate,
            timeProvider.GetUtcNow(), copy, nineKpis, checksum, sourceWatermark);
        cache.Set(runId, snapshot, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = Lifetime,
            Size = Math.Max(1, copy.Length)
        }.RegisterPostEvictionCallback((key, _, _, _) =>
            _knownKeys.TryRemove(Convert.ToString(key) ?? string.Empty, out _)));
        return runId;
    }

    public bool TryGet(string runId, string actor, out M3OpdEmergencyDailyReportSnapshot snapshot)
    {
        snapshot = null!;
        if (string.IsNullOrWhiteSpace(runId) || runId.Length != 48 || !runId.All(char.IsAsciiHexDigit))
            return false;
        if (!cache.TryGetValue(runId, out M3OpdEmergencyDailyReportSnapshot? stored) || stored is null ||
            !string.Equals(stored.Actor, actor, StringComparison.Ordinal)) return false;
        snapshot = stored;
        return true;
    }

    private static long RowChecksum(M3OpdEmergencyDailyReportRow row) => checked(
        row.Op1SQty + row.Op1HQty + row.Op2SQty + row.Op2HQty + row.Em1SQty + row.Em1HQty +
        row.Em2SQty + row.Em2HQty + row.OESQty + row.OEHQty + row.Op1MonQty + row.Op2MonQty +
        row.Em1MonQty + row.Em2MonQty + row.OEMonSQty + row.OEMonHQty + row.Op1YearQty +
        row.Op2YearQty + row.Em1YearQty + row.Em2YearQty + row.OEYearSQty + row.OEYearHQty);

    private static long KpiChecksum(M3NineKpis value) => checked(value.OutpatientMorning +
        value.OutpatientAfternoon + value.OutpatientNight + value.EmergencyDay + value.EmergencyEvening +
        value.EmergencyNight + value.Appointment + value.NoShow + value.NetAppointment);
}
