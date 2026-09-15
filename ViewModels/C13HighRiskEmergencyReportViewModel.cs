using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C13HighRiskEmergencyReportViewModel
{
    [Description("就診日期")]
    public string VisitDate { get; init; } = string.Empty;

    [Description("病歷號")]
    public string MedicalRecordNumber { get; init; } = string.Empty;

    [Description("姓名")]
    public string PatientName { get; init; } = string.Empty;

    [Description("就診身份")]
    public string IdentityName { get; init; } = string.Empty;

    [Description("部份負擔")]
    public string PartialPaymentName { get; init; } = string.Empty;

    [Description("生日")]
    public string BirthDate { get; init; } = string.Empty;

    [Description("身分證字號")]
    public string NationalId { get; init; } = string.Empty;

    [Description("地址")]
    public string Address { get; init; } = string.Empty;

    [Description("電話")]
    public string Telephone { get; init; } = string.Empty;

    [Description("急診床號")]
    public string EmergencyBedNumber { get; init; } = string.Empty;
}

public sealed class C13PreviewViewModel
{
    public required string StartDate { get; init; }
    public required string EndDate { get; init; }
    public required string GeneratedAt { get; init; }
    public required string GeneratedBy { get; init; }
    public required IReadOnlyList<C13HighRiskEmergencyReportViewModel> Rows { get; init; }
}
