using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services
{
    public interface IReportService
    {
        ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition);

        Task<ReportDataAndColumns<C211ContractBalanceReportViewModel>> ReportC211Async(
            SearchReportCondition searchCondition,
            string userId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("C211 report service 尚未設定。");

        Task<ReportDataAndColumns<C212BoneBankBalanceReportViewModel>> ReportC212Async(
            C212Query query,
            string userId,
            string correlationId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("C212 report service 尚未設定。");
    }
}
