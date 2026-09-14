using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class SafeNeedleReportViewModel
{
    [Description("分類")]
    public string? Category { get; set; }

    [Description("醫令日期")]
    public string? OrderDate { get; set; }

    [Description("醫令碼")]
    public string? OrderCode { get; set; }

    [Description("護理站／床號")]
    public string? BedNumber { get; set; }

    [Description("病歷號")]
    public string? MedicalRecordNumber { get; set; }

    [Description("病患姓名")]
    public string? PatientName { get; set; }
}
