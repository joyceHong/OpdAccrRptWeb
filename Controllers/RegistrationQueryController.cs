using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
[Route("data-query/registration")]
public sealed class RegistrationQueryController(
    IRegistrationQueryService service,
    IReportCatalogService catalog,
    ILogger<RegistrationQueryController> logger) : Controller
{
    [HttpGet("")]
    public IActionResult Index() =>
        View("~/Views/Report/Index.cshtml", catalog.GetReportIndex());

    [HttpGet("sections")]
    public async Task<IActionResult> Sections([FromQuery] string? q, CancellationToken token)
    {
        try
        {
            return Ok(await service.SearchSectionsAsync(q ?? string.Empty, token));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Q3 section lookup failed. TraceId={TraceId}",
                HttpContext.TraceIdentifier);
            return Failure(StatusCodes.Status503ServiceUnavailable, "無法載入科別清單，請稍後再試。");
        }
    }

    [HttpGet("doctors")]
    public async Task<IActionResult> Doctors([FromQuery] string? q, CancellationToken token)
    {
        try
        {
            return Ok(await service.SearchDoctorsAsync(q ?? string.Empty, token));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Q3 doctor lookup failed. TraceId={TraceId}",
                HttpContext.TraceIdentifier);
            return Failure(StatusCodes.Status503ServiceUnavailable, "無法載入醫師清單，請稍後再試。");
        }
    }

    [HttpPost("query")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Query(
        [FromBody] RegistrationQueryRequest? request,
        CancellationToken token)
    {
        if (request is null)
        {
            return BadRequest("查詢條件格式不正確。");
        }

        Stopwatch watch = Stopwatch.StartNew();
        try
        {
            RegistrationQueryResult result = await service.QueryAsync(request, token);
            NoStore();
            logger.LogInformation(
                "Q3 registration query completed. Mode={Mode}, Rows={Rows}, TotalCount={TotalCount}, ElapsedMs={ElapsedMs}, TraceId={TraceId}",
                result.Mode,
                result.Rows.Count,
                result.TotalCount,
                watch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Q3 registration query failed. ElapsedMs={ElapsedMs}, TraceId={TraceId}",
                watch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);
            return Failure(StatusCodes.Status503ServiceUnavailable, "掛號資料查詢暫時無法使用。");
        }
    }

    private void NoStore()
    {
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.Pragma = "no-cache";
    }

    private ObjectResult Failure(int status, string title)
    {
        NoStore();
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title
        };
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }
}
