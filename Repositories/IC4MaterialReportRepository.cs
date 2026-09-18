using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IC4MaterialReportRepository
{
    Task<IReadOnlyList<C4MaterialSourceRow>> QueryDayAsync(string runDate,
        string? sectionPrefix, CancellationToken cancellationToken = default);
}
