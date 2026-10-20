using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OpdAccrRptWeb.Services;

public sealed class ReportPermissionManagerFilter(
    IReportPermissionService permissions,
    ILogger<ReportPermissionManagerFilter> logger) : IAsyncAuthorizationFilter
{
    public const string SnapshotItemKey = "ReportPermissionSnapshot";

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        try
        {
            ReportPermissionSnapshot snapshot = await permissions.ResolveAsync(
                context.HttpContext.User.Identity.Name, context.HttpContext.RequestAborted);
            if (!snapshot.CanManage) context.Result = new StatusCodeResult(403);
            else context.HttpContext.Items[SnapshotItemKey] = snapshot;
        }
        catch (OperationCanceledException) when (context.HttpContext.RequestAborted.IsCancellationRequested)
        {
            context.Result = new StatusCodeResult(499);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Cannot check report permission manager access");
            context.Result = new StatusCodeResult(503);
        }
    }
}
