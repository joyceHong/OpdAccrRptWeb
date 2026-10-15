using System.Globalization;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C141RecalculationService(IC141RecalculationRepository repository)
    : IC141RecalculationService
{
    public IReadOnlyList<C141CalculatedCase> CalculateClaimFlaggedCases(
        string accountingDateRoc,
        string oracleTodayRoc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountingDateRoc);
        ArgumentException.ThrowIfNullOrWhiteSpace(oracleTodayRoc);
        if (!IsRocDate(accountingDateRoc) || !IsRocDate(oracleTodayRoc))
            throw new ArgumentException("C141 重排行日期必須是七碼民國日期。", nameof(accountingDateRoc));

        bool isCurrentAccountingDay = string.Equals(
            accountingDateRoc,
            oracleTodayRoc,
            StringComparison.Ordinal);
        IReadOnlyList<C141RecalculationCase> cases = repository.GetClaimFlaggedCases(cancellationToken);
        var results = new List<C141CalculatedCase>(cases.Count);

        foreach (C141RecalculationCase item in cases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            C141RecalculationTotals totals = repository.GetTotals(
                item.Key,
                accountingDateRoc,
                isCurrentAccountingDay,
                cancellationToken);
            results.Add(new C141CalculatedCase(item, totals, isCurrentAccountingDay));
        }

        return results;
    }

    private static bool IsRocDate(string value) =>
        value.Length == 7
        && value.All(char.IsAsciiDigit)
        && int.TryParse(value.AsSpan(0, 3), NumberStyles.None, CultureInfo.InvariantCulture, out int year)
        && year > 0
        && DateOnly.TryParseExact(
            $"{year + 1911:0000}{value.AsSpan(3, 4)}",
            "yyyyMMdd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out _);
}
