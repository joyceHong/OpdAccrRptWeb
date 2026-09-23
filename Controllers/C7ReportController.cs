using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Controllers;

[Authorize]
[Route("reports/c7")]
public sealed class C7ReportController(IC7ReportService service,IC7PatientAccessAuditWriter audit):Controller
{
    [HttpGet("")][AllowAnonymous] public IActionResult Index()=>Redirect("/Report/C7");
    [HttpGet("input-users")]
    public async Task<IActionResult> Users([FromQuery]string endDate,CancellationToken token)
    {try{return Ok(await service.GetEligibleUsersAsync(endDate,token));}catch(ArgumentException e){return BadRequest(e.Message);}}
    [HttpPost("query")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Query([FromBody]C7ReportRequest request,CancellationToken token)
    {try{var timer=Stopwatch.StartNew();var result=await service.QueryAsync(request,token);await WriteAudit(result,timer,token);return Ok(result.Page);}catch(ArgumentException e){return BadRequest(e.Message);}}
    [HttpPost("preview")][ValidateAntiForgeryToken]
    public async Task<IActionResult> Preview([FromForm]C7ReportRequest request,CancellationToken token)
    {try{var timer=Stopwatch.StartNew();var result=await service.QueryAsync(request with{PageNumber=1,PageSize=50},token);if(result.AllRows.Count==0)return NotFound("查無此筆資料");await WriteAudit(result,timer,token);string D(DateOnly d)=>$"{d.Year-1911:000}/{d:MM/dd}";string T(string t)=>$"{t[..2]}:{t[2..]}";var now=DateTime.Now;return View("~/Views/C7/Preview.cshtml",new C7PreviewViewModel(User.Identity?.Name??"",D(result.Request.Start),T(result.Request.StartTime),D(result.Request.End),T(result.Request.EndTime),$"{now.Year-1911:000}/{now:MM/dd  HH:mm:ss}",result.AllRows));}catch(ArgumentException e){return BadRequest(e.Message);}}
    private Task WriteAudit(C7ReportResult r,Stopwatch t,CancellationToken token)=>audit.WriteAsync(new(User.Identity?.Name??"",r.Request.InputUserId,r.Request.Start,r.Request.End,r.Request.StartTime,r.Request.EndTime,r.Request.ChargeKind,r.AllRows.Count,t.ElapsedMilliseconds,r.QueryIds,HttpContext.TraceIdentifier),token);
}
