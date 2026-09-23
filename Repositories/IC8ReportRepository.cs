using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IC8ReportRepository
{
    Task<IReadOnlyList<C8SourceRow>> QueryAsync(string rocStartDate, string rocEndDate,
        CancellationToken token = default);
    Task<IReadOnlyDictionary<string, string>> GetSectionMappingsAsync(
        IReadOnlyCollection<string> legacySectionCodes, CancellationToken token = default);
}
