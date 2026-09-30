using System.Reflection;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class OpdPriceQueryControllerTests
{
    [Fact] public void Controller_RequiresAuthorizationAndPostAntiforgery()
    { Assert.NotNull(typeof(OpdPriceQueryController).GetCustomAttribute<AuthorizeAttribute>());Assert.NotNull(typeof(OpdPriceQueryController).GetMethod(nameof(OpdPriceQueryController.Visits))!.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());Assert.NotNull(typeof(OpdPriceQueryController).GetMethod(nameof(OpdPriceQueryController.Detail))!.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()); }

    [Fact] public async Task Visits_ReturnsNoStoreAndAuditsWithoutIdentifier()
    { var service=new FakeService{Page=new([],0,1,10,0)};var audit=new Audit();var controller=Create(service,audit);IActionResult result=await controller.Visits(new("SECRET-MR",new DateOnly(2026,9,30),null),default);Assert.IsType<OkObjectResult>(result);Assert.Equal("private, no-store",controller.Response.Headers.CacheControl);Assert.Equal("Visits",audit.Value?.Operation);Assert.DoesNotContain("SECRET-MR",audit.Value?.ConditionFingerprint??""); }

    [Fact] public async Task InvalidAndMissingTokensReturnSafeResponses()
    { var service=new FakeService{Error=new ArgumentException("安全訊息")};var controller=Create(service,new Audit());var bad=Assert.IsType<BadRequestObjectResult>(await controller.Visits(new("",null,null),default));Assert.Equal("安全訊息",bad.Value);service.Error=new KeyNotFoundException("internal key");var missing=Assert.IsType<NotFoundObjectResult>(await controller.Detail(new("bad"),default));Assert.Equal("查無指定的就診資料。",missing.Value); }

    private static OpdPriceQueryController Create(FakeService service,Audit audit)
    { var value=new OpdPriceQueryController(service,audit,new ReportCatalogService(),NullLogger<OpdPriceQueryController>.Instance);value.ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name,"tester")],"test"))}};value.HttpContext.TraceIdentifier="trace";return value; }
    private sealed class Audit : IOpdPricePatientAccessAuditWriter { public OpdPriceAuditEvent? Value;public Task WriteAsync(OpdPriceAuditEvent value,CancellationToken token){Value=value;return Task.CompletedTask;} }
    private sealed class FakeService : IOpdPriceQueryService
    { public OpdPriceVisitPage Page=new([],0,1,10,0);public Exception? Error;public Task<OpdPriceVisitPage> QueryVisitsAsync(OpdPriceVisitRequest request,string actor,CancellationToken token)=>Error is null?Task.FromResult(Page):Task.FromException<OpdPriceVisitPage>(Error);public Task<OpdPriceDetail> QueryDetailAsync(OpdPriceDetailRequest request,string actor,CancellationToken token)=>Error is null?throw new NotSupportedException():Task.FromException<OpdPriceDetail>(Error);public Task<IReadOnlyList<OpdPriceSectionOption>> SearchSectionsAsync(string query,CancellationToken token)=>Task.FromResult<IReadOnlyList<OpdPriceSectionOption>>([]);public Task<OpdReceiptPreview> CreateReceiptAsync(string token,string actor,CancellationToken cancellationToken)=>throw new NotSupportedException(); }
}
