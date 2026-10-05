using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
[Route("data-query/medical-record")]
public sealed class MedicalRecordQueryController(
    IMedicalRecordQueryService service,
    IReportCatalogService catalog,
    ILogger<MedicalRecordQueryController> logger) : Controller
{
    [HttpGet("")]
    public IActionResult Index() => View("~/Views/Report/Index.cshtml", catalog.GetReportIndex());

    [HttpPost("records")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Records(
        [FromBody] MedicalRecordQueryRequest? request,
        CancellationToken token)
    {
        if (request is null)
        {
            return BadRequest("查詢條件格式不正確。");
        }

        Stopwatch watch = Stopwatch.StartNew();
        try
        {
            MedicalRecordQueryPage result = await service.QueryAsync(request, token);
            NoStore();
            logger.LogInformation(
                "Q2 records query completed. Rows={Rows}, ElapsedMs={ElapsedMs}, TraceId={TraceId}",
                result.Rows.Count,
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
                "Q2 records query failed. ElapsedMs={ElapsedMs}, TraceId={TraceId}",
                watch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);
            return Failure(503, "病歷查詢暫時無法使用。");
        }
    }

    [HttpPost("detail")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Detail(
        [FromBody] MedicalRecordDetailRequest? request,
        CancellationToken token)
    {
        if (request is null)
        {
            return BadRequest("病歷明細條件格式不正確。");
        }

        Stopwatch watch = Stopwatch.StartNew();
        try
        {
            MedicalRecordDetail result = await service.QueryDetailAsync(request, token);
            NoStore();
            logger.LogInformation(
                "Q2 detail query completed. ElapsedMs={ElapsedMs}, TraceId={TraceId}",
                watch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(exception.Message);
        }
        catch (KeyNotFoundException)
        {
            return NotFound("查無指定的病歷資料。");
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Q2 detail query failed. ElapsedMs={ElapsedMs}, TraceId={TraceId}",
                watch.ElapsedMilliseconds,
                HttpContext.TraceIdentifier);
            return Failure(503, "病歷明細暫時無法使用。");
        }
    }

    private void NoStore()
    {
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.Pragma = "no-cache";
    }

    private ObjectResult Failure(int status, string title)
    {
        var problem = new ProblemDetails { Status = status, Title = title };
        problem.Extensions["traceId"] = HttpContext.TraceIdentifier;
        return StatusCode(status, problem);
    }
}
