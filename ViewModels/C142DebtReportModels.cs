using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed record C142Request(string? StartDate, string? EndDate, string? Source,
    string? SectionCode, int PageNumber = 1, int PageSize = 10, string? SnapshotId = null,
    string? LegacySectionCode = null);

public sealed record C142ExportRequest(string? SnapshotId, string? Mode);

public sealed class C142DebtRow
{
    [Description("病歷號")] public string? MedicalRecordNumber { get; init; }
    [Description("姓名")] public string? PatientName { get; init; }
    [Description("就診日")] public string? VisitDate { get; init; }
    [Description("出院日")] public string? DischargeDate { get; init; }
    [Description("自費總額")] public decimal SelfPayAmount { get; init; }
    [Description("已繳金額")] public decimal PaidAmount { get; init; }
    [Description("折扣金額")] public decimal DiscountAmount { get; init; }
    [Description("差額")] public decimal DifferenceAmount { get; init; }
    [Description("尚欠金額")] public decimal OutstandingAmount { get; init; }
    [Description("科別名稱")] public string? SectionName { get; init; }
    [Description("醫師")] public string? DoctorName { get; init; }
    [Description("床位號")] public string? BedNumber { get; init; }
    [Description("卡號")] public string? CardNumber { get; init; }
    public string VisitTime { get; init; } = "";
    public string RoomNumber { get; init; } = "";
    public decimal SequenceNumber { get; init; }
    public string EncounterKey => $"{VisitDate},{VisitTime},{RoomNumber},{SequenceNumber}";
}

public sealed record C142RowError(string EncounterKey, string Message);
public sealed record C142Snapshot(string Owner, string StartDate, string EndDate, string Source, string SectionCode,
    IReadOnlyList<C142DebtRow> Rows,
    IReadOnlyList<C142RowError> Errors, decimal TotalAmount);
