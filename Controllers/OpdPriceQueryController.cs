using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
namespace OpdAccrRptWeb.Controllers;

[Authorize]
[Route("data-query/opd-price")]
public sealed class OpdPriceQueryController(IOpdPriceQueryService service, IOpdPricePatientAccessAuditWriter audit, IReportCatalogService catalog, ILogger<OpdPriceQueryController> logger) : Controller
{
    [HttpGet("")] public IActionResult Index() => View("~/Views/Report/Index.cshtml", catalog.GetReportIndex());
    [HttpGet("sections")]
    public async Task<IActionResult> Sections([FromQuery] string? q, CancellationToken token)
    { try { return Ok(await service.SearchSectionsAsync(q ?? string.Empty, token)); } catch (Exception exception) { logger.LogError(exception, "Q1 section search failed. TraceId={TraceId}", HttpContext.TraceIdentifier); return Failure(503, "無法載入科別清單，仍可直接輸入科別代碼。"); } }
    [HttpPost("visits"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Visits([FromBody] OpdPriceVisitRequest request, CancellationToken token)
    { var watch = Stopwatch.StartNew(); string actor = Actor(); string fingerprint = Fingerprint(request.MedicalRecordNo, request.VisitDate?.ToString(), request.SectionCode); try { OpdPriceVisitPage result = await service.QueryVisitsAsync(request, actor, token); NoStore(); await Audit(actor, "Visits", "Success", result.Rows.Count, watch, fingerprint, token); return Ok(result); } catch (ArgumentException ex) { await Audit(actor, "Visits", "Invalid", 0, watch, fingerprint, CancellationToken.None); return BadRequest(ex.Message); } catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; } catch (Exception ex) { logger.LogError(ex, "Q1 visits failed. TraceId={TraceId}", HttpContext.TraceIdentifier); await Audit(actor, "Visits", "Failed", 0, watch, fingerprint, CancellationToken.None); return Failure(503, "批價查詢暫時無法使用。"); } }
    [HttpPost("detail"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Detail([FromBody] OpdPriceDetailRequest request, CancellationToken token)
    { var watch = Stopwatch.StartNew(); string actor = Actor(); try { OpdPriceDetail result = await service.QueryDetailAsync(request, actor, token); NoStore(); await Audit(actor, "Detail", "Success", result.Charges.Count, watch, string.Empty, token); return Ok(result); } catch (KeyNotFoundException) { await Audit(actor, "Detail", "NotFound", 0, watch, string.Empty, CancellationToken.None); return NotFound("查無指定的就診資料。"); } catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; } catch (Exception ex) { logger.LogError(ex, "Q1 detail failed. TraceId={TraceId}", HttpContext.TraceIdentifier); await Audit(actor, "Detail", "Failed", 0, watch, string.Empty, CancellationToken.None); return Failure(503, "就診明細暫時無法使用。"); } }
    [HttpPost("basic"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Basic([FromBody] OpdPriceDetailRequest request, CancellationToken token)
    { var watch = Stopwatch.StartNew(); string actor = Actor(); try { OpdPriceBasic result = await service.QueryBasicAsync(request.VisitToken ?? string.Empty, actor, token); NoStore(); await Audit(actor, "Basic", "Success", 1, watch, string.Empty, token); return Ok(result); } catch (KeyNotFoundException) { await Audit(actor, "Basic", "NotFound", 0, watch, string.Empty, CancellationToken.None); return NotFound("查無指定的就診資料。"); } catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; } catch (Exception ex) { logger.LogError(ex, "Q1 basic failed. TraceId={TraceId}", HttpContext.TraceIdentifier); await Audit(actor, "Basic", "Failed", 0, watch, string.Empty, CancellationToken.None); return Failure(503, "病患資料暫時無法使用。"); } }
    [HttpGet("receipt")]
    public async Task<IActionResult> Receipt([FromQuery] string token, CancellationToken cancellationToken)
    { var watch = Stopwatch.StartNew(); string actor = Actor(); try { OpdReceiptPreview result = await service.CreateReceiptAsync(token, actor, cancellationToken); NoStore(); await Audit(actor, "Receipt", "Success", result.ItemRows.Sum(x => x.Count), watch, string.Empty, cancellationToken); return View(result); } catch (KeyNotFoundException) { await Audit(actor, "Receipt", "NotFound", 0, watch, string.Empty, CancellationToken.None); return NotFound("收據已失效或查無可列印資料。"); } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; } catch (Exception ex) { logger.LogError(ex, "Q1 receipt failed. TraceId={TraceId}", HttpContext.TraceIdentifier); await Audit(actor, "Receipt", "Failed", 0, watch, string.Empty, CancellationToken.None); return Failure(503, "收據預覽暫時無法使用。"); } }
    [HttpPost("receipts"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Receipts([FromForm] string? visitToken, [FromForm] string[] receiptTokens, CancellationToken cancellationToken)
    {
        var watch = Stopwatch.StartNew(); string actor = Actor();
        try
        {
            OpdPriceReceiptBatch result = await service.CreateReceiptBatchAsync(new(visitToken, receiptTokens), actor, cancellationToken);
            NoStore();
            await Audit(actor, "ReceiptBatch", result.FailedCount == 0 ? "Success" : result.ReadyCount == 0 ? "Failed" : "Partial",
                result.ReadyCount, watch, string.Empty, cancellationToken);
            return View("BatchReceipt", result);
        }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException) { return NotFound("就診資料已失效，請重新查詢。"); }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            logger.LogError(ex, "Q1 batch receipt failed. TraceId={TraceId}", HttpContext.TraceIdentifier);
            await Audit(actor, "ReceiptBatch", "Failed", 0, watch, string.Empty, CancellationToken.None);
            return Failure(503, "收據預覽暫時無法使用。");
        }
    }
    private string Actor() => User.Identity?.Name ?? string.Empty; private void NoStore() { Response.Headers.CacheControl = "private, no-store"; Response.Headers.Pragma = "no-cache"; }
    private Task Audit(string actor, string op, string outcome, int rows, Stopwatch watch, string fingerprint, CancellationToken token) => audit.WriteAsync(new(actor, op, outcome, rows, watch.ElapsedMilliseconds, HttpContext.TraceIdentifier, fingerprint), token);
    private static string Fingerprint(params string?[] values) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('|', values))));
    private ObjectResult Failure(int status, string title) { var p = new ProblemDetails { Status = status, Title = title }; p.Extensions["traceId"] = HttpContext.TraceIdentifier; return StatusCode(status, p); }
}
