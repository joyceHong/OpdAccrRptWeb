using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IReportPermissionRepository
{
    Task<IReadOnlyList<ReportPermissionUser>> FindUsersAsync(
        string loginName, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportPermissionGrant>> FindGrantsAsync(
        string userId, string sector, CancellationToken cancellationToken = default);

    Task<bool> TargetExistsAsync(
        int targetType, string target, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportPermissionGrant>> FindTargetGrantsAsync(
        int targetType, string target, CancellationToken cancellationToken = default);

    Task SaveGrantsAsync(
        int targetType, string target, IReadOnlyDictionary<string, int> reportMasks,
        string actor, CancellationToken cancellationToken = default);
}
