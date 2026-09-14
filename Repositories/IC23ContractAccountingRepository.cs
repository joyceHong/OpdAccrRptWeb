using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC23ContractAccountingRepository
{
    List<ModelDescriptionsHelper.PropertyMetadata> GetColumns();
    IReadOnlyList<C23ContractOption> GetContracts();
    int GetCount(SearchReportCondition condition);
    List<C23ContractAccountingReportViewModel> GetPage(SearchReportCondition condition);
    bool HasIntermediateData(string encounterSource, string rocDate);
    void RebuildSingleDay(string encounterSource, string rocDate, string? contractCode);
}
