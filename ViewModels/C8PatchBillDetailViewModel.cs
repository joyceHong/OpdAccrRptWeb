using System.ComponentModel;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C8PatchBillDetailViewModel
{
    [Description("日期")] public string VisitDate { get; init; } = string.Empty;
    [Description("病歷號")] public string MedicalRecordNo { get; init; } = string.Empty;
    [Description("科別")] public string SectionCode { get; init; } = string.Empty;
    [Description("身分")] public string IdentityCode { get; init; } = string.Empty;
    [Description("輸入者")] public string ChargeUserId { get; init; } = string.Empty;
    [Description("批價碼")] public string ChargeCode { get; init; } = string.Empty;
    [Description("名稱")] public string ChargeName { get; init; } = string.Empty;
    [Description("健保單價")] public decimal InsuranceUnitPrice { get; init; }
    [Description("自費單價")] public decimal SelfPayUnitPrice { get; init; }
    [Description("數量")] public decimal Quantity { get; init; }
    [Description("健保金額")] public decimal InsuranceAmount { get; init; }
    [Description("自費金額")] public decimal SelfPayAmount { get; init; }

    public static C8PatchBillDetailViewModel From(C8ReportRow row) => new()
    {
        VisitDate = row.VisitDate, MedicalRecordNo = row.MedicalRecordNo,
        SectionCode = row.SectionCode, IdentityCode = row.IdentityCode,
        ChargeUserId = row.ChargeUserId, ChargeCode = row.ChargeCode,
        ChargeName = row.ChargeName, InsuranceUnitPrice = row.InsuranceUnitPrice,
        SelfPayUnitPrice = row.SelfPayUnitPrice, Quantity = row.Quantity,
        InsuranceAmount = row.InsuranceAmount, SelfPayAmount = row.SelfPayAmount
    };
}

public sealed record C8ReportResult(C8ValidatedRequest Request, IReadOnlyList<C8ReportRow> AllRows,
    ReportDataAndColumns<C8PatchBillDetailViewModel> Page, string QueryId);
public sealed record C8PreviewViewModel(string LoginUserId, string StartDate, string EndDate,
    string PrintedAt, IReadOnlyList<C8ReportRow> Rows);
