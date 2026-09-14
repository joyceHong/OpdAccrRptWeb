using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IInpatientReceivableBalanceRepository
{
    List<ModelDescriptionsHelper.PropertyMetadata> GetColumns();
    int GetCount(SearchReportCondition searchCondition);
    List<InpatientReceivableBalanceReportViewModel> GetPage(SearchReportCondition searchCondition);
}
