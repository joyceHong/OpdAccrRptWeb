using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC10AmountCalculationService
{
    IReadOnlyList<C10ReceivableDetailRow> Calculate(
        string source,
        C10RepositoryResult sourceData);
}
