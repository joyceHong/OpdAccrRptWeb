using System.Globalization;

namespace OpdAccrRptWeb.Models;

public enum C16Source { OutpatientEmergency = 0, Inpatient = 1 }
public enum C16ReportType { All = 0, Child = 1, NewHope = 2 }
public enum C16DateBasis { AccountingDate = 0, VisitDate = 1 }

public sealed record C16PreviewRequest(
    string StartDate,
    string EndDate,
    C16Source Source,
    C16ReportType ReportType,
    C16DateBasis DateBasis,
    int PageNumber = 1,
    int PageSize = 10)
{
    public C16DateBasis EffectiveDateBasis =>
        Source == C16Source.Inpatient ? C16DateBasis.AccountingDate : DateBasis;

    public C16QueryPeriod ValidateAndCreatePeriod()
    {
        if (!Enum.IsDefined(Source) || !Enum.IsDefined(ReportType) || !Enum.IsDefined(DateBasis))
            throw new ArgumentException("C16 查詢條件不正確。");
        if (!DateOnly.TryParseExact(StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly start) || start.Year < 1912)
            throw new ArgumentException("請輸入有效的 C16 起始日期。");
        if (!DateOnly.TryParseExact(EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly end) || end.Year < 1912)
            throw new ArgumentException("請輸入有效的 C16 截止日期。");
        if (start > end) throw new ArgumentException("C16 起始日期不可晚於截止日期。");
        if (PageNumber < 1 || PageSize is not (10 or 30 or 50))
            throw new ArgumentException("C16 分頁條件不正確。");
        return new C16QueryPeriod(ToRoc(start), ToRoc(end));
    }

    private static string ToRoc(DateOnly value) => $"{value.Year - 1911:000}{value:MMdd}";
}

public sealed record C16QueryPeriod(string StartDate, string EndDate)
{
    public string StartDateTime => StartDate + "0000";
    public string EndDateTime => EndDate + "9999";
}

public sealed record C16SourceRow(
    int SourceOrdinal,
    string PatientName,
    string PatientId,
    string BirthDate,
    string VisitDate,
    string DischargeDate,
    string SectionName,
    string BasicDiagnosis,
    string? FirstInpatientDiagnosis,
    string PFin1,
    string PFin2,
    string OrderCode,
    string VisitTime,
    string VisitRoom,
    decimal EncounterNumber,
    decimal? Sub5,
    decimal? Sub2);

public readonly record struct C16VisitKey(
    string VisitDate, string VisitTime, string VisitRoom, decimal EncounterNumber)
{
    public static C16VisitKey From(C16SourceRow row) => new(
        row.VisitDate.Trim(), row.VisitTime.Trim(), row.VisitRoom.Trim(), row.EncounterNumber);
}

public sealed class C16ReportRow
{
    public int EncounterOrdinal { get; init; }
    public string PatientName { get; init; } = string.Empty;
    public string PatientId { get; init; } = string.Empty;
    public string BirthDate { get; init; } = string.Empty;
    public string VisitDate { get; init; } = string.Empty;
    public string DischargeDate { get; init; } = string.Empty;
    public int Days { get; init; }
    public string SectionName { get; init; } = string.Empty;
    public string Diagnosis { get; init; } = string.Empty;
    public string SubsidyType { get; init; } = string.Empty;
    public string RoomType { get; init; } = string.Empty;
    public decimal Rl25 { get; set; }
    public decimal Rl49 { get; set; }
    public decimal Rl49Drug { get; set; }
    public decimal Rl50 { get; set; }
    public decimal OutpatientPartPay => RoomType == "R" ? Rl49 + Rl50 : 0m;
    public decimal EmergencyPartPay => RoomType == "E" ? Rl49 + Rl50 : 0m;
    public decimal OutpatientTotal => Rl25 + Rl49 + Rl50 + Rl49Drug;
    public decimal InpatientTotal => Rl49 + Rl50;
}
