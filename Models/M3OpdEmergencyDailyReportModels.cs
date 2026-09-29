namespace OpdAccrRptWeb.Models;

public sealed record M3OpdEmergencyDailyReportRequest(
    DateOnly? ReportDate,
    int PageNumber = 1,
    int PageSize = 10,
    string? RunId = null);

public sealed record M3DepartmentAggregate(
    string DepartmentId,
    long S1, long S2, long S3, long S4, long S5, long S6,
    long S7, long S8, long S9, long S10, long S11, long S12);

public sealed record M3EmergencyShiftCounts(long Day, long Evening, long Night);

public sealed record M3KpiSource(
    long? MorningTotal,
    long? AfternoonTotal,
    long? NightTotal,
    long? AppointmentTotal,
    long? NoShowTotal);

public sealed record M3NineKpis(
    long OutpatientMorning,
    long OutpatientAfternoon,
    long OutpatientNight,
    long EmergencyDay,
    long EmergencyEvening,
    long EmergencyNight,
    long Appointment,
    long NoShow,
    long NetAppointment);

public sealed record M3RepositoryResult(
    IReadOnlyList<M3DepartmentAggregate> Daily,
    IReadOnlyList<M3DepartmentAggregate> Monthly,
    IReadOnlyList<M3DepartmentAggregate> Yearly,
    M3EmergencyShiftCounts EmergencyShifts,
    M3KpiSource KpiSource);

public sealed record M3OpdEmergencyDailyReportRow(
    string DepartmentId,
    string DepartmentName,
    long Op1SQty, long Op1HQty, long Op2SQty, long Op2HQty,
    long Em1SQty, long Em1HQty, long Em2SQty, long Em2HQty,
    long OESQty, long OEHQty,
    long Op1MonQty, long Op2MonQty, long Em1MonQty, long Em2MonQty,
    long OEMonSQty, long OEMonHQty,
    long Op1YearQty, long Op2YearQty, long Em1YearQty, long Em2YearQty,
    long OEYearSQty, long OEYearHQty)
{
    public long Op1DaySum => checked(Op1SQty + Op1HQty);
    public long Op2DaySum => checked(Op2SQty + Op2HQty);
    public long Em1DaySum => checked(Em1SQty + Em1HQty);
    public long Em2DaySum => checked(Em2SQty + Em2HQty);
    public long OEDaySum => checked(OESQty + OEHQty);
    public long OEMonSum => checked(OEMonSQty + OEMonHQty);
    public long OEYearSum => checked(OEYearSQty + OEYearHQty);
}

public sealed record M3OpdEmergencyDailyReportSnapshot(
    string RunId,
    string Actor,
    DateOnly ReportDate,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<M3OpdEmergencyDailyReportRow> Rows,
    M3NineKpis NineKpis,
    long NumericChecksum,
    string? SourceWatermark = null);
