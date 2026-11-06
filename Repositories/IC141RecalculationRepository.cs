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

    void UpdateAmounts(C141AdmissionKey key, decimal receivableAmount,
        decimal insuredReceivableAmount, CancellationToken cancellationToken);
}
