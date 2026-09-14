using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class SurgicalAccountingReportViewModel
{
    [Description("診別")]
    public string? EncounterType { get; set; }

    [Description("看診日期")]
    public string? EncounterDate { get; set; }

    [Description("時間")]
    public string? EncounterTime { get; set; }

    [Description("病歷號")]
    public string? MedicalRecordNumber { get; set; }

    [Description("病患姓名")]
    public string? PatientName { get; set; }

    [Description("身分")]
    public string? PatientIdentity { get; set; }

    [Description("付費方式")]
    public string? PaymentMethod { get; set; }

    [Description("醫師姓名")]
    public string? DoctorName { get; set; }

    [Description("科別")]
    public string? DepartmentCode { get; set; }

    [Description("手術碼")]
    public string? SurgicalOrderCode { get; set; }

    [Description("數量")]
    public decimal Quantity { get; set; }

    [Description("金額")]
    public decimal Amount { get; set; }

    [Description("科目")]
    public string? ChargeItem { get; set; }

    [Description("刀別")]
    public string? SurgicalClass { get; set; }

    [Description("手術比例科別")]
    public string? RatioDepartmentCode { get; set; }

    [Description("手術比例醫師")]
    public string? RatioDoctorNumber { get; set; }

    [Description("手術比例")]
    public decimal RatioQuantity { get; set; }

    [Description("手術比例金額")]
    public decimal RatioAmount { get; set; }
}
