using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC141RecalculationService
{
    IReadOnlyList<C141CalculatedCase> CalculateClaimFlaggedCases(
        string accountingDateRoc,
        string oracleTodayRoc,
        CancellationToken cancellationToken = default);
}
