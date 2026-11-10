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
    M2VisitScope VisitScope,
    M2TimeSlot TimeSlot,
    string GeneratedAt,
    string ProgramId,
    string UserName,
    IReadOnlyList<M2DoctorMonthlyReportRow> Rows)
{
    public string VisitScopeLabel => VisitScope switch
    {
        M2VisitScope.All => "門急診",
        M2VisitScope.Outpatient => "門診",
        M2VisitScope.Emergency => "急診",
        _ => throw new ArgumentOutOfRangeException(nameof(VisitScope))
    };
    public string TimeSlotLabel => TimeSlot switch
    {
        M2TimeSlot.All => "全部",
        M2TimeSlot.Morning => "上午診",
        M2TimeSlot.Afternoon => "下午診",
        M2TimeSlot.Night => "夜診",
        _ => throw new ArgumentOutOfRangeException(nameof(TimeSlot))
    };
    public IReadOnlyList<int> DailyTotals => Enumerable.Range(0, 31)
        .Select(day => Rows.Sum(row => row.DailyCounts[day])).ToArray();
    public int GrandTotal => Rows.Sum(row => row.MonthlyTotal);
}

public sealed record M2RenderedFile(byte[] Content, string ContentType, string FileName);
