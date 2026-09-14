using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class AssistiveDeviceDepositBalanceReportViewModel
{
    [Description("入帳年月")] public string? EffectiveYearMonth { get; set; }
    [Description("病歷號")] public string? MedicalRecordNumber { get; set; }
    [Description("病患姓名")] public string? PatientName { get; set; }
    [Description("身分別")] public string? FinancialCategory { get; set; }
    [Description("輔具保證金餘額")] public decimal AssistiveDeviceDepositBalance { get; set; }
    [Description("序號")] public string? SequenceNumber { get; set; }
}
