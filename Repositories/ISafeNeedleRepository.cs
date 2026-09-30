using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface ISafeNeedleRepository
{
    List<ModelDescriptionsHelper.PropertyMetadata> GetColumns();

    int GetCount(SearchReportCondition searchCondition);

    List<SafeNeedleReportViewModel> GetPage(SearchReportCondition searchCondition);

    List<SafeNeedleReportViewModel> GetBatch(
        SearchReportCondition searchCondition,
        int offset,
        int batchSize);
}
