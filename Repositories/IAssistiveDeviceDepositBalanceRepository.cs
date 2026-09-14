using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IAssistiveDeviceDepositBalanceRepository
{
    List<ModelDescriptionsHelper.PropertyMetadata> GetColumns();
    int GetCount(SearchReportCondition searchCondition);
    List<AssistiveDeviceDepositBalanceReportViewModel> GetPage(SearchReportCondition searchCondition);
}
