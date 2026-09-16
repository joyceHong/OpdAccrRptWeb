using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC144DebtDetailReportRepository
{
    int GetCount(C144Query query, CancellationToken cancellationToken = default);
    List<C144DebtDetailReportViewModel> GetPage(
        C144Query query, int offset, int pageSize, CancellationToken cancellationToken = default);
    List<C144DebtDetailReportViewModel> GetAll(
        C144Query query, CancellationToken cancellationToken = default);
}
