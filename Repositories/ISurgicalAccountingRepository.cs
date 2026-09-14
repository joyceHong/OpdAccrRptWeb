using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface ISurgicalAccountingRepository
{
    List<ModelDescriptionsHelper.PropertyMetadata> GetColumns();

    int GetCount(SearchReportCondition searchCondition);

    List<SurgicalAccountingReportViewModel> GetPage(SearchReportCondition searchCondition);
}
