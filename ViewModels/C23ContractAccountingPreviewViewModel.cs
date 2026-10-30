namespace OpdAccrRptWeb.ViewModels;

public sealed record C23ContractAccountingPreviewViewModel(
    string Title,
    string DateRange,
    string ProcessedAt,
    string GeneratedBy,
    IReadOnlyList<C23ContractAccountingPreviewPage> Pages);

public sealed record C23ContractAccountingPreviewPage(
    string ContractCode,
    string ContractName,
    int PageNumber,
    IReadOnlyList<C23ContractAccountingReportViewModel> Rows,
    decimal? GrossTotal,
    decimal? DiscountTotal,
    decimal? ContractTotal);
