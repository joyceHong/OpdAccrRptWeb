using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC10ReceivableDetailRepository
{
    C10RepositoryResult Load(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default);
}
