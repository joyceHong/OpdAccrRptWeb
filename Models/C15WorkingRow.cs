namespace OpdAccrRptWeb.Models;

public sealed class C15WorkingRow
{
    public string VisitDate { get; set; } = string.Empty;
    public string MedicalRecordNumber { get; set; } = string.Empty;
    public string PatientName { get; set; } = string.Empty;
    public string ReturnDate { get; set; } = string.Empty;
    public decimal? Rl001 { get; set; }
    public decimal? Rl002 { get; set; }
    public decimal? Rl003 { get; set; }
    public decimal? Rl004 { get; set; }
    public string Type { get; set; } = string.Empty;
    public int EncounterOrdinal { get; init; }
}
