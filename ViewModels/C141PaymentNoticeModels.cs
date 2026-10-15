namespace OpdAccrRptWeb.ViewModels;

public sealed record C141NoticeBasicData(
    string MedicalRecordNumber,
    string PatientName,
    string SectionCode,
    string SectionName,
    string BedNumber,
    string? InpatientAdmissionDate,
    string? PaymentType,
    string? InsuranceIdentity,
    string? InsuranceCategory,
    string? DoctorName,
    string? InpatientSequence,
    string? HealthCardNumber);

public sealed record C141NoticeCategory(
    string? DepartmentCode,
    string? DepartmentName,
    decimal? Amount1,
    decimal? Amount2,
    decimal? Subsidy1,
    decimal? Subsidy2,
    decimal? Subsidy3,
    decimal? Subsidy4,
    decimal? Subsidy5,
    decimal? Subsidy6);

public sealed record C141PaymentNoticeSource(
    C141AdmissionKey Key,
    C141NoticeBasicData Basic,
    IReadOnlyList<C141NoticeCategory> Categories,
    decimal? TemporaryAmount2,
    decimal? PayTotal,
    decimal? DiscountAmount,
    decimal PartialPaymentAmount = 0m,
    bool IsFourteenDayReadmission = false,
    bool PartialPaymentCalculationAvailable = false);

public sealed record C141PartialPaymentStay(
    string PaymentCode,
    string? AdmissionDate,
    string? DischargeDate,
    bool ExtendsFreePeriod,
    string? EmergencyAdmissionDate);

public sealed record C141PartialPaymentData(
    DateOnly OracleToday,
    IReadOnlyList<C141PartialPaymentStay> Stays,
    int PriorChargedDays,
    int PriorFreeDays,
    decimal FirstBandCap);

public sealed record C141NoticeCategoryLine(
    string? DepartmentCode,
    string? DepartmentName,
    decimal InsuranceAmount,
    decimal PatientAmount);

public sealed record C141NoticeTotals(
    decimal TotalAmountA,
    decimal InsuranceAmountB,
    decimal SubsidyAmountC,
    decimal PreferentialAmountD,
    decimal AmountDueE,
    decimal PrepaidAmountF,
    decimal RemainingAmountG);

public sealed record C141PaymentNoticeDocument(
    C141AdmissionKey Key,
    C141NoticeBasicData Basic,
    IReadOnlyList<C141NoticeCategoryLine> Categories,
    C141NoticeTotals Totals,
    string? Note1,
    string? Note2,
    string? Note3)
{
    public bool PartialPaymentCalculationAvailable { get; init; }
}

public sealed record C141NoticePreviewRequest(
    IReadOnlyList<C141AdmissionKey>? Cases,
    string? EntryDateTicket);

public sealed record C141NoticePreviewItem(
    C141AdmissionKey Key,
    string Status,
    string Message,
    string? Html);

public sealed record C141NoticePreviewPage(IReadOnlyList<C141NoticePreviewItem> Items);
