using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C15AssistiveDeviceDepositDetailReportViewModel
{
    [Description("租借日期")] public string VisitDate { get; init; } = string.Empty;
    [Description("病歷號")] public string MedicalRecordNumber { get; init; } = string.Empty;
    [Description("租借人")] public string PatientName { get; init; } = string.Empty;
    [Description("輔具編號")] public string DeviceNumber { get; init; } = string.Empty;
    [Description("租借期限")] public string RentalPeriod { get; init; } = string.Empty;
    [Description("金額一")] public decimal? Rl001 { get; init; }
    [Description("歸還日期")] public string ReturnDate { get; init; } = string.Empty;
    [Description("金額二")] public decimal? Rl002 { get; init; }
    [Description("金額四")] public decimal? Rl004 { get; init; }
    [Description("金額三")] public decimal? Rl003 { get; init; }
    public string Type { get; init; } = string.Empty;
    public int EncounterOrdinal { get; init; }
}

public sealed record C15GroupSummary(
    string Type,
    string Title,
    string Amount001Label,
    string Amount002Label,
    string Amount004Label,
    string Amount003Label,
    decimal Rl001Total,
    decimal Rl002Total,
    decimal Rl004Total,
    decimal Rl003Total);

public sealed record C15ReportSummary(IReadOnlyList<C15GroupSummary> Groups);
