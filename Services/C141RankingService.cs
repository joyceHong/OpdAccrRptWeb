using System.Globalization;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C141RankingService(IC141RankingRepository repository) : IC141RankingService
{
    public DateOnly GetOracleToday(CancellationToken cancellationToken = default) =>
        repository.GetOracleToday(cancellationToken);

    public IReadOnlyList<C141RankingRow> Query(
        C141RankingSearch search,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(search);
        C141RankingSearch normalized = Normalize(search);
        IReadOnlyList<C141RankingSourceRow> sourceRows = repository.Query(normalized, cancellationToken);

        return sourceRows
            .Select(row => Project(row, normalized.OracleToday))
            .Where(row => normalized.MinimumNetAmount is null || row.NetAmount > normalized.MinimumNetAmount.Value)
            .OrderByDescending(row => row.NetAmount)
            .ThenBy(row => row.Key.AdmissionDate, StringComparer.Ordinal)
            .ThenBy(row => row.Key.AdmissionTime, StringComparer.Ordinal)
            .ThenBy(row => row.Key.AdmissionRoom, StringComparer.Ordinal)
            .ThenBy(row => row.Key.AdmissionNumber)
            .ToArray();
    }

    public int GetCount(C141RankingSearch search, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(search);
        return repository.GetCount(Normalize(search), cancellationToken);
    }

    public IReadOnlyList<C141RankingRow> GetPage(
        C141RankingSearch search,
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(search);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        C141RankingSearch normalized = Normalize(search);
        return repository.GetPage(normalized, offset, pageSize, cancellationToken)
            .Select(row => Project(row, normalized.OracleToday))
            .ToArray();
    }

    private static C141RankingSearch Normalize(C141RankingSearch search)
    {
        string? medicalRecordNumber = NormalizeFilter(
            search.MedicalRecordNumber,
            C141OracleParameters.MedicalRecordNumberLength,
            nameof(search.MedicalRecordNumber))?.ToUpperInvariant();
        string? bedNumber = NormalizeFilter(search.BedNumber, 8, nameof(search.BedNumber));
        return search with { MedicalRecordNumber = medicalRecordNumber, BedNumber = bedNumber };
    }

    private static string? NormalizeFilter(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string normalized = value.Trim();
        if (normalized.Any(character => character > 0x7f))
            throw new ArgumentException("C141 查詢識別值必須是 ASCII。", parameterName);
        if (normalized.Length > maxLength)
            throw new ArgumentException("C141 查詢值超過欄位長度。", parameterName);
        return normalized;
    }

    private static C141RankingRow Project(C141RankingSourceRow row, DateOnly? oracleToday)
    {
        decimal netAmount = (row.ReceivableAmount ?? 0m) - (row.PayTotal ?? 0m);
        C141AutomaticSelection selection = C141AutomaticNoticeSelection.Evaluate(row, netAmount, oracleToday);
        return new C141RankingRow(
            row.Key,
            row.MedicalRecordNumber,
            row.PatientName,
            row.BedNumber,
            row.InpatientAdmissionDate,
            row.InsuranceIdentity,
            row.InsuranceCategory,
            row.PaymentType,
            row.AdmissionSequence,
            row.InpatientSequence,
            row.AccountingDate,
            row.ReceivableAmount,
            row.InsuredReceivableAmount,
            row.PaymentDate,
            row.NoticeDate,
            row.PayTotal,
            netAmount,
            row.HasPreferredContact)
        {
            DaysSinceAdmission = selection.DaysSinceAdmission,
            IsAutomaticallySelected = selection.IsSelected,
            AutomaticSelectionReason = selection.Reason
        };
    }
}

internal readonly record struct C141AutomaticSelection(int? DaysSinceAdmission, bool IsSelected, string? Reason);

internal static class C141AutomaticNoticeSelection
{
    internal static C141AutomaticSelection Evaluate(
        C141RankingSourceRow row,
        decimal netAmount,
        DateOnly? oracleToday)
    {
        if (oracleToday is null || !TryParseRocDate(row.InpatientAdmissionDate, out DateOnly admissionDate))
            return new(null, false, null);

        int daysSinceAdmission = oracleToday.Value.DayNumber - admissionDate.DayNumber;
        bool qualifiesForSevenDayRule = daysSinceAdmission >= 7 && netAmount >= 20_000m;
        bool qualifiesForThirtyDayRule = daysSinceAdmission >= 30 && netAmount >= 5_000m;
        if (!qualifiesForSevenDayRule && !qualifiesForThirtyDayRule)
            return new(daysSinceAdmission, false, null);

        if (!string.IsNullOrWhiteSpace(row.NoticeDate))
        {
            if (!TryParseRocDate(row.NoticeDate, out DateOnly noticeDate)
                || oracleToday.Value.DayNumber - noticeDate.DayNumber <= 4)
                return new(daysSinceAdmission, false, null);
        }

        string reason = qualifiesForSevenDayRule
            ? "住院滿 7 日且淨額達 20,000 元"
            : "住院滿 30 日且淨額達 5,000 元";
        return new(daysSinceAdmission, true, reason);
    }

    private static bool TryParseRocDate(string? value, out DateOnly date)
    {
        date = default;
        if (value is null || value.Length < 7)
            return false;

        ReadOnlySpan<char> rocDate = value.AsSpan(0, 7);
        foreach (char character in rocDate)
        {
            if (!char.IsAsciiDigit(character))
                return false;
        }

        if (!int.TryParse(rocDate[..3], NumberStyles.None, CultureInfo.InvariantCulture, out int rocYear)
            || !int.TryParse(rocDate[3..5], NumberStyles.None, CultureInfo.InvariantCulture, out int month)
            || !int.TryParse(rocDate[5..7], NumberStyles.None, CultureInfo.InvariantCulture, out int day))
            return false;

        string gregorianDate = $"{rocYear + 1911:0000}{month:00}{day:00}";
        return DateOnly.TryParseExact(
            gregorianDate,
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }
}
