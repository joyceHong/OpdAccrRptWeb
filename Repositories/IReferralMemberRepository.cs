using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IReferralMemberRepository
{
    List<ModelDescriptionsHelper.PropertyMetadata> GetColumns();

    int GetCount(SearchReportCondition searchCondition);

    List<ReferralMemberReportViewModel> GetPage(SearchReportCondition searchCondition);
}
