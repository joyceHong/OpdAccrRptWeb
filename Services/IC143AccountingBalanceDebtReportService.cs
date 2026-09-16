using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC143AccountingBalanceDebtReportService
{
    Task<ReportDataAndColumns<C143AccountingBalanceDebtReportViewModel>> QueryAsync(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default);
}
