namespace OpdAccrRptWeb.ViewModels;

public sealed record C21PrintCell(
    string Code,
    string Name,
    decimal? SelfPayAmount,
    decimal? InsuranceAmount,
    decimal? InsuranceWithoutCardAmount);

public sealed record C21PrintRow(C21PrintCell Left, C21PrintCell Right);

public sealed record C21PrintSummary(
    string Label,
    decimal SelfPayAmount,
    decimal InsuranceAmount,
    decimal InsuranceWithoutCardAmount);

public sealed record C21AccountingSummaryPreviewViewModel(
    string Title,
    string ScopeName,
    string DateRange,
    string PrintedAt,
    string GeneratedBy,
    IReadOnlyList<C21PrintRow> FirstSection,
    IReadOnlyList<C21PrintRow> SecondSection,
    C21PrintSummary? Subtotal,
    C21PrintSummary? Income,
    C21PrintSummary? Difference);

public sealed record C21AccountingSummaryPreviewDocumentViewModel(
    IReadOnlyList<C21AccountingSummaryPreviewViewModel> Pages);
