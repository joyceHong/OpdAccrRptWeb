using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
[Route("reports/c9")]
public sealed class C9ReportController(IC9ReportService service, IC9PatientAccessAuditWriter audit,
    TimeProvider timeProvider, ILogger<C9ReportController> logger) : Controller
{
    [HttpGet("")] public IActionResult Index() => Redirect("/Report/C9");

    [HttpPost("query"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Query([FromBody] C9ReportRequest request, CancellationToken token)
    {
        var timer=Stopwatch.StartNew(); string actor=User.Identity?.Name??string.Empty;
        try { var result=await service.QueryAsync(request,actor,token); await WriteAudit(result,actor,timer,"NotRequested","NotRequested","Success",token); NoStore(); return Ok(result.Page); }
        catch(ArgumentException e){return BadRequest(e.Message);} catch(OperationCanceledException) when(token.IsCancellationRequested){throw;}
        catch(Exception e){logger.LogError(e,"C9 query failed. CorrelationId={CorrelationId}",HttpContext.TraceIdentifier);return Failure("C9 報表查詢失敗");}
    }

    [HttpPost("preview"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview([FromForm] C9ReportRequest request, CancellationToken token)
    {
        var timer=Stopwatch.StartNew(); string actor=User.Identity?.Name??string.Empty; C9ReportResult? result=null;
        try { result=await service.QueryAsync(request with{PageNumber=1},actor,token); if(result.AllRows.Count==0)return NotFound("查無此筆資料"); var now=timeProvider.GetLocalNow(); var model=new C9PreviewViewModel(actor,FormatRoc(result.Request.StartDate),FormatRoc(result.Request.EndDate),$"{now.Year-1911:000}/{now:MM/dd  HH:mm:ss}","OpdAccrRptWeb-25","ReportOpdAcc-25",C9ReportService.Group(result.AllRows)); await WriteAudit(result,actor,timer,"Success","Success","Preview",token); NoStore(); return View("~/Views/C9/Preview.cshtml",model); }
        catch(ArgumentException e){return BadRequest(e.Message);} catch(OperationCanceledException) when(token.IsCancellationRequested){throw;}
        catch(Exception e){logger.LogError(e,"C9 preview failed. CorrelationId={CorrelationId}",HttpContext.TraceIdentifier);if(result is not null)await WriteAudit(result,actor,timer,"Success","Failed","Incomplete",token);return Failure("C9 報表預覽未完整產生");}
    }
    private Task WriteAudit(C9ReportResult r,string actor,Stopwatch timer,string summary,string detail,string outcome,CancellationToken token)=>audit.WriteAsync(new(actor,r.Request.StartDate,r.Request.EndDate,timeProvider.GetLocalNow(),r.AllRows.Count,r.QueryId,summary,detail,timer.ElapsedMilliseconds,outcome,HttpContext.TraceIdentifier),token);
    private ObjectResult Failure(string title){var d=new ProblemDetails{Status=500,Title=$"{title}，請提供追蹤碼給系統管理人員。"};d.Extensions["traceId"]=HttpContext.TraceIdentifier;return StatusCode(500,d);}
    private void NoStore(){Response.Headers.CacheControl="private, no-store";Response.Headers.Pragma="no-cache";}
    private static string FormatRoc(DateOnly d)=>$"{d.Year-1911:000}/{d:MM/dd}";
}
