using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class ReferralMemberReportViewModel
{
    [Description("診院代碼")]
    public string? ClinicCode { get; set; }

    [Description("診院名稱")]
    public string? ClinicName { get; set; }

    [Description("身分證號")]
    public string? PatientIdentifier { get; set; }

    [Description("收案類別")]
    public string? CaseCategory { get; set; }

    [Description("病患姓名")]
    public string? PatientName { get; set; }

    [Description("病歷號")]
    public string? MedicalRecordNumber { get; set; }

    [Description("就診科別")]
    public string? EncounterDepartment { get; set; }

    [Description("病床號")]
    public string? BedNumber { get; set; }

    [Description("主治醫師")]
    public string? AttendingPhysician { get; set; }

    [Description("住院日期")]
    public string? AdmissionDate { get; set; }

    [Description("出院日期")]
    public string? DischargeDate { get; set; }

    [Description("診斷碼1")]
    public string? DiagnosisCode1 { get; set; }

    [Description("診斷碼2")]
    public string? DiagnosisCode2 { get; set; }

    [Description("診斷碼3")]
    public string? DiagnosisCode3 { get; set; }

    [Description("診斷碼名稱1")]
    public string? DiagnosisName1 { get; set; }

    [Description("診斷碼名稱2")]
    public string? DiagnosisName2 { get; set; }

    [Description("診斷碼名稱3")]
    public string? DiagnosisName3 { get; set; }

    [Description("網路同意")]
    public string? NetworkConsent { get; set; }

    [Description("完整回覆")]
    public string? CompleteResponse { get; set; }

    [Description("轉診註記")]
    public string? ReferralAnnotation { get; set; }
}
