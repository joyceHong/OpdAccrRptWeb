using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed class ReportPermissionService(
    IReportPermissionRepository repository,
    IReportCatalogService catalog,
    ILogger<ReportPermissionService> logger) : IReportPermissionService
{
    public async Task<ReportPermissionSnapshot> ResolveAsync(
        string? loginName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(loginName)) return Denied();

        IReadOnlyList<ReportPermissionUser> users =
            await repository.FindUsersAsync(loginName, cancellationToken);
        if (users.Count != 1 ||
            !string.Equals(users[0].UserId.TrimEnd(), loginName, StringComparison.Ordinal))
        {
            logger.LogWarning("Report permission identity did not resolve uniquely for {LoginName}", loginName);
            return Denied();
        }

        var user = new ReportPermissionUser(users[0].UserId.TrimEnd(), users[0].Sector.TrimEnd());
        IReadOnlyList<ReportPermissionGrant> grants =
            await repository.FindGrantsAsync(user.UserId, user.Sector, cancellationToken);
        HashSet<string> reportCodes = catalog.GetReportIndex().Categories
            .SelectMany(category => category.Groups)
            .SelectMany(group => group.Reports)
            .Select(report => report.Code)
            .ToHashSet(StringComparer.Ordinal);

        var selected = new Dictionary<string, (int Priority, ReportAction Actions)>(StringComparer.Ordinal);
        var ambiguous = new HashSet<string>(StringComparer.Ordinal);
        bool canManage = false;
        foreach (ReportPermissionGrant grant in grants)
        {
            if (!reportCodes.Contains(grant.ReportCode))
            {
                logger.LogWarning("Unknown report permission code {ReportCode}", grant.ReportCode);
                continue;
            }
            if (grant.ActionMask is < 0 or > 31 ||
                (grant.ReportCode != "C141" && (grant.ActionMask & (int)ReportAction.LineBot) != 0))
            {
                logger.LogWarning("Invalid report permission mask for {ReportCode}", grant.ReportCode);
                continue;
            }

            string target = grant.Target.TrimEnd();
            int priority = grant.TargetType switch
            {
                0 when target == user.UserId => 3,
                2 when target == user.UserId => 2,
                1 when user.Sector.Length > 0 && target == user.Sector => 1,
                _ => 0
            };
            if (priority == 0) continue;
            if (priority == 3) canManage = true;

            if (selected.TryGetValue(grant.ReportCode, out var previous))
            {
                if (priority == previous.Priority)
                {
                    ambiguous.Add(grant.ReportCode);
                    logger.LogWarning("Duplicate report permission priority for {ReportCode}", grant.ReportCode);
                }
                else if (priority > previous.Priority)
                {
                    selected[grant.ReportCode] = (priority, (ReportAction)grant.ActionMask);
                    ambiguous.Remove(grant.ReportCode);
                }
            }
            else
            {
                selected.Add(grant.ReportCode, (priority, (ReportAction)grant.ActionMask));
            }
        }

        IReadOnlyDictionary<string, ReportAction> actions = selected
            .Where(pair => !ambiguous.Contains(pair.Key))
            .ToDictionary(pair => pair.Key, pair => pair.Value.Actions, StringComparer.Ordinal);
        return new ReportPermissionSnapshot(user.UserId, canManage, actions);
    }

    private static ReportPermissionSnapshot Denied() =>
        new(null, false, new Dictionary<string, ReportAction>(StringComparer.Ordinal));
}
