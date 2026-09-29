using System.Collections.ObjectModel;

namespace OpdAccrRptWeb.Models;

public enum M2CalculationBasis { Statistics, ActualVisit }
public enum M2VisitScope { All, Outpatient, Emergency }
public enum M2TimeSlot { All, Morning, Afternoon, Night }

public sealed record M2DoctorMonthlyReportRequest(
    string? ReportMonth,
    M2CalculationBasis CalculationBasis = M2CalculationBasis.Statistics,
    M2VisitScope VisitScope = M2VisitScope.All,
    M2TimeSlot TimeSlot = M2TimeSlot.All,
    int PageNumber = 1,
    int PageSize = 10,
    string? RunId = null);

public sealed record M2DoctorMonthlySourceRow(
    string SectionNo,
    string SectionName,
    string DoctorNo,
    string DoctorName,
    int Day,
    int Count);

public sealed record M2DoctorMonthlyReportRow
{
    private M2DoctorMonthlyReportRow(string sectionNo, string sectionName, string doctorNo,
        string doctorName, IReadOnlyList<int> dailyCounts)
    {
        SectionNo = sectionNo;
        SectionName = sectionName;
        DoctorNo = doctorNo;
        DoctorName = doctorName;
        DailyCounts = dailyCounts;
    }

    public string SectionNo { get; }
    public string SectionName { get; }
    public string DoctorNo { get; }
    public string DoctorName { get; }
    public IReadOnlyList<int> DailyCounts { get; }
    public int MonthlyTotal => checked(DailyCounts.Sum());

    public static M2DoctorMonthlyReportRow Create(string sectionNo, string sectionName,
        string doctorNo, string doctorName, IEnumerable<int> dailyCounts)
    {
        int[] counts = dailyCounts.ToArray();
        if (counts.Length != 31)
            throw new ArgumentException("M2 每月人數必須固定包含 D01 至 D31。", nameof(dailyCounts));
        return new(sectionNo.Trim(), sectionName.Trim(), doctorNo.Trim(), doctorName.Trim(),
            new ReadOnlyCollection<int>(counts));
    }
}

public sealed record M2DoctorMonthlyReportSnapshot(
    string RunId,
    string Actor,
    DateOnly ReportMonth,
    M2CalculationBasis CalculationBasis,
    M2VisitScope VisitScope,
    M2TimeSlot TimeSlot,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<M2DoctorMonthlyReportRow> Rows,
    long NumericChecksum);
