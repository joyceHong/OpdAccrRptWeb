using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface ISapInterfaceRepository
{
    Task<string?> GetDefaultRocDateAsync(CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, bool>> GetCompletedAsync(string rocDate, CancellationToken cancellationToken);
    Task<SapInterfaceRepositoryRunResult> RunEventAsync(string eventCode, string rocDate, string gregorianDate,
        bool confirmRerun, CancellationToken cancellationToken);
}
