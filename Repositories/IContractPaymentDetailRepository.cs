using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IContractPaymentDetailRepository
{
    List<ModelDescriptionsHelper.PropertyMetadata> GetColumns();
    int GetCount(SearchReportCondition searchCondition);
    List<ContractPaymentDetailReportViewModel> GetPage(SearchReportCondition searchCondition);
}
