using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IOutpatientReceivableBalanceRepository
{
    List<ModelDescriptionsHelper.PropertyMetadata> GetColumns();
    int GetCount(SearchReportCondition searchCondition);
    List<OutpatientReceivableBalanceReportViewModel> GetPage(SearchReportCondition searchCondition);
}
