using System.ComponentModel;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C7DailyChargeDetailViewModel
{
    [Description("日期")] public string VisitDate { get; init; } = "";
    [Description("時段")] public string VisitTime { get; init; } = "";
    [Description("科別")] public string SectionCode { get; init; } = "";
    [Description("診別")] public string EncounterLabel { get; init; } = "";
    [Description("狀態")] public string? Status { get; init; }
    [Description("批價碼")] public string ChargeCode { get; init; } = "";
    [Description("名稱")] public string ChargeName { get; init; } = "";
    [Description("單價")] public decimal UnitPrice { get; init; }
    [Description("數量")] public decimal Quantity { get; init; }
    [Description("金額")] public decimal Amount { get; init; }
    [Description("病歷號")] public string MedicalRecordNo { get; init; } = "";
    [Description("身分")] public string IdentityCode { get; init; } = "";
    [Description("輸入者")] public string InputUserId { get; init; } = "";
    [Description("輸入者姓名")] public string InputUserName { get; init; } = "";
    public static C7DailyChargeDetailViewModel From(C7ReportRow r) => new() { VisitDate=r.VisitDate,VisitTime=r.VisitTime,SectionCode=r.SectionCode,EncounterLabel=r.EncounterLabel,Status=r.Status,ChargeCode=r.ChargeCode,ChargeName=r.ChargeName,UnitPrice=r.UnitPrice,Quantity=r.Quantity,Amount=r.Amount,MedicalRecordNo=r.MedicalRecordNo,IdentityCode=r.IdentityCode,InputUserId=r.InputUserId,InputUserName=r.InputUserName };
}
public sealed record C7ReportResult(C7ValidatedRequest Request, IReadOnlyList<C7ReportRow> AllRows,
    ReportDataAndColumns<C7DailyChargeDetailViewModel> Page, IReadOnlyList<string> QueryIds);
public sealed record C7PreviewViewModel(string LoginUserId, string DataTime, string DataTime1,
    string DataTime2, string DataTime3, string NowTime, IReadOnlyList<C7ReportRow> Rows);
