using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories
{
    public interface IHealthCenterRepository
    {
        List<T> GetHealthCenterData<T>(SearchReportCondition searchCondition);
        List<ModelDescriptionsHelper.PropertyMetadata> GetHelthCenterDetailColumns();
        List<ModelDescriptionsHelper.PropertyMetadata> GetHelthCenterCountColumns();
        List<T> GetHealthCenterCountData<T>(SearchReportCondition searchCondition);
    }
}