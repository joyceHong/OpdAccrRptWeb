using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C141PaymentNoticeService(IC141PaymentNoticeRepository repository)
    : IC141PaymentNoticeService
{
    public DateTimeOffset GetOracleNow(CancellationToken cancellationToken = default) =>
        repository.GetOracleNow(cancellationToken);

    public C141PaymentNoticeDocument? Calculate(
        C141AdmissionKey key,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        C141PaymentNoticeSource? source = repository.LoadNoticeSource(key, cancellationToken);
        return source is null ? null : Calculate(source);
    }

    public void RecordBeforeDocument(
        C141PaymentNoticeDocument document,
        string userId,
        CancellationToken cancellationToken = default) =>
        repository.RecordBeforeDocument(document, userId, cancellationToken);

    public void RecordAfterDocument(
        C141AdmissionKey key,
        string entryDateRoc,
        CancellationToken cancellationToken = default) =>
        repository.RecordAfterDocument(key, entryDateRoc, cancellationToken);

    internal static C141PaymentNoticeDocument Calculate(C141PaymentNoticeSource source)
    {
        ArgumentNullException.ThrowIfNull(source);
        decimal temporaryAmount2 = source.TemporaryAmount2 ?? 0m;
        bool isInsured = source.Basic.InsuranceIdentity == "30";

        C141NoticeCategoryLine[] lines = source.Categories
            .Select(category => new C141NoticeCategoryLine(
                category.DepartmentCode,
                category.DepartmentName,
                LegacyInt(category.Amount1),
                CalculatePatientAmount(category, temporaryAmount2, isInsured)))
            .ToArray();

        decimal amountA = source.Categories.Sum(category =>
            LegacyInt((category.Amount1 ?? 0m) + (category.Amount2 ?? 0m)))
            + source.PartialPaymentAmount;
        decimal amountB = source.Categories.Sum(category => LegacyInt(category.Amount1));
        decimal amountC = source.Categories.Sum(category => LegacyInt(category.Subsidy5));
        decimal amountD = source.Categories.Sum(category => LegacyInt(category.Subsidy3));
        decimal amountE = amountA - amountB - amountC - amountD;
        decimal amountF = (source.PayTotal ?? 0m) + LegacyInt(source.DiscountAmount);
        decimal amountG = amountE - amountF;

        bool hasNoCard = string.IsNullOrWhiteSpace(source.Basic.HealthCardNumber);
        bool needsCardNote = source.Basic.InsuranceIdentity != "01" && hasNoCard;
        return new C141PaymentNoticeDocument(
            source.Key,
            source.Basic,
            lines,
            new C141NoticeTotals(amountA, amountB, amountC, amountD, amountE, amountF, amountG),
            needsCardNote ? "尚未讀取健保卡" : null,
            needsCardNote ? "繳費請攜卡" : null,
            source.IsFourteenDayReadmission ? "14日再返說明" : null)
        {
            PartialPaymentCalculationAvailable = source.PartialPaymentCalculationAvailable
        };
    }

    private static decimal CalculatePatientAmount(
        C141NoticeCategory category,
        decimal temporaryAmount2,
        bool isInsured)
    {
        decimal amount = LegacyInt(category.Amount2);
        if (!isInsured)
            return amount;

        if (category.DepartmentCode is "06" or "35")
            return 0m;
        return category.DepartmentCode == "02" ? amount + temporaryAmount2 : amount;
    }

    private static decimal LegacyInt(decimal? amount) => decimal.Floor((amount ?? 0m) + 0.5m);
}
