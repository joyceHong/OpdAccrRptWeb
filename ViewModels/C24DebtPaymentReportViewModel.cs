using System.ComponentModel;
using System.Text.Json.Serialization;

namespace OpdAccrRptWeb.ViewModels;

[JsonConverter(typeof(JsonStringEnumConverter<C24RoomCategory>))]
public enum C24RoomCategory { Outpatient, Emergency, Inpatient }
[JsonConverter(typeof(JsonStringEnumConverter<C24SourceKind>))]
public enum C24SourceKind { Drg, Ord, Acc69, Billing }

public sealed record C24Candidate(
    DateOnly AccountingDate, DateOnly VisitDate, string VisitKey, string SourceBusinessKey,
    string? MedicalRecordNo, string? DepartmentCode, string? PayerClassCode,
    string? ChargeItemCode, string? IDate, string? DcDate, decimal QuerySub6,
    decimal QuerySub3, decimal SourceSub1, C24SourceKind SourceKind, string? CreatedBy = null);

public sealed record C24PatientEnrichment(
    string VisitKey, string MedicalRecordNo, string PatientName, string? Phone,
    string? RoomType, string? DepartmentCode, string? PayerClassCode, string? CardSequenceNo);

public sealed record C24ChargeItemEnrichment(string ChargeItemCode, string? ChargeItemName);

public sealed record C24BillingRow(
    DateOnly BillDate, string VisitKey, string MedicalRecordNo, string? PatientName,
    string? Phone, string? RoomType, string? DepartmentCode, string? PayerClassCode,
    string? CardSequenceNo, decimal Amount, string? BackFlag, string SourceBusinessKey);

public sealed class C24RepositoryResult
{
    public required IReadOnlyList<C24Candidate> Candidates { get; init; }
    public required IReadOnlyList<C24PatientEnrichment> Patients { get; init; }
    public required IReadOnlyList<C24ChargeItemEnrichment> ChargeItems { get; init; }
    public required IReadOnlyList<C24BillingRow> BillingRows { get; init; }
}

public sealed class C24DebtPaymentDetail
{
    [Description("會計/顯示日期")] public DateOnly AccountingDate { get; init; }
    [Description("就診日")] public DateOnly VisitDate { get; init; }
    [Description("診別")] public C24RoomCategory RoomCategory { get; init; }
    [Description("病歷號")] public string MedicalRecordNo { get; init; } = "";
    [Description("姓名")] public string PatientName { get; init; } = "";
    [Description("電話")] public string? MaskedPhone { get; init; }
    [Description("科別")] public string? DepartmentCode { get; init; }
    [Description("身分")] public string? PayerClassCode { get; init; }
    [Description("卡序號")] public string? CardSequenceNo { get; init; }
    [Description("科目")] public string? ChargeItemCode { get; init; }
    [Description("科目名稱")] public string? ChargeItemName { get; init; }
    [Description("事件金額")] public decimal EventAmount { get; init; }
    [Description("折扣")] public decimal DiscountAmount { get; init; }
    [Description("應補繳金額")] public decimal AmountDue { get; init; }
    [Description("建檔人員")] public string? CreatedBy { get; init; }
    [Description("來源")] public C24SourceKind SourceKind { get; init; }
    public string SourceBusinessKey { get; init; } = "";
    [JsonIgnore] public string? LegacyRoomType { get; init; }
    [JsonIgnore] public string? LegacyDischargeFlag { get; init; }
}

public sealed record C24Summary(
    C24RoomCategory RoomCategory, decimal DebtAmount, int DebtCount,
    decimal PaymentAmount, int PaymentCount, decimal OutstandingAmount, int OutstandingCount);

public sealed record C24LegacyDetailRow(
    DateOnly AccountingDate, DateOnly VisitDate, string RoomType, string RoomTypeName,
    string MedicalRecordNo, string? AlternateMedicalRecordNo, string PatientName,
    string? Phone, string? DepartmentCode, string? PayerClassCode, string? CardSequenceNo,
    string? ChargeItemCode, string? ChargeItemName, decimal SignedAmount,
    decimal SignedDiscount, decimal AmountDue, string? OtherDate, string? CreatedBy,
    string? DeletedBy, int? Flag, string? DateFlag, string? DischargeFlag);

public sealed record C24LegacySummaryRow(
    DateOnly AccountingDate, string RoomType, string RoomTypeName,
    decimal DebtAmount, decimal DebtCount, decimal PaymentAmount, decimal PaymentCount,
    decimal OutstandingAmount, decimal OutstandingCount, string? DateFlag);

public sealed record C24LegacyResult(
    IReadOnlyList<C24LegacyDetailRow> Details,
    IReadOnlyList<C24LegacySummaryRow> Summaries);

public sealed class C24CanonicalResult
{
    public required IReadOnlyList<C24DebtPaymentDetail> Details { get; init; }
    public required IReadOnlyList<C24Summary> Summaries { get; init; }
    public IReadOnlyList<C24LegacyDetailRow> LegacyDetails { get; init; } = [];
    public IReadOnlyList<C24LegacySummaryRow> LegacySummaries { get; init; } = [];
    public int RejectionCount { get; init; }
    public int WarningCount { get; init; }
    public required string RunId { get; init; }
    public required string CorrelationId { get; init; }
}

public sealed class C24DebtPaymentResponse
{
    public required IReadOnlyList<C24DebtPaymentDetail> Data { get; init; }
    public required IReadOnlyList<C24Summary> Summary { get; init; }
    public required int TotalCount { get; init; }
    public required int PageNumber { get; init; }
    public required int PageSize { get; init; }
    public required int TotalPages { get; init; }
}
