using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC144DebtDetailReportService
{
    Task<ReportDataAndColumns<C144DebtDetailReportViewModel>> QueryAsync(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<C144DebtDetailReportViewModel>> QueryAllAsync(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default);
}
