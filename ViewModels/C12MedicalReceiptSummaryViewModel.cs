namespace OpdAccrRptWeb.ViewModels;

public enum C12Source { OutpatientAndEmergency, Inpatient }

public sealed record C12ReportRequest(string StartDate, string EndDate, C12Source Source,
    int RoomType, string PatientIdentity, string? NewSectionCode);

public sealed record C12SectionOption(string Code, string Name);

public sealed record C12VisitKey(string Date, string Time, string Room, decimal Number);
public sealed record C12VisitRow(C12VisitKey Key, string SectionCode, string DoctorName,
    string MedicalRecordNumber, bool IsCurrentlyInpatient);
public sealed record C12ChargeRow(string Name, decimal? InsuranceAmount,
    decimal? RawSelfPayAmount, decimal? DiscountOrOnAccountAmount);
public sealed record C12PatientRow(string MedicalRecordNumber, string Name, string NationalId);
public sealed record C12ReportItem(int Sequence, string Name, int InsuranceAmount,
    int RawSelfPayAmount, int DiscountOrOnAccountAmount)
{
    public int ReceivedAmount => checked(RawSelfPayAmount - DiscountOrOnAccountAmount);
    public int TotalAmount => checked(InsuranceAmount + RawSelfPayAmount);
}
public sealed record C12VisitModel(C12VisitKey Key, string SectionCode, string SectionName,
    string DoctorName, bool IsCurrentlyInpatient, IReadOnlyList<C12ReportItem> Items);
public sealed record C12ReportCriteria(string StartDate, string EndDate, C12Source Source,
    string RoomTypeLabel, string? NewSectionCode);
public sealed record C12ReportModel(C12PatientRow Patient, C12ReportCriteria Criteria,
    IReadOnlyList<string> Warnings, IReadOnlyList<C12VisitModel> Visits);
public sealed record C12PackedItemRow(C12ReportItem? Item1, C12ReportItem? Item2, C12ReportItem? Item3);
public sealed record C12VisitDetail(C12VisitModel Visit, IReadOnlyList<C12PackedItemRow> Rows,
    int InsuranceTotal, int RawSelfPayTotal, int DiscountTotal, int ReceivedTotal, int Total);
public sealed record C12SummaryRow(string Name, int InsuranceAmount, int RawSelfPayAmount,
    int DiscountOrOnAccountAmount);
public sealed record C12DetailRow(string VisitDate, string VisitTime, string Room,
    decimal VisitNumber, string SectionName, string DoctorName, int Sequence,
    string ItemName, int InsuranceAmount, int RawSelfPayAmount,
    int DiscountOrOnAccountAmount, int ReceivedAmount);
public sealed record C12ReportTotals(int Total, int Insurance, int DiscountOrOnAccount, int Received);
public sealed record C12MedicalReceiptSummaryViewModel(C12PatientRow Patient,
    C12ReportCriteria Criteria, IReadOnlyList<string> Warnings,
    IReadOnlyList<C12VisitDetail> DetailVisits, IReadOnlyList<C12DetailRow> DetailRows,
    IReadOnlyList<C12SummaryRow> SummaryRows,
    C12ReportTotals DetailTotals, C12ReportTotals SummaryTotals, bool HasVisits);
