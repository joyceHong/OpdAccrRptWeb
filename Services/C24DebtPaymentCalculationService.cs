using System.Security.Cryptography;
using System.Text;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C24DebtPaymentCalculationService(ILogger<C24DebtPaymentCalculationService> logger)
    : IC24DebtPaymentCalculationService
{
    private static readonly IReadOnlyDictionary<string, string> DepartmentMap =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["0201"] = "11910", ["0281"] = "11920", ["0220"] = "11930",
            ["0221"] = "11930", ["0230"] = "11309"
        };

    public C24CanonicalResult Calculate(SearchReportCondition condition, C24RepositoryResult source,
        string? correlationId = null)
    {
        var runId = Guid.NewGuid().ToString("N");
        correlationId ??= runId;
        var rejected = 0;
        var warnings = 0;
        var completeDetails = condition.Mode == C24Modes.Billing
            ? CalculateBilling(condition, source.BillingRows)
            : CalculateAccounting(condition, source, runId, correlationId, ref rejected, ref warnings);
        var completeOrdered = Sort(completeDetails).ToList();
        var ordered = Sort(completeOrdered.Where(x => Matches(condition, x.RoomCategory, x.MedicalRecordNo))).ToList();
        var summaries = Summarize(ordered, condition.Mode!).ToList();
        var legacyDetails = condition.Mode == C24Modes.Accounting
            ? completeOrdered.Select(ToLegacyDetail).ToList() : [];
        var legacySummaries = condition.Mode == C24Modes.Accounting
            ? SummarizeLegacy(legacyDetails).ToList() : [];
        logger.LogInformation(
            "C24-COMPLETE RunId={RunId} CorrelationId={CorrelationId} ReportCode=C24 Mode={Mode} Source={Source} Stage=Complete Counts.Detail={DetailCount} Counts.Rejected={Rejected} Counts.Warning={Warning}",
            runId, correlationId, condition.Mode, condition.Source, ordered.Count, rejected, warnings);
        return new C24CanonicalResult
        {
            Details = ordered, Summaries = summaries, LegacyDetails = legacyDetails,
            LegacySummaries = legacySummaries, RejectionCount = rejected,
            WarningCount = warnings, RunId = runId, CorrelationId = correlationId
        };
    }

    private List<C24DebtPaymentDetail> CalculateAccounting(SearchReportCondition condition,
        C24RepositoryResult source, string runId, string correlationId, ref int rejected, ref int warnings)
    {
        var patients = source.Patients.GroupBy(x => x.VisitKey, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.Ordinal);
        var items = source.ChargeItems.GroupBy(x => x.ChargeItemCode, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First().ChargeItemName, StringComparer.Ordinal);
        var details = new List<C24DebtPaymentDetail>();
        foreach (var candidate in source.Candidates)
        {
            if (candidate.VisitDate > candidate.AccountingDate
                || SameLegacyDate(candidate.IDate, candidate.DcDate)) continue;
            if (!patients.TryGetValue(candidate.VisitKey, out var matches) || matches.Count == 0)
            {
                rejected++;
                logger.LogWarning("C24-ENRICH-001 RunId={RunId} CorrelationId={CorrelationId} Stage=Enrichment RecordFingerprint={Fingerprint} Action=Reject",
                    runId, correlationId, Fingerprint(candidate.VisitKey));
                continue;
            }
            if (matches.Count > 1)
            {
                logger.LogError("C24-ENRICH-002 RunId={RunId} CorrelationId={CorrelationId} Stage=Enrichment RecordFingerprint={Fingerprint} Action=Fail",
                    runId, correlationId, Fingerprint(candidate.VisitKey));
                throw new C24EnrichmentIntegrityException("C24 病患補值鍵不唯一。");
            }
            var patient = matches[0];
            if (patient.MedicalRecordNo is "C36979" or "1000000") continue;
            var category = Category(patient.RoomType, condition.Source!);
            string? itemName = null;
            if (candidate.ChargeItemCode is not null && !items.TryGetValue(candidate.ChargeItemCode, out itemName))
            {
                warnings++;
                logger.LogWarning("C24-ENRICH-003 RunId={RunId} CorrelationId={CorrelationId} Stage=Enrichment RecordFingerprint={Fingerprint} Action=RetainCode",
                    runId, correlationId, Fingerprint(candidate.SourceBusinessKey));
            }
            var querySub6 = candidate.SourceKind == C24SourceKind.Acc69 ? -candidate.SourceSub1 : candidate.QuerySub6;
            var querySub3 = candidate.SourceKind == C24SourceKind.Acc69 ? 0m : candidate.QuerySub3;
            var cancelled = IsAccountingDate(candidate.DcDate, candidate.AccountingDate);
            details.Add(new C24DebtPaymentDetail
            {
                AccountingDate = candidate.AccountingDate, VisitDate = candidate.VisitDate,
                RoomCategory = category, MedicalRecordNo = patient.MedicalRecordNo,
                PatientName = patient.PatientName, MaskedPhone = MaskPhone(patient.Phone),
                DepartmentCode = MapDepartment(patient.DepartmentCode ?? candidate.DepartmentCode),
                PayerClassCode = patient.PayerClassCode ?? candidate.PayerClassCode,
                CardSequenceNo = patient.CardSequenceNo, ChargeItemCode = candidate.ChargeItemCode,
                ChargeItemName = itemName, EventAmount = cancelled ? -querySub6 : querySub6,
                DiscountAmount = cancelled ? -querySub3 : querySub3, AmountDue = querySub6,
                CreatedBy = candidate.CreatedBy,
                SourceKind = candidate.SourceKind, SourceBusinessKey = candidate.SourceBusinessKey,
                LegacyRoomType = patient.RoomType, LegacyDischargeFlag = cancelled ? "1" : "0"
            });
        }
        return details;
    }

    private static List<C24DebtPaymentDetail> CalculateBilling(SearchReportCondition condition,
        IReadOnlyList<C24BillingRow> rows) => rows
        .Where(x => string.IsNullOrWhiteSpace(x.BackFlag) && x.Amount != 0m)
        .Select(x => new C24DebtPaymentDetail
        {
            AccountingDate = x.BillDate, VisitDate = x.BillDate,
            RoomCategory = Category(x.RoomType, condition.Source!), MedicalRecordNo = x.MedicalRecordNo,
            PatientName = x.PatientName ?? "", MaskedPhone = MaskPhone(x.Phone),
            DepartmentCode = MapDepartment(x.DepartmentCode), PayerClassCode = x.PayerClassCode,
            CardSequenceNo = x.CardSequenceNo, EventAmount = x.Amount, DiscountAmount = 0m,
            AmountDue = x.Amount, SourceKind = C24SourceKind.Billing,
            SourceBusinessKey = x.SourceBusinessKey
        }).ToList();

    public static IEnumerable<C24LegacySummaryRow> SummarizeLegacy(
        IEnumerable<C24LegacyDetailRow> details)
    {
        foreach (var group in details.GroupBy(x => new
                 { x.AccountingDate, x.RoomType, x.RoomTypeName }))
        {
            var debt = group.Where(x => x.ChargeItemCode is not null and not "69").ToList();
            var payment = group.Where(x => x.ChargeItemCode == "69").ToList();
            var debtAmount = debt.Sum(x => x.SignedAmount);
            var paymentSigned = payment.Sum(x => x.SignedAmount);
            yield return new C24LegacySummaryRow(group.Key.AccountingDate, group.Key.RoomType,
                group.Key.RoomTypeName, debtAmount, debt.Count, -paymentSigned, payment.Count,
                debtAmount + paymentSigned, debt.Count - payment.Count, null);
        }
    }

    private static C24LegacyDetailRow ToLegacyDetail(C24DebtPaymentDetail detail) => new(
        detail.AccountingDate, detail.VisitDate, detail.LegacyRoomType ?? RoomType(detail.RoomCategory),
        RoomTypeName(detail.RoomCategory), detail.MedicalRecordNo, null, detail.PatientName,
        detail.MaskedPhone, detail.DepartmentCode, detail.PayerClassCode, detail.CardSequenceNo,
        detail.ChargeItemCode, detail.ChargeItemName, detail.EventAmount, detail.DiscountAmount,
        detail.AmountDue, null, detail.CreatedBy, null, null, null, detail.LegacyDischargeFlag);

    public static IEnumerable<C24Summary> Summarize(IEnumerable<C24DebtPaymentDetail> details, string mode)
    {
        foreach (var group in details.GroupBy(x => x.RoomCategory).OrderBy(x => x.Key))
        {
            if (mode == C24Modes.Billing)
            {
                var amount = group.Sum(x => x.EventAmount);
                yield return new C24Summary(group.Key, amount, group.Count(), 0m, 0, amount, group.Count());
                continue;
            }
            var debt = group.Where(x => x.ChargeItemCode is not null and not "69").ToList();
            var payment = group.Where(x => x.ChargeItemCode == "69").ToList();
            var debtAmount = debt.Sum(x => x.EventAmount);
            var paymentSigned = payment.Sum(x => x.EventAmount);
            yield return new C24Summary(group.Key, debtAmount, debt.Count, -paymentSigned,
                payment.Count, debtAmount + paymentSigned, debt.Count - payment.Count);
        }
    }

    public static string? MaskPhone(string? phone)
    {
        if (phone is null) return null;
        var value = phone.Trim();
        if (value.Length > 10) value = value[..10];
        return value.Length <= 4 ? value : value[..4] + new string('*', value.Length - 4);
    }

    private static IEnumerable<C24DebtPaymentDetail> Sort(IEnumerable<C24DebtPaymentDetail> details) => details
        .OrderBy(x => x.RoomCategory).ThenBy(x => x.MedicalRecordNo, StringComparer.Ordinal)
        .ThenBy(x => x.DepartmentCode, StringComparer.Ordinal).ThenBy(x => x.PayerClassCode, StringComparer.Ordinal)
        .ThenBy(x => x.ChargeItemCode, StringComparer.Ordinal).ThenBy(x => x.SourceKind)
        .ThenBy(x => x.VisitDate).ThenBy(x => x.SourceBusinessKey, StringComparer.Ordinal);
    private static C24RoomCategory Category(string? roomType, string source) => source == C24Sources.Inpatient || roomType == "I"
        ? C24RoomCategory.Inpatient : roomType == "E" ? C24RoomCategory.Emergency : C24RoomCategory.Outpatient;
    private static string RoomType(C24RoomCategory category) => category switch
    {
        C24RoomCategory.Emergency => "E", C24RoomCategory.Inpatient => "I", _ => "O"
    };
    private static string RoomTypeName(C24RoomCategory category) => category switch
    {
        C24RoomCategory.Emergency => "急診", C24RoomCategory.Inpatient => "住院", _ => "門診"
    };
    private static bool Matches(SearchReportCondition condition, C24RoomCategory category, string mrn) =>
        (condition.RoomScope == C24RoomScopes.All
         || condition.RoomScope == C24RoomScopes.Emergency && category == C24RoomCategory.Emergency
         || condition.RoomScope == C24RoomScopes.NonEmergency && category != C24RoomCategory.Emergency)
        && (condition.MedicalRecordNo is null || condition.MedicalRecordNo == mrn);
    private static string? MapDepartment(string? code) => code is not null && DepartmentMap.TryGetValue(code, out var mapped) ? mapped : code;
    private static bool SameLegacyDate(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) && string.Equals(left.Trim(), right?.Trim(), StringComparison.Ordinal);
    private static bool IsAccountingDate(string? value, DateOnly day)
    {
        value = value?.Trim();
        return value == day.ToString("yyyyMMdd") || value == $"{day.Year - 1911:000}{day:MMdd}";
    }
    private static string Fingerprint(string value) => Convert.ToHexString(
        HMACSHA256.HashData(Encoding.UTF8.GetBytes("C24-log-fingerprint"), Encoding.UTF8.GetBytes(value)))[..16];
}

public sealed class C24EnrichmentIntegrityException(string message) : Exception(message);
