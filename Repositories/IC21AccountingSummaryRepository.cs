using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC21AccountingSummaryRepository
{
    List<OpdAccrRptWeb.Help.ModelDescriptionsHelper.PropertyMetadata> GetColumns();
    IReadOnlyList<C21BillingItem> GetBillingItems();
    IReadOnlyList<C21SourceAmount> GetSourceAmounts(SearchReportCondition condition);
    bool HasInpatientRoom23Data(string rocDate);
    void RebuildSingleInpatientDay(string rocDate);
}
