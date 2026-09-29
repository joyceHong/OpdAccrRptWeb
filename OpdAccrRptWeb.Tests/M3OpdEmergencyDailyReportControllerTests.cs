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

public sealed class M3OpdEmergencyDailyReportControllerTests
{
    [Fact]
    public void Controller_UsesAuthenticationRouteAndQueryAntiforgery()
    {
        Assert.Single(typeof(OpdEmergencyDailyReportController).GetCustomAttributes(typeof(AuthorizeAttribute),true));
        Assert.Single(typeof(OpdEmergencyDailyReportController).GetMethod(nameof(OpdEmergencyDailyReportController.Query))!
            .GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute),true));
        Assert.Empty(typeof(OpdEmergencyDailyReportController).GetMethod(nameof(OpdEmergencyDailyReportController.Preview))!
            .GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute),true));
    }

    [Fact]
    public async Task QueryPreviewAndExport_UseOwnedSnapshot()
    {
        var service=new Service(); var audit=new Audit(); var controller=Create(service,audit);
        Assert.IsType<OkObjectResult>(await controller.Query(new(new(2026,9,24)),default));
        Assert.IsType<ViewResult>(await controller.Preview(service.Snapshot.RunId,default));
        Assert.IsType<FileContentResult>(await controller.Export(service.Snapshot.RunId,"xlsx",default));
        Assert.IsType<BadRequestObjectResult>(await controller.Export(service.Snapshot.RunId,"pdf",default));
        Assert.Equal("trace-m3",audit.Value!.CorrelationId);
        Assert.Equal("private, no-store",controller.Response.Headers.CacheControl);
    }

    [Fact]
    public async Task InvalidInputMissingRunAndUnsupportedFormat_UseStableStatuses()
    {
        var invalid=Create(new Service{Invalid=true},new Audit());
        Assert.IsType<BadRequestObjectResult>(await invalid.Query(new(null),default));
        var missing=Create(new Service(),new Audit());
        Assert.IsType<NotFoundObjectResult>(await missing.Preview("missing",default));
        Assert.IsType<BadRequestObjectResult>(await missing.Export("run","csv",default));
    }

    [Fact]
    public void Catalog_ExposesM3WithStableName()
    {
        var medical=new ReportCatalogService().GetReportIndex().Categories.Single(x=>x.Key=="medical");
        var m3=medical.Groups.SelectMany(x=>x.Reports).Single(x=>x.Code=="M3");
        Assert.Equal("門急診日報表",m3.Name);
    }

    private static OpdEmergencyDailyReportController Create(Service service,Audit audit)
    {
        var controller=new OpdEmergencyDailyReportController(service,new M3OpdEmergencyDailyReportRenderer(),audit,
            new ReportCatalogService(),TimeProvider.System,NullLogger<OpdEmergencyDailyReportController>.Instance);
        controller.ControllerContext=new ControllerContext{HttpContext=new DefaultHttpContext{
            User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name,"alice")],"test")),TraceIdentifier="trace-m3"}};
        return controller;
    }
    private sealed class Service : IM3OpdEmergencyDailyReportService
    {
        public bool Invalid{get;init;}
        public M3OpdEmergencyDailyReportSnapshot Snapshot{get;}=new("run","alice",new(2026,9,24),DateTimeOffset.Now,
            [M3ReportRunStoreTests.Row()],M3ReportRunStoreTests.Kpis(),123);
        public Task<M3OpdEmergencyDailyPagedResponse> QueryAsync(M3OpdEmergencyDailyReportRequest r,string a,CancellationToken t=default)
        {if(Invalid)throw new ArgumentException("bad");return Task.FromResult(new M3OpdEmergencyDailyPagedResponse(Snapshot.RunId,Snapshot.Rows,M3OpdEmergencyDailyReportService.Columns,Snapshot.NineKpis,1,1,10,1));}
        public Task<M3OpdEmergencyDailyReportSnapshot> GenerateAsync(M3OpdEmergencyDailyReportRequest r,string a,CancellationToken t=default)=>Task.FromResult(Snapshot);
        public bool TryGetRun(string id,string actor,out M3OpdEmergencyDailyReportSnapshot snapshot){snapshot=Snapshot;return id==Snapshot.RunId&&actor==Snapshot.Actor;}
    }
    private sealed class Audit : IM3PatientAccessAuditWriter
    {public M3PatientAccessAudit? Value{get;private set;} public Task WriteAsync(M3PatientAccessAudit value,CancellationToken token=default){Value=value;return Task.CompletedTask;}}
}
