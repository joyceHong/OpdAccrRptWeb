using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
[ServiceFilter(typeof(ReportPermissionManagerFilter))]
public sealed class ReportPermissionManagementController(
    ReportPermissionAdministrationService administration,
    IReportCatalogService catalog,
    ILogger<ReportPermissionManagementController> logger) : Controller
{
    [HttpGet("permissions")]
    public IActionResult Index()
    {
        ReportPermissionSnapshot snapshot =
            (ReportPermissionSnapshot)HttpContext.Items[ReportPermissionManagerFilter.SnapshotItemKey]!;
        string path = ReportNavigationService.FirstAllowedPath(catalog.GetReportIndex(), snapshot)
            ?? "/Report";
        return Redirect($"{path}?permissionSettings=1");
    }

    [HttpGet("permissions/panel")]
    public IActionResult Panel() =>
        PartialView("~/Views/ReportPermissionManagement/_PermissionPanel.cshtml", catalog.GetReportIndex());

    [HttpGet("permissions/grants")]
    public async Task<IActionResult> Grants(
        int targetType, string target, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await administration.GetGrantsAsync(targetType, target, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Cannot read report permission grants");
            return StatusCode(503);
        }
    }

    [HttpPost("permissions/grants")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(
        [FromBody] PermissionGrantEditRequest request, CancellationToken cancellationToken)
    {
        if (HttpContext.Items[ReportPermissionManagerFilter.SnapshotItemKey]
            is not ReportPermissionSnapshot snapshot || snapshot.UserId is null)
        {
            return StatusCode(403);
        }
        try
        {
            await administration.SaveGrantAsync(request, snapshot.UserId, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Cannot save report permission grant");
            return StatusCode(503);
        }
    }

    [HttpPost("permissions/batch/preview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewBatch(
        [FromBody] PermissionBatchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await administration.PreviewBatchAsync(request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Cannot preview report permission batch");
            return StatusCode(503);
        }
    }

    [HttpPost("permissions/batch/apply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyBatch(
        [FromBody] PermissionBatchRequest request, CancellationToken cancellationToken)
    {
        if (HttpContext.Items[ReportPermissionManagerFilter.SnapshotItemKey]
            is not ReportPermissionSnapshot snapshot || snapshot.UserId is null)
        {
            return StatusCode(403);
        }
        try
        {
            await administration.ApplyBatchAsync(request, snapshot.UserId, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Cannot apply report permission batch");
            return StatusCode(503);
        }
    }

    [HttpPost("permissions/copy/preview")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PreviewCopy(
        [FromBody] PermissionCopyRequest request, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await administration.PreviewCopyAsync(request, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Cannot preview administrator permission copy");
            return StatusCode(503);
        }
    }

    [HttpPost("permissions/copy/apply")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplyCopy(
        [FromBody] PermissionCopyRequest request, CancellationToken cancellationToken)
    {
        if (HttpContext.Items[ReportPermissionManagerFilter.SnapshotItemKey]
            is not ReportPermissionSnapshot snapshot || snapshot.UserId is null)
        {
            return StatusCode(403);
        }
        try
        {
            await administration.ApplyCopyAsync(request, snapshot.UserId, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Cannot copy administrator permissions");
            return StatusCode(503);
        }
    }
}
