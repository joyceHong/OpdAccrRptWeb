using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IContractPaymentDetailRepository
{
    List<ModelDescriptionsHelper.PropertyMetadata> GetColumns();
    IReadOnlyList<C29ContractChoice> GetContractChoices();
    int GetCount(SearchReportCondition searchCondition);
    List<ContractPaymentDetailReportViewModel> GetPage(SearchReportCondition searchCondition);
}

public sealed record C29ContractChoice(string Code, string Name);
