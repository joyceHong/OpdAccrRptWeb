using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class OutpatientReceivableBalanceReportViewModel
{
    [Description("病歷號")] public string? MedicalRecordNumber { get; set; }
    [Description("就診日期")] public string? VisitDate { get; set; }
    [Description("應收金額")] public decimal ReceivableAmount { get; set; }
}
