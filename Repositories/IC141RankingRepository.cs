using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC141RankingRepository
{
    DateOnly GetOracleToday(CancellationToken cancellationToken);

    IReadOnlyList<C141RankingSourceRow> Query(C141RankingSearch search, CancellationToken cancellationToken);

    int GetCount(C141RankingSearch search, CancellationToken cancellationToken);

    IReadOnlyList<C141RankingSourceRow> GetPage(
        C141RankingSearch search,
        int offset,
        int pageSize,
        CancellationToken cancellationToken);
}
