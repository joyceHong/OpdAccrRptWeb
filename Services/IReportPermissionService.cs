using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public interface IReportPermissionService
{
    Task<ReportPermissionSnapshot> ResolveAsync(
        string? loginName, CancellationToken cancellationToken = default);
}

public sealed class ReportPermissionSnapshot(
    string? userId,
    bool canManage,
    IReadOnlyDictionary<string, ReportAction> actions)
{
    public string? UserId { get; } = userId;
    public bool CanManage { get; } = canManage;
    public IReadOnlyDictionary<string, ReportAction> Actions { get; } = actions;

    public bool Allows(string reportCode, ReportAction action)
    {
        if (action == ReportAction.None ||
            !Actions.TryGetValue(reportCode, out ReportAction granted) ||
            (granted & ReportAction.Query) == 0)
        {
            return false;
        }

        return (granted & action) == action;
    }
}
