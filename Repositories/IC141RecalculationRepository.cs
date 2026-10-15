using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC141RecalculationRepository
{
    IReadOnlyList<C141RecalculationCase> GetClaimFlaggedCases(CancellationToken cancellationToken);

    C141RecalculationTotals GetTotals(
        C141AdmissionKey key,
        string accountingDateRoc,
        bool isCurrentAccountingDay,
        CancellationToken cancellationToken);
}
