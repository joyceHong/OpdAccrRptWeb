using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C143AccountingBalanceDebtReportViewModel
{
    [Description("診別")]
    public string? EncounterType { get; init; }

    [Description("病歷號")]
    public string? MedicalRecordNumber { get; init; }

    [Description("就診日")]
    public string? VisitDate { get; init; }

    [Description("序號")]
    public decimal? SequenceNumber { get; init; }

    [Description("離院日")]
    public string? EmergencyDepartureDate { get; init; }

    [Description("出院日")]
    public string? DischargeDate { get; init; }

    [Description("一般身分自費金額_會計")]
    public decimal? AccountingGeneralSelfPay { get; init; }

    [Description("健保身分自費金額_會計")]
    public decimal? AccountingInsuranceSelfPay { get; init; }

    [Description("部分負擔金額_會計")]
    public decimal? AccountingCopayment { get; init; }

    [Description("尚欠_會計")]
    public decimal? AccountingOutstanding { get; init; }

    [Description("尚欠(合併病歷號)_會計")]
    public decimal? AccountingMergedOutstanding { get; init; }

    [Description("欠款_批價")]
    public decimal? BillingDebt { get; init; }

    [Description("尚欠_批價")]
    public decimal? BillingOutstanding { get; init; }

    [Description("差額(尚欠_會計-尚欠_批價)")]
    public decimal? Difference { get; init; }

    public string ResultGroup { get; init; } = string.Empty;
}

public sealed record C143Query(
    string StartDate,
    string EndDate,
    string Source,
    string ReportType,
    int PageNumber,
    int PageSize);

public static class C143ResultGroups
{
    public const string OutpatientEmergency = "OutpatientEmergency";
    public const string Discharged = "Discharged";
    public const string InHospital = "InHospital";
}
