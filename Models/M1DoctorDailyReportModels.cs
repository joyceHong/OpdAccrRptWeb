namespace OpdAccrRptWeb.Models;

public sealed record M1DoctorDailyReportRequest(
    DateOnly? ReportDate,
    bool FutureDateConfirmed = false,
    int PageNumber = 1,
    int PageSize = 10,
    string? RunId = null);

public sealed record M1DoctorDailyAggregateRow(
    string SectionNo,
    string SectionName,
    string DoctorNo,
    string DoctorName,
    int S1,
    int S2,
    int S3,
    int S4,
    int S5,
    int S6,
    int S7,
    int S8,
    int S9,
    int S10,
    int S11);

public sealed record M1DoctorDailyReportRow(
    string SectionNo,
    string SectionName,
    string DoctorNo,
    string DoctorName,
    string VisitType,
    int SelfPayCount,
    int InsuranceCount,
    int MorningCount,
    int AfternoonCount,
    int NightCount,
    int AppointmentCount)
{
    public int TotalCount => checked(SelfPayCount + InsuranceCount);

    public static M1DoctorDailyReportRow Create(
        string sectionNo,
        string sectionName,
        string doctorNo,
        string doctorName,
        string visitType,
        int selfPayCount,
        int insuranceCount,
        int morningCount,
        int afternoonCount,
        int nightCount,
        int appointmentCount)
    {
        if (visitType is not ("R" or "E"))
            throw new ArgumentException("M1 門急診類別只允許 R 或 E。", nameof(visitType));
        if (visitType == "E" && appointmentCount != 0)
            throw new ArgumentException("M1 急診預約量必須為 0。", nameof(appointmentCount));

        return new(
            sectionNo.Trim(), sectionName.Trim(), doctorNo.Trim(), doctorName.Trim(), visitType,
            selfPayCount, insuranceCount, morningCount, afternoonCount, nightCount,
            appointmentCount);
    }
}

public sealed record M1DoctorDailyReportSnapshot(
    string RunId,
    string Actor,
    DateOnly ReportDate,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<M1DoctorDailyReportRow> Rows);

