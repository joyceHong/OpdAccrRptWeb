using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IC9ReportRepository
{
    Task<IReadOnlyList<C9SourceRow>> QueryDayAsync(string rocDate,
        CancellationToken token = default);
}
