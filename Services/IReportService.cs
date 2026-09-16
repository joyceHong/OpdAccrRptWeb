using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services
{
    public interface IReportService
    {
        ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition);

        Task<ReportDataAndColumns<C10ReceivableDetailRow>> ReportC10Async(
            SearchReportCondition searchCondition,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("C10 report service 尚未設定。");

        Task<C11ReceivablesCollectionReportViewModel> ReportC11Async(
            SearchReportCondition searchCondition,
            string generatedBy,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("C11 report service 尚未設定。");

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

        Task<ReportDataAndColumns<C143AccountingBalanceDebtReportViewModel>> ReportC143Async(
            SearchReportCondition searchCondition,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("C143 report service 尚未設定。");

        Task<ReportDataAndColumns<C144DebtDetailReportViewModel>> ReportC144Async(
            SearchReportCondition searchCondition,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("C144 report service 尚未設定。");

        Task<ReportDataAndColumns<C15AssistiveDeviceDepositDetailReportViewModel>> ReportC15Async(
            SearchReportCondition searchCondition,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("C15 report service 尚未設定。");
    }
}
