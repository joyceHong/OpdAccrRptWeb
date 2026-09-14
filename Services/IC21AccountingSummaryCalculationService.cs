using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC21AccountingSummaryCalculationService
{
    IReadOnlyList<C21AccountingSummaryReportViewModel> Calculate(
        SearchReportCondition condition,
        IReadOnlyCollection<C21SourceAmount> sourceAmounts,
        IReadOnlyCollection<C21BillingItem> billingItems);
}
