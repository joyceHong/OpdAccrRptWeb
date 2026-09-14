using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC24DebtPaymentCalculationService
{
    C24CanonicalResult Calculate(SearchReportCondition condition, C24RepositoryResult source,
        string? correlationId = null);
}
