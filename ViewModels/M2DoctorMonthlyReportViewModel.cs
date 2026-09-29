using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.ViewModels;

public sealed record M2DoctorMonthlyPagedResponse(
    string? RunId,
    IReadOnlyList<M2DoctorMonthlyReportRow> Data,
    IReadOnlyList<ReportColumn> Columns,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

public sealed record M2DoctorMonthlyPreviewViewModel(
    string Title,
    string QueryMonth,
    string CalculationBasis,
    string VisitScope,
    string TimeSlot,
    string GeneratedAt,
    string ProgramId,
    string UserName,
    IReadOnlyList<M2DoctorMonthlyReportRow> Rows)
{
    public IReadOnlyList<int> DailyTotals => Enumerable.Range(0, 31)
        .Select(day => Rows.Sum(row => row.DailyCounts[day])).ToArray();
    public int GrandTotal => Rows.Sum(row => row.MonthlyTotal);
}

public sealed record M2RenderedFile(byte[] Content, string ContentType, string FileName);
