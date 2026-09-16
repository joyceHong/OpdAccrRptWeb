using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC143AccountingBalanceDebtRepository
{
    int GetOutpatientEmergencyCount(C143Query query, CancellationToken cancellationToken = default);
    List<C143AccountingBalanceDebtReportViewModel> GetOutpatientEmergencyPage(
        C143Query query, int offset, int pageSize, CancellationToken cancellationToken = default);
    int GetInpatientCount(C143Query query, int dischargeGroup, CancellationToken cancellationToken = default);
    List<C143AccountingBalanceDebtReportViewModel> GetInpatientPage(
        C143Query query, int dischargeGroup, int offset, int pageSize,
        CancellationToken cancellationToken = default);
}
