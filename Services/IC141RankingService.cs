using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC141RankingService
{
    DateOnly GetOracleToday(CancellationToken cancellationToken = default);

    IReadOnlyList<C141RankingRow> Query(C141RankingSearch search, CancellationToken cancellationToken = default);

    int GetCount(C141RankingSearch search, CancellationToken cancellationToken = default);

    IReadOnlyList<C141RankingRow> GetPage(
        C141RankingSearch search,
        int offset,
        int pageSize,
        CancellationToken cancellationToken = default);
}
