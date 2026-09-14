using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface ICashierCashRepository
{
    List<ModelDescriptionsHelper.PropertyMetadata> GetColumns();
    int GetCount(SearchReportCondition searchCondition);
    List<CashierCashReportViewModel> GetPage(SearchReportCondition searchCondition);
}
