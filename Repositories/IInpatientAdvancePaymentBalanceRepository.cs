using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IInpatientAdvancePaymentBalanceRepository
{
    List<ModelDescriptionsHelper.PropertyMetadata> GetColumns();
    int GetCount(SearchReportCondition searchCondition);
    List<InpatientAdvancePaymentBalanceReportViewModel> GetPage(SearchReportCondition searchCondition);
}
