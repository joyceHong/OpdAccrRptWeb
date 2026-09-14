using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C23ContractAccountingReportViewModel
{
    [Description("日期")] public string AccountingDate { get; set; } = string.Empty;
    [Description("合約代碼")] public string ContractCode { get; set; } = string.Empty;
    [Description("合約名稱")] public string ContractName { get; set; } = string.Empty;
    [Description("病歷號")] public string MedicalRecordNumber { get; set; } = string.Empty;
    [Description("病患姓名")] public string PatientName { get; set; } = string.Empty;
    [Description("科別")] public string DepartmentName { get; set; } = string.Empty;
    [Description("收費代碼")] public string BillingCode { get; set; } = string.Empty;
    [Description("收費名稱")] public string BillingName { get; set; } = string.Empty;
    [Description("原價")] public decimal GrossAmount { get; set; }
    [Description("記帳金額")] public decimal ContractAmount { get; set; }
    [Description("折扣金額")] public decimal DiscountAmount { get; set; }
    [Description("結果類型")] public string ResultType { get; set; } = C23ResultTypes.Detail;
}

public sealed record C23ContractOption(string Code, string Name);

internal sealed class C23SourceRow
{
    public string ChOp1Date { get; set; } = string.Empty;
    public string ChOp1Time { get; set; } = string.Empty;
    public string ChOp1Room { get; set; } = string.Empty;
    public int IntOp1No { get; set; }
    public string? IDate { get; set; }
    public string? DCDate { get; set; }
    public string ChOp4PFin2 { get; set; } = string.Empty;
    public string ChDctTypeName { get; set; } = string.Empty;
    public string ChOp4Dct { get; set; } = string.Empty;
    public string ChDctItemName { get; set; } = string.Empty;
    public string ChOp4CUser { get; set; } = string.Empty;
    public string ChOp4OrdNo { get; set; } = string.Empty;
    public decimal RlOp4Sub5 { get; set; }
    public decimal RlOp4Sub3 { get; set; }
    public string? ChOp4SPay { get; set; }
    public string? VchIrbNo { get; set; }
}

internal sealed class C23BasicRow
{
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
}

public static class C23ResultTypes
{
    public const string Detail = "Detail";
    public const string Summary = "Summary";
}
