using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;
public sealed class C7ReportControllerTests
{
    [Fact] public async Task Query_AuthenticatedWithoutPermission_QueriesAndAuditsOnlyFilterMetadata(){var s=new FakeService();var a=new FakeAudit();var c=Create(s,a,[new Claim(ClaimTypes.Name,"tester")]);Assert.IsType<OkObjectResult>(await c.Query(Request(),default));Assert.Equal(1,s.Calls);Assert.Equal("tester",a.Value!.Actor);Assert.Equal("U1",a.Value.InputUserId);Assert.DoesNotContain("Medical",string.Join('|',a.Value.GetType().GetProperties().Select(x=>x.Name)));}
    [Fact] public async Task Preview_AuthenticatedWithoutPermission_QueriesAndAudits(){var s=new FakeService(withRows:true);var a=new FakeAudit();var c=Create(s,a,[new Claim(ClaimTypes.Name,"tester")]);Assert.IsType<ViewResult>(await c.Preview(Request(),default));Assert.Equal(1,s.Calls);Assert.Equal("tester",a.Value!.Actor);}
    [Fact] public async Task Preview_Empty_ReturnsNotFound(){var c=Create(new FakeService(),new FakeAudit(),[new Claim("permission","C7.PatientDetail")]);var r=Assert.IsType<NotFoundObjectResult>(await c.Preview(Request(),default));Assert.Equal("查無此筆資料",r.Value);}
    [Fact] public async Task Query_BlankUser_IsRejectedWithoutAudit(){var a=new FakeAudit();var c=Create(new FakeService(),a,[new Claim("permission","C7.PatientDetail")]);Assert.IsType<BadRequestObjectResult>(await c.Query(new("2026-01-01","2026-01-01",InputUserId:""),default));Assert.Null(a.Value);}
    [Fact] public void Endpoints_RequireAntiforgery(){foreach(var n in new[]{nameof(C7ReportController.Query),nameof(C7ReportController.Preview)})Assert.NotNull(typeof(C7ReportController).GetMethod(n)!.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute),true).SingleOrDefault());}
    [Fact] public void Controller_RequiresAuthentication(){Assert.NotNull(typeof(C7ReportController).GetCustomAttributes(typeof(AuthorizeAttribute),true).SingleOrDefault());Assert.Null(typeof(C7ReportController).GetMethod(nameof(C7ReportController.Query))!.GetCustomAttributes(typeof(AllowAnonymousAttribute),true).SingleOrDefault());Assert.Null(typeof(C7ReportController).GetMethod(nameof(C7ReportController.Preview))!.GetCustomAttributes(typeof(AllowAnonymousAttribute),true).SingleOrDefault());}
    [Fact] public void Catalog_ExposesC7(){var e=new ReportCatalogService().GetReportIndex().Categories.SelectMany(x=>x.Groups).SelectMany(x=>x.Reports).Single(x=>x.Code=="C7");Assert.Equal("門急診每日批價明細表",e.Name);}
    private static C7ReportRequest Request()=>new("2026-01-01","2026-01-01",InputUserId:"U1");
    private static C7ReportController Create(IC7ReportService s,IC7PatientAccessAuditWriter a,Claim[] claims){var c=new C7ReportController(s,a);c.ControllerContext=new(){HttpContext=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity(claims,claims.Length==0?null:"test"))}};return c;}
    private sealed class FakeService(bool withRows=false):IC7ReportService{public int Calls;public Task<IReadOnlyList<C7InputUser>> GetEligibleUsersAsync(string d,CancellationToken t=default)=>Task.FromResult<IReadOnlyList<C7InputUser>>([]);public Task<C7ReportResult> QueryAsync(C7ReportRequest r,CancellationToken t=default){Calls++;var v=r.Validate();IReadOnlyList<C7ReportRow> rows=withRows?[new("1150101","0800","11910","門診",null,"A1","測試",1,1,1,"M1","I1","U1","測試人員")]:[];return Task.FromResult(new C7ReportResult(v,rows,new(){Columns=[],Data=[],PageNumber=1,PageSize=v.PageSize,TotalCount=rows.Count,TotalPages=rows.Count},["C7_OPD_DRUG_DETAIL"]));}}
    private sealed class FakeAudit:IC7PatientAccessAuditWriter{public C7PatientAccessAudit? Value;public Task WriteAsync(C7PatientAccessAudit a,CancellationToken t=default){Value=a;return Task.CompletedTask;}}
}
