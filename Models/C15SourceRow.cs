namespace OpdAccrRptWeb.Models;

public sealed record C15SourceRow(
    string VisitDate,
    string VisitTime,
    string VisitRoom,
    decimal EncounterNumber,
    string MedicalRecordNumber,
    string PatientName,
    string ReturnDate,
    string OrderCode,
    decimal? Amount);

public readonly record struct C15VisitKey(
    string VisitDate,
    string VisitTime,
    string VisitRoom,
    decimal EncounterNumber)
{
    public static C15VisitKey From(C15SourceRow row) => new(
        row.VisitDate.Trim(),
        row.VisitTime.Trim(),
        row.VisitRoom.Trim(),
        row.EncounterNumber);
}
