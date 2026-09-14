using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class CashierCashSummaryReportViewModel
{
    [Description("收款員代碼")] public string? CashierUserId { get; set; }
    [Description("收款員姓名")] public string? CashierUserName { get; set; }
    [Description("門診")] public decimal OutpatientAmount { get; set; }
    [Description("急診")] public decimal EmergencyAmount { get; set; }
    [Description("住院")] public decimal InpatientAmount { get; set; }
    [Description("HIS 小計")] public decimal HisSubtotalAmount { get; set; }
    [Description("現金平台")] public decimal CashPlatformAmount { get; set; }
    [Description("合計")] public decimal TotalAmount { get; set; }
}
