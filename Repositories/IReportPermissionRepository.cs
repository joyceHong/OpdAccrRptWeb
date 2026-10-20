using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IReportPermissionRepository
{
    Task<PermissionGrantRecordPage> FindActiveGrantPageAsync(
        IReadOnlyList<string> categoryCodes, IReadOnlyList<string> matchingNameCodes,
        string search, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PermissionTargetListPage> FindActiveTargetPageAsync(
        IReadOnlyList<string> categoryCodes, IReadOnlyList<string> matchingNameCodes,
        string search, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionTargetOption>> SearchTargetsAsync(
        int targetType, string search, CancellationToken cancellationToken = default);

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

    Task SaveGrantsIfUnchangedAsync(
        int targetType, string target, IReadOnlyList<PermissionChangeItem> grants,
        string actor, CancellationToken cancellationToken = default,
        string? sourceUserId = null);

    Task RevokeGrantIfUnchangedAsync(
        PermissionRevokeRequest request, string actor,
        CancellationToken cancellationToken = default);

    Task RevokeTargetIfUnchangedAsync(
        PermissionDeleteTargetRequest request, string actor,
        CancellationToken cancellationToken = default);
}
