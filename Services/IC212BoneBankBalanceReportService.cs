using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC212BoneBankBalanceReportService
{
    Task<ReportDataAndColumns<C212BoneBankBalanceReportViewModel>> CreateAsync(
        C212Query query,
        string userId,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task<C212ReportResult> CreateResultAsync(
        C212Query query,
        string userId,
        string correlationId,
        CancellationToken cancellationToken = default);
}
