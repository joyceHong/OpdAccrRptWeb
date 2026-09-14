using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface ICashierCashSummaryRepository
{
    List<ModelDescriptionsHelper.PropertyMetadata> GetColumns();
    int GetCount(SearchReportCondition searchCondition);
    List<CashierCashSummaryReportViewModel> GetPage(SearchReportCondition searchCondition);
}
