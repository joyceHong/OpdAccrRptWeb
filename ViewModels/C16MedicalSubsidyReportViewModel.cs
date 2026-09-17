using System.ComponentModel;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C16MedicalSubsidyReportViewModel
{
    [Description("姓名")] public string PatientName { get; init; } = string.Empty;
    [Description("身分證號")] public string PatientId { get; init; } = string.Empty;
    [Description("生日")] public string BirthDate { get; init; } = string.Empty;
    [Description("就診／住院日")] public string VisitDate { get; init; } = string.Empty;
    [Description("出院日")] public string DischargeDate { get; init; } = string.Empty;
    [Description("天數")] public int Days { get; init; }
    [Description("科別")] public string SectionName { get; init; } = string.Empty;
    [Description("ICD-10-CM")] public string Diagnosis { get; init; } = string.Empty;
    [Description("補助類別")] public string SubsidyType { get; init; } = string.Empty;
    [Description("掛號費")] public decimal Rl25 { get; init; }
    [Description("部分負擔")] public decimal Rl49 { get; init; }
    [Description("藥品部分負擔")] public decimal Rl49Drug { get; init; }
    [Description("其他自付額")] public decimal Rl50 { get; init; }
    [Description("費用總計")] public decimal Total { get; init; }
    public int EncounterOrdinal { get; init; }
}

public sealed record C16ReportResult(
    C16PreviewRequest Request,
    IReadOnlyList<C16ReportRow> AllRows,
    ReportDataAndColumns<C16MedicalSubsidyReportViewModel> Page);

public sealed record C16PreviewViewModel(string Title, string StartDate, string EndDate,
    C16Source Source, IReadOnlyList<C16ReportRow> Rows)
{
    public decimal Rl25Total => Rows.Sum(row => row.Rl25);
    public decimal OutpatientPartPayTotal => Rows.Sum(row => row.OutpatientPartPay);
    public decimal EmergencyPartPayTotal => Rows.Sum(row => row.EmergencyPartPay);
    public decimal DrugPartPayTotal => Rows.Sum(row => row.Rl49Drug);
    public decimal Rl49Total => Rows.Sum(row => row.Rl49);
    public decimal Rl50Total => Rows.Sum(row => row.Rl50);
    public decimal Total => Source == C16Source.Inpatient
        ? Rows.Sum(row => row.InpatientTotal) : Rows.Sum(row => row.OutpatientTotal);
}
