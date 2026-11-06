using System.Globalization;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C141RecalculationService(
    IC141RecalculationRepository repository,
    ILogger<C141RecalculationService> logger,
    IC141PaymentNoticeService? paymentNoticeService = null)
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

    public C141RecalculationResult Recalculate(string accountingDateRoc,
        string oracleTodayRoc, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accountingDateRoc);
        ArgumentException.ThrowIfNullOrWhiteSpace(oracleTodayRoc);
        if (!IsRocDate(accountingDateRoc) || !IsRocDate(oracleTodayRoc))
            throw new ArgumentException("C141 重排行日期必須是七碼民國日期。", nameof(accountingDateRoc));

        bool isCurrentAccountingDay = string.Equals(
            accountingDateRoc, oracleTodayRoc, StringComparison.Ordinal);
        IReadOnlyList<C141RecalculationCase> cases = repository.GetClaimFlaggedCases(cancellationToken);
        int updated = 0;
        int skipped = 0;
        int failed = 0;
        foreach (C141RecalculationCase item in cases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                C141RecalculationTotals totals = repository.GetTotals(
                    item.Key, accountingDateRoc, isCurrentAccountingDay, cancellationToken);
                decimal first = totals.AllTot1 ?? 0m;
                decimal second = totals.AllTot2 ?? 0m;
                if (first <= 0m && second <= 0m)
                {
                    skipped++;
                    continue;
                }

                decimal partialPayment = 0m;
                if (isCurrentAccountingDay && item.InsuranceIdentity?.Trim() == "30")
                {
                    partialPayment = (paymentNoticeService
                        ?? throw new InvalidOperationException("C141 部分負擔服務無法計算重新排行金額。"))
                        .CalculatePartialPaymentForRanking(item.Key,
                            item.MedicalRecordNumber, item.BedNumber, cancellationToken);
                }

                decimal receivable = LegacyInt(first) + partialPayment;
                decimal insured = LegacyInt(totals.AllTot3 ?? 0m);
                repository.UpdateAmounts(item.Key, receivable, insured, cancellationToken);
                updated++;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                failed++;
                logger.LogError(exception,
                    "C141 重新排行個案失敗。批次序號: {CaseIndex}; ExceptionType: {ExceptionType}",
                    updated + skipped + failed, exception.GetType().Name);
            }
        }
        return new C141RecalculationResult(cases.Count, updated, skipped, failed);
    }

    private static decimal LegacyInt(decimal value) => decimal.Floor(value + 0.5m);

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
