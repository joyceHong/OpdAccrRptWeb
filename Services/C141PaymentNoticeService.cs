using System.Data;
using System.Globalization;
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
        if (source is null)
            return null;
        if (source.Basic.InsuranceIdentity?.Trim() == "30")
        {
            bool isDayCare = source.Basic.BedNumber.Trim().StartsWith("01P", StringComparison.Ordinal);
            C141PartialPaymentData data = repository.LoadPartialPaymentData(
                key, source.Basic.MedicalRecordNumber, isDayCare, cancellationToken);
            if (data.Stays.Count == 0 && (isDayCare || source.Basic.MedicalRecordNumber.Trim() != "C36979"))
                throw new DataException("C141 部分負擔住院段資料不存在。");
            decimal amount = CalculatePartialPayment(data, key, isDayCare, cancellationToken);
            source = source with
            {
                PartialPaymentAmount = amount,
                IsFourteenDayReadmission = data.PriorChargedDays > 0,
                PartialPaymentCalculationAvailable = true
            };
        }
        return Calculate(source);
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

        IEnumerable<C141NoticeCategoryLine> categoryLines = source.Categories
            .Select(category => new C141NoticeCategoryLine(
                category.DepartmentCode,
                category.DepartmentName,
                LegacyInt(category.Amount1),
                CalculatePatientAmount(category, temporaryAmount2, isInsured)));
        if (isInsured)
            categoryLines = categoryLines.Append(new C141NoticeCategoryLine(
                "49", "基本部分負擔", 0m, source.PartialPaymentAmount));
        C141NoticeCategoryLine[] lines = categoryLines.ToArray();

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

    private decimal CalculatePartialPayment(
        C141PartialPaymentData data,
        C141AdmissionKey key,
        bool isDayCare,
        CancellationToken cancellationToken)
    {
        var periods = new List<(DateOnly Begin, DateOnly End, int Rate)>();
        if (isDayCare)
        {
            var dayCareStays = data.Stays.Where(stay => stay.PaymentCode == "000").ToArray();
            int totalDays = 0;
            DateOnly? firstAdmission = null;
            for (int index = 0; index < dayCareStays.Length; index++)
            {
                C141PartialPaymentStay stay = dayCareStays[index];
                DateOnly begin = ParseRocDate(index == 0 && !string.IsNullOrWhiteSpace(stay.EmergencyAdmissionDate)
                    ? stay.EmergencyAdmissionDate : stay.AdmissionDate);
                firstAdmission ??= begin;
                DateOnly end = stay.DischargeDate is null
                    ? data.OracleToday : ParseRocDate(stay.DischargeDate);
                if (end < begin)
                    throw new DataException("C141 部分負擔住院段日期逆序。");
                totalDays += end.DayNumber - begin.DayNumber;
            }
            // VB6 sums admission-inclusive/discharge-exclusive days, restores one final day,
            // then builds contiguous bands from the first admission date.
            if (totalDays > 0 && firstAdmission is DateOnly firstDay)
            {
                totalDays++;
                for (int offset = 0; offset < totalDays; offset++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int dayNumber = offset + 1;
                    int rate = dayNumber <= 30 ? 5 : dayNumber <= 90 ? 10
                        : dayNumber <= 180 ? 20 : 30;
                    AddDay(periods, firstDay.AddDays(offset), rate, false);
                }
            }
        }
        else
        {
            int chargedDays = data.PriorChargedDays;
            int freeDays = data.PriorFreeDays;
            int freeLimit = 30 + data.Stays.Count(stay => stay.ExtendsFreePeriod) * 30;
            int lastEligibleIndex = -1;
            for (int index = 0; index < data.Stays.Count; index++)
            {
                if (data.Stays[index].PaymentCode is "000" or "999")
                    lastEligibleIndex = index;
            }
            for (int index = 0; index < data.Stays.Count; index++)
            {
                C141PartialPaymentStay stay = data.Stays[index];
                if (stay.PaymentCode is not ("000" or "999"))
                    continue;
                bool hasChargedDay = false;
                DateOnly begin = ParseRocDate(index == 0 && !string.IsNullOrWhiteSpace(stay.EmergencyAdmissionDate)
                    ? stay.EmergencyAdmissionDate
                    : stay.AdmissionDate);
                DateOnly end = stay.DischargeDate is null
                    ? data.OracleToday : ParseRocDate(stay.DischargeDate);
                if (end < begin)
                    throw new DataException("C141 部分負擔住院段日期逆序。");
                if (end == begin)
                {
                    // The VB6 same-day branch assigns 10% without advancing its day counters.
                    AddDay(periods, begin, 10, true);
                    continue;
                }
                // VB6 excludes a stay's discharge day, then restores only the final day.
                DateOnly lastIncluded = index == lastEligibleIndex ? end : end.AddDays(-1);
                for (DateOnly day = begin; day <= lastIncluded; day = day.AddDays(1))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    int rate;
                    if (stay.PaymentCode == "999" && freeDays < freeLimit)
                    {
                        freeDays++;
                        rate = 0;
                    }
                    else
                    {
                        chargedDays++;
                        rate = chargedDays <= 30 ? 10 : chargedDays <= 60 ? 20 : 30;
                    }
                    if (rate != 0)
                    {
                        AddDay(periods, day, rate, !hasChargedDay);
                        hasChargedDay = true;
                    }
                }
            }
        }

        var amounts = new Dictionary<int, decimal>();
        foreach ((DateOnly begin, DateOnly end, int rate) in periods)
        {
            decimal insurance = repository.GetPeriodInsuranceAmount(
                key, begin, end, isDayCare, cancellationToken);
            decimal payment = LegacyInt(insurance * rate / 100m);
            amounts[rate] = amounts.GetValueOrDefault(rate) + payment;
        }
        if (isDayCare)
        {
            decimal firstTwoBands = amounts.GetValueOrDefault(5) + amounts.GetValueOrDefault(10);
            if (firstTwoBands > data.FirstBandCap)
            {
                amounts[5] = Math.Min(amounts.GetValueOrDefault(5), data.FirstBandCap);
                amounts[10] = Math.Max(0m, data.FirstBandCap - amounts[5]);
            }
        }
        else
        {
            amounts[10] = Math.Min(amounts.GetValueOrDefault(10), data.FirstBandCap);
        }
        return amounts.Values.Sum();
    }

    private static void AddDay(
        List<(DateOnly Begin, DateOnly End, int Rate)> periods,
        DateOnly day,
        int rate,
        bool beginsStay)
    {
        if (!beginsStay && periods.Count > 0)
        {
            var last = periods[^1];
            if (last.Rate == rate && last.End.AddDays(1) == day)
            {
                periods[^1] = (last.Begin, day, rate);
                return;
            }
        }
        periods.Add((day, day, rate));
    }

    private static DateOnly ParseRocDate(string? value)
    {
        if (value is null || value.Length < 7 || !int.TryParse(value[..3], out int year)
            || !DateOnly.TryParseExact($"{year + 1911:0000}{value[3..7]}", "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            throw new DataException("C141 部分負擔日期資料無效。");
        return date;
    }
}
