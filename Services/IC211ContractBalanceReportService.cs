using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC211ContractBalanceReportService
{
    Task<ReportDataAndColumns<C211ContractBalanceReportViewModel>> CreateAsync(
        SearchReportCondition condition,
        string userId,
        CancellationToken cancellationToken = default);
}
