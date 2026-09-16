using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C144DebtDetailReportViewModel
{
    [Description("診別")] public string? EncounterType { get; init; }
    [Description("就診日")] public string? VisitDate { get; init; }
    [Description("時段")] public string? VisitTime { get; init; }
    [Description("診間號")] public string? RoomNumber { get; init; }
    [Description("序號")] public decimal? SequenceNumber { get; init; }
    [Description("病歷號")] public string? MedicalRecordNumber { get; init; }
    [Description("病人姓名")] public string? PatientName { get; init; }
    [Description("出院日")] public string? DischargeDate { get; init; }
    [Description("科別代碼")] public string? SectionCode { get; init; }
    [Description("科別名稱")] public string? SectionName { get; init; }
    [Description("醫師代碼")] public string? DoctorId { get; init; }
    [Description("醫師名稱")] public string? DoctorName { get; init; }
    [Description("身分別")] public string? PatientIdentity { get; init; }
    [Description("尚欠")] public decimal? OutstandingAmount { get; init; }
    [Description("應繳自費總額")] public decimal? TotalSelfPayAmount { get; init; }
    [Description("一般身分自費金額")] public decimal? GeneralSelfPayAmount { get; init; }
    [Description("健保身分自費金額")] public decimal? InsuredSelfPayAmount { get; init; }
    [Description("部分負擔金額")] public decimal? CopaymentAmount { get; init; }
    [Description("藥品部分負擔金額")] public decimal? DrugCopaymentAmount { get; init; }
    [Description("優待金額")] public decimal? DiscountAmount { get; init; }
    [Description("記帳金額")] public decimal? OnAccountAmount { get; init; }
    [Description("一般身分醫療材料費")] public decimal? GeneralMaterialAmount { get; init; }
    [Description("健保身分醫療材料費")] public decimal? InsuredMaterialAmount { get; init; }
    [Description("一般身分手術費")] public decimal? GeneralSurgeryAmount { get; init; }
    [Description("健保身分手術費")] public decimal? InsuredSurgeryAmount { get; init; }
    [Description("一般身分麻醉費")] public decimal? GeneralAnesthesiaAmount { get; init; }
    [Description("健保身分麻醉費")] public decimal? InsuredAnesthesiaAmount { get; init; }
    [Description("一般身分藥品費")] public decimal? GeneralDrugAmount { get; init; }
    [Description("健保身分藥品費")] public decimal? InsuredDrugAmount { get; init; }
    [Description("一般身分掛號費")] public decimal? GeneralRegistrationAmount { get; init; }
    [Description("健保身分掛號費")] public decimal? InsuredRegistrationAmount { get; init; }
}

public sealed record C144Query(
    string StartDate,
    string EndDate,
    string Source,
    int PageNumber,
    int PageSize);

public static class C144Sources
{
    public const string OpdEr = "OpdEr";
    public const string Inpatient = "Inpatient";

    public static bool IsSupported(string? value) => value is OpdEr or Inpatient;
}
