using System.ComponentModel;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C9MaterialAccountingMonthlyViewModel
{
    [Description("日期")] public string VisitDate { get; init; } = string.Empty;
    [Description("病歷號")] public string MedicalRecordNo { get; init; } = string.Empty;
    [Description("姓名")] public string PatientName { get; init; } = string.Empty;
    [Description("醫師")] public string DoctorName { get; init; } = string.Empty;
    [Description("科別")] public string SectionName { get; init; } = string.Empty;
    [Description("記帳員")] public string ChargeUserId { get; init; } = string.Empty;
    [Description("收費科目代碼")] public string ChargeCode { get; init; } = string.Empty;
    [Description("收費科目名稱")] public string ChargeName { get; init; } = string.Empty;
    [Description("折扣額")] public decimal DiscountAmount { get; init; }
    [Description("應付金額")] public decimal PayableAmount { get; init; }
    [Description("總金額")] public decimal TotalAmount { get; init; }

    public static C9MaterialAccountingMonthlyViewModel From(C9ReportRow row) => new()
    {
        VisitDate = row.VisitDate,
        MedicalRecordNo = row.MedicalRecordNo,
        PatientName = row.PatientName,
        DoctorName = row.DoctorName,
        SectionName = row.SectionName,
        ChargeUserId = row.ChargeUserId,
        ChargeCode = row.ChargeCode,
        ChargeName = row.ChargeName,
        DiscountAmount = row.DiscountAmount,
        PayableAmount = row.PayableAmount,
        TotalAmount = row.TotalAmount
    };
}

public sealed record C9ReportResult(
    C9ValidatedRequest Request,
    IReadOnlyList<C9ReportRow> AllRows,
    ReportDataAndColumns<C9MaterialAccountingMonthlyViewModel> Page,
    string QueryId);

public sealed record C9ReportGroup(
    string ChargeCode,
    IReadOnlyList<C9ReportRow> Rows,
    decimal TotalAmount,
    decimal DiscountAmount,
    decimal PayableAmount);

public sealed record C9PreviewViewModel(
    string LoginUserId,
    string StartDate,
    string EndDate,
    string PrintedAt,
    string ProgramNo,
    string ReportNo,
    IReadOnlyList<C9ReportGroup> Groups);
