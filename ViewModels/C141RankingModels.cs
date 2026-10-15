namespace OpdAccrRptWeb.ViewModels;

public sealed record C141AdmissionKey(
    string AdmissionDate,
    string AdmissionTime,
    string AdmissionRoom,
    decimal AdmissionNumber);

public sealed record C141RankingSearch(
    string? MedicalRecordNumber,
    string? BedNumber,
    decimal? MinimumNetAmount)
{
    public string? AccountingDateRoc { get; init; }
    public DateOnly? OracleToday { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public sealed record C141QueryRequest(
    string? AccountingDate,
    string? MedicalRecordNumber,
    string? BedNumber,
    decimal? MinimumNetAmount,
    int PageNumber = 1,
    int PageSize = 10);

public sealed record C141ReportColumn(string Key, string Label, bool IsAmount = false);

public static class C141RankingColumns
{
    public static IReadOnlyList<C141ReportColumn> Visible { get; } = Array.AsReadOnly<C141ReportColumn>(
    [
        new("medicalRecordNumber", "病歷號"),
        new("patientName", "姓名"),
        new("bedNumber", "床號"),
        new("inpatientAdmissionDate", "入院日"),
        new("insuranceIdentity", "健保身分"),
        new("paymentType", "部分負擔類別"),
        new("receivableAmount", "應收金額", true),
        new("insuredReceivableAmount", "健保金額", true),
        new("displayPayTotal", "累計繳款", true),
        new("netAmount", "累計淨額", true),
        new("paymentDate", "最後繳費日"),
        new("noticeDate", "通知日期")
    ]);
}

public sealed record C141RankingSourceRow(
    C141AdmissionKey Key,
    string? MedicalRecordNumber,
    string? PatientName,
    string? BedNumber,
    string? InpatientAdmissionDate,
    string? InsuranceIdentity,
    string? InsuranceCategory,
    string? PaymentType,
    string? AdmissionSequence,
    string? InpatientSequence,
    string? AccountingDate,
    decimal? ReceivableAmount,
    decimal? InsuredReceivableAmount,
    string? PaymentDate,
    string? NoticeDate,
    decimal? PayTotal,
    bool HasPreferredContact);

public sealed record C141RankingRow(
    C141AdmissionKey Key,
    string? MedicalRecordNumber,
    string? PatientName,
    string? BedNumber,
    string? InpatientAdmissionDate,
    string? InsuranceIdentity,
    string? InsuranceCategory,
    string? PaymentType,
    string? AdmissionSequence,
    string? InpatientSequence,
    string? AccountingDate,
    decimal? ReceivableAmount,
    decimal? InsuredReceivableAmount,
    string? PaymentDate,
    string? NoticeDate,
    decimal? PayTotal,
    decimal NetAmount,
    bool HasPreferredContact)
{
    public decimal DisplayPayTotal => decimal.Floor((PayTotal ?? 0m) + 0.5m);
    public int? DaysSinceAdmission { get; init; }
    public bool IsAutomaticallySelected { get; init; }
    public string? AutomaticSelectionReason { get; init; }
}

public sealed record C141RecalculationCase(
    C141AdmissionKey Key,
    string? InsuranceIdentity,
    string? PartialPaymentCode,
    string? BedNumber);

public sealed record C141RecalculationTotals(
    decimal? AllTot1,
    decimal? AllTot2,
    decimal? AllTot3);

public sealed record C141CalculatedCase(
    C141RecalculationCase Case,
    C141RecalculationTotals Totals,
    bool IsCurrentAccountingDay);
