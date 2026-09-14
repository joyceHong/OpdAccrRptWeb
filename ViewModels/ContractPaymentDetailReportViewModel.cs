using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class ContractPaymentDetailReportViewModel
{
    [Description("合約代碼")] public string? ContractCode { get; set; }
    [Description("合約名稱")] public string? ContractName { get; set; }
    [Description("就醫日期")] public string? VisitDate { get; set; }
    [Description("病歷號")] public string? MedicalRecordNumber { get; set; }
    [Description("病人姓名")] public string? PatientName { get; set; }
    [Description("科別")] public string? DepartmentName { get; set; }
    [Description("醫師")] public string? DoctorName { get; set; }
    [Description("收款金額")] public decimal PaymentAmount { get; set; }
    [Description("收款人員")] public string? CashierUserId { get; set; }
}
