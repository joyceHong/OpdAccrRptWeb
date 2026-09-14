using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC11ReceivablesCollectionReportService
{
    Task<C11ReceivablesCollectionReportViewModel> CreateAsync(
        SearchReportCondition condition,
        string generatedBy,
        CancellationToken cancellationToken = default);
}
