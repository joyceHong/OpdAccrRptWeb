using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
[Route("sap-interface")]
public sealed class SapInterfaceController(
    ISapInterfaceService service,
    IReportCatalogService catalog,
    ILogger<SapInterfaceController> logger) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Report/Index.cshtml", catalog.GetReportIndex());

    [HttpGet("default-date")]
    public async Task<IActionResult> DefaultDate(CancellationToken cancellationToken)
    {
        try
        {
            NoStore();
            return Ok(new { businessDate = await service.GetDefaultDateAsync(cancellationToken) });
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "SAP default date failed. TraceId={TraceId}", HttpContext.TraceIdentifier);
            return Failure("無法取得預設日期。");
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> Status([FromQuery] string businessDate, CancellationToken cancellationToken)
    {
        try
        {
            NoStore();
            return Ok(await service.GetStatusAsync(businessDate, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "SAP status failed. TraceId={TraceId}", HttpContext.TraceIdentifier);
            return Failure("無法取得作業狀態。");
        }
    }

    [HttpPost("run")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Run([FromBody] SapInterfaceRequest? request, CancellationToken cancellationToken)
    {
        if (request is null) return BadRequest("作業條件格式不正確。");
        try
        {
            NoStore();
            SapInterfaceRunResult result = await service.RunAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "SAP run failed. TraceId={TraceId}", HttpContext.TraceIdentifier);
            return Failure("SAP 介接作業暫時無法執行。");
        }
    }

    private void NoStore() => Response.Headers.CacheControl = "private, no-store";

    private ObjectResult Failure(string title) => StatusCode(StatusCodes.Status503ServiceUnavailable,
        new ProblemDetails { Status = StatusCodes.Status503ServiceUnavailable, Title = title });
}
