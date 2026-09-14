using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class InpatientReceivableBalanceReportViewModel
{
    [Description("病歷號")] public string? MedicalRecordNumber { get; set; }
    [Description("住院日期")] public string? AdmissionDate { get; set; }
    [Description("住院時間")] public string? AdmissionTime { get; set; }
    [Description("病房")] public string? Room { get; set; }
    [Description("住院序號")] public int AdmissionNumber { get; set; }
    [Description("自費金額")] public decimal SelfPayAmount { get; set; }
    [Description("申報金額")] public decimal ClaimAmount { get; set; }
    [Description("部分負擔金額")] public decimal CopaymentAmount { get; set; }
}
