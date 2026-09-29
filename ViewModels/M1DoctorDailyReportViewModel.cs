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
    string Title,
    string QueryDate,
    string GeneratedAt,
    string ProgramId,
    string UserName,
    IReadOnlyList<M1DoctorDailyReportRow> Rows)
{
    public int SelfPayTotal => Rows.Sum(row => row.SelfPayCount);
    public int InsuranceTotal => Rows.Sum(row => row.InsuranceCount);
    public int MorningTotal => Rows.Sum(row => row.MorningCount);
    public int AfternoonTotal => Rows.Sum(row => row.AfternoonCount);
    public int NightTotal => Rows.Sum(row => row.NightCount);
    public int AppointmentTotal => Rows.Sum(row => row.AppointmentCount);
    public int GrandTotal => Rows.Sum(row => row.TotalCount);
}

public sealed record M1RenderedFile(byte[] Content, string ContentType, string FileName);

public sealed record ReportColumn(string Key, string Label);

