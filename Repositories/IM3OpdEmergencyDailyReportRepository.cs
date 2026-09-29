using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IM3OpdEmergencyDailyReportRepository
{
    Task<M3RepositoryResult> QueryAsync(string reportDate, string monthStartDate,
        string yearStartDate, CancellationToken cancellationToken = default);
    Task<string> ResolveDepartmentNameAsync(string oldDepartmentId,
        CancellationToken cancellationToken = default);
}
