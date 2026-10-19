namespace OpdAccrRptWeb.ViewModels;

public sealed record C1SurgicalPrintPage(
    string EncounterType,
    int PageNumber,
    int TotalPages,
    IReadOnlyList<SurgicalAccountingReportViewModel> Rows);

public sealed record C1SurgicalPrintPreviewViewModel(
    string StartDate,
    string EndDate,
    string GeneratedAt,
    string GeneratedBy,
    IReadOnlyList<C1SurgicalPrintPage> Pages);
