using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
public sealed class ReportPermissionStatusController(
    IReportPermissionService permissions,
    ILogger<ReportPermissionStatusController> logger) : Controller
{
    [HttpGet("permissions/me")]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        try
        {
            ReportPermissionSnapshot snapshot =
                await permissions.ResolveAsync(User.Identity?.Name, cancellationToken);
            return Ok(new { canManage = snapshot.CanManage });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Cannot check report permission manager navigation");
            return StatusCode(503);
        }
    }
}
