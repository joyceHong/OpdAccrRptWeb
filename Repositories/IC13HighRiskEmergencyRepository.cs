using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC13HighRiskEmergencyRepository
{
    int GetCount(SearchReportCondition condition, CancellationToken cancellationToken = default);

    List<C13HighRiskEmergencyReportViewModel> GetPage(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default);

    List<C13HighRiskEmergencyReportViewModel> GetAllForPreview(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default);

    List<ModelDescriptionsHelper.PropertyMetadata> GetColumns();
}
