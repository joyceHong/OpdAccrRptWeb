using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.ViewModels;

public sealed record M1DoctorDailyPagedResponse(
    string? RunId,
    IReadOnlyList<M1DoctorDailyReportRow> Data,
    IReadOnlyList<ReportColumn> Columns,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

public sealed record M1DoctorDailyPreviewViewModel(
    string HospitalName,
    string Title,
    string QueryDate,
    string GeneratedAt,
    string ProgramId,
    string UserName,
    IReadOnlyList<M1DoctorDailyReportRow> Rows)
{
    public IReadOnlyList<M1DoctorDailyOutputGroup> Groups =>
    [
        new("門診", Rows.Where(row => row.VisitType == "R").Select(M1DoctorDailyOutputRow.From).ToArray()),
        new("急診", Rows.Where(row => row.VisitType == "E").Select(M1DoctorDailyOutputRow.From).ToArray())
    ];

    public int SelfPayTotal => Rows.Sum(row => row.SelfPayCount);
    public int InsuranceTotal => Rows.Sum(row => row.InsuranceCount);
    public int MorningTotal => Rows.Sum(row => row.MorningCount);
    public int AfternoonTotal => Rows.Sum(row => row.AfternoonCount);
    public int NightTotal => Rows.Sum(row => row.NightCount);
    public int AppointmentTotal => Rows.Sum(row => row.AppointmentCount);
    public int GrandTotal => Rows.Sum(row => row.TotalCount);
    public decimal AppointmentRatio => M1DoctorDailyOutputRow.CalculateAppointmentRatio(
        AppointmentTotal, GrandTotal);
}

public sealed record M1DoctorDailyOutputGroup(
    string Label,
    IReadOnlyList<M1DoctorDailyOutputRow> Rows);

public sealed record M1DoctorDailyOutputRow(
    string SectionNo,
    string DoctorDisplay,
    int SelfPayCount,
    int InsuranceCount,
    int AppointmentCount,
    decimal AppointmentRatio,
    int TotalCount,
    int MorningCount,
    int AfternoonCount,
    int NightCount)
{
    public static M1DoctorDailyOutputRow From(M1DoctorDailyReportRow row) => new(
        row.SectionNo,
        string.Join(' ', new[] { row.DoctorNo, row.DoctorName }
            .Where(value => !string.IsNullOrWhiteSpace(value))),
        row.SelfPayCount,
        row.InsuranceCount,
        row.AppointmentCount,
        CalculateAppointmentRatio(row.AppointmentCount, row.TotalCount),
        row.TotalCount,
        row.MorningCount,
        row.AfternoonCount,
        row.NightCount);

    public static decimal CalculateAppointmentRatio(int appointmentCount, int totalCount) =>
        totalCount == 0 ? 0m : decimal.Divide(appointmentCount, totalCount);

    public static string FormatAppointmentRatio(decimal ratio) =>
        $"{ratio * 100m:0.00}%";
}

public sealed record M1RenderedFile(byte[] Content, string ContentType, string FileName);

public sealed record ReportColumn(string Key, string Label);
