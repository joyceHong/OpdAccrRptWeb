using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C8ReportControllerTests
{
    [Fact] public async Task Query_WithoutC8Permission_StillExecutesReport()
    { var s=new Service();var c=Create(s,new Audit(),[new Claim(ClaimTypes.Name,"tester")]);Assert.IsType<OkObjectResult>(await c.Query(Request(),default));Assert.Equal(1,s.Calls); }
    [Fact] public async Task Preview_WithoutC8Permission_StillExecutesReport()
    { var s=new Service(true);var c=Create(s,new Audit(),[new Claim(ClaimTypes.Name,"tester")]);Assert.IsType<ViewResult>(await c.Preview(Request(),default));Assert.Equal(1,s.Calls); }
    [Fact] public async Task Query_WithPermission_ReturnsPageAndPrivacySafeAudit()
    { var s=new Service(true);var a=new Audit();var c=Create(s,a,Claims());Assert.IsType<OkObjectResult>(await c.Query(Request(),default));Assert.Equal("tester",a.Value!.Actor);Assert.DoesNotContain("Medical",string.Join('|',a.Value.GetType().GetProperties().Select(x=>x.Name)));Assert.DoesNotContain("ChargeName",string.Join('|',a.Value.GetType().GetProperties().Select(x=>x.Name))); }
    [Fact] public async Task Preview_WithPermission_RendersView()
    { var c=Create(new Service(true),new Audit(),Claims());var v=Assert.IsType<ViewResult>(await c.Preview(Request(),default));Assert.IsType<C8PreviewViewModel>(v.Model); }
    [Fact] public async Task Preview_Empty_ReturnsNotFound()
    { var c=Create(new Service(),new Audit(),Claims());Assert.IsType<NotFoundObjectResult>(await c.Preview(Request(),default)); }
    [Fact] public void Endpoints_RequireAntiforgery()
    { foreach(string n in new[]{nameof(C8ReportController.Query),nameof(C8ReportController.Preview)})Assert.Single(typeof(C8ReportController).GetMethod(n)!.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute),true)); }
    [Fact] public void Controller_RequiresAuthentication()=>Assert.Single(typeof(C8ReportController).GetCustomAttributes(typeof(AuthorizeAttribute),true));
    [Fact] public void Catalog_ExposesC8()
    { var e=new ReportCatalogService().GetReportIndex().Categories.SelectMany(x=>x.Groups).SelectMany(x=>x.Reports).Single(x=>x.Code=="C8");Assert.Equal("批價補帳明細表",e.Name); }
    private static Claim[] Claims()=>[new("permission","C8.PatientDetail"),new(ClaimTypes.Name,"tester")];
    private static C8ReportRequest Request()=>new(new(2026,1,1),new(2026,1,2));
    private static C8ReportController Create(IC8ReportService s,IC8PatientAccessAuditWriter a,Claim[] claims){var c=new C8ReportController(s,a,TimeProvider.System,NullLogger<C8ReportController>.Instance);c.ControllerContext=new(){HttpContext=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity(claims,"test")),TraceIdentifier="trace-c8"}};return c;}
    private sealed class Service(bool rows=false):IC8ReportService{public int Calls;public Task<C8ReportResult> QueryAsync(C8ReportRequest r,string a,CancellationToken t=default){Calls++;var v=r.Validate();IReadOnlyList<C8ReportRow> all=rows?[new("1150101","M1","11910","01","U1","A1","測試",1,2,3,4,5)]:[];return Task.FromResult(new C8ReportResult(v,all,new(){Columns=[],Data=[],PageNumber=1,PageSize=v.PageSize,TotalCount=all.Count,TotalPages=all.Count},"C8_PATCH_BILL_DETAIL"));}}
    private sealed class Audit:IC8PatientAccessAuditWriter{public C8PatientAccessAudit? Value;public Task WriteAsync(C8PatientAccessAudit a,CancellationToken t=default){Value=a;return Task.CompletedTask;}}
}
