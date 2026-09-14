using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class InpatientAdvancePaymentBalanceReportViewModel
{
    [Description("入帳年月")] public string? EffectiveYearMonth { get; set; }
    [Description("病歷號")] public string? MedicalRecordNumber { get; set; }
    [Description("病患姓名")] public string? PatientName { get; set; }
    [Description("身分別")] public string? FinancialCategory { get; set; }
    [Description("預收餘額")] public decimal AdvancePaymentBalance { get; set; }
    [Description("序號")] public string? SequenceNumber { get; set; }
}
