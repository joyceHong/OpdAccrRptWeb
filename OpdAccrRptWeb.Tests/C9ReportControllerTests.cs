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

public sealed class C9ReportControllerTests
{
    [Fact]
    public void Controller_RequiresAuthenticationWithoutC9SpecificPolicy()
    {
        var attribute = Assert.Single(typeof(C9ReportController).GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>());
        Assert.Null(attribute.Policy);
        Assert.Null(attribute.Roles);
    }

    [Fact]
    public void QueryAndPreview_RequireAntiforgery()
    {
        foreach (string method in new[] { nameof(C9ReportController.Query), nameof(C9ReportController.Preview) })
            Assert.Single(typeof(C9ReportController).GetMethod(method)!.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true));
    }

    [Fact]
    public async Task Query_ReturnsPageAndPrivacySafeAudit()
    {
        var audit = new Audit(); var controller = Create(new Service(true), audit);
        var result = Assert.IsType<OkObjectResult>(await controller.Query(Request(), default));
        Assert.IsType<ReportDataAndColumns<C9MaterialAccountingMonthlyViewModel>>(result.Value);
        Assert.Equal("C9_SPAY6_ORDER_DETAIL_V1", audit.Value!.QueryId);
        Assert.Equal("NotRequested", audit.Value.DetailRendererStatus);
    }

    [Fact]
    public async Task QueryAndPreview_WithoutC9Permission_StillExecuteService()
    {
        var service = new Service(true);
        var controller = Create(service, new Audit());
        Assert.IsType<OkObjectResult>(await controller.Query(Request(), default));
        Assert.IsType<ViewResult>(await controller.Preview(Request(), default));
        Assert.Equal(2, service.Calls);
    }

    [Fact]
    public async Task Preview_OrdersGroupsAndIncludesBothRendererStates()
    {
        var audit = new Audit(); var controller = Create(new Service(true), audit);
        var view = Assert.IsType<ViewResult>(await controller.Preview(Request(), default));
        var model = Assert.IsType<C9PreviewViewModel>(view.Model);
        Assert.Equal(["A", "B"], model.Groups.Select(group => group.ChargeCode));
        Assert.Equal("Success", audit.Value!.SummaryRendererStatus);
        Assert.Equal("Success", audit.Value.DetailRendererStatus);
    }

    [Fact]
    public void Catalog_ExposesC9()
    {
        var entry = new ReportCatalogService().GetReportIndex().Categories.SelectMany(c => c.Groups)
            .SelectMany(g => g.Reports).Single(r => r.Code == "C9");
        Assert.Equal("維康耗材記帳月報表", entry.Name);
    }

    private static C9ReportController Create(IC9ReportService service, IC9PatientAccessAuditWriter audit)
    {
        var controller = new C9ReportController(service, audit, TimeProvider.System,
            NullLogger<C9ReportController>.Instance);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.Name, "tester")
            ], "test")), TraceIdentifier = "trace-c9"
        }};
        return controller;
    }

    private static C9ReportRequest Request() => new(new(2026, 1, 1), new(2026, 1, 2));
    private sealed class Service(bool rows) : IC9ReportService
    {
        public int Calls { get; private set; }
        public Task<C9ReportResult> QueryAsync(C9ReportRequest request, string actor, CancellationToken token = default)
        {
            Calls++;
            C9ValidatedRequest validated = request.Validate();
            IReadOnlyList<C9ReportRow> all = rows ? [
                new("1150101","M1","P1","D1","S1","U1","B","Name B",1,2,3),
                new("1150101","M2","P2","D2","S2","U2","A","Name A",4,5,9)
            ] : [];
            return Task.FromResult(new C9ReportResult(validated, all, new()
            { Columns=[], Data=[], TotalCount=all.Count, PageNumber=1, PageSize=10, TotalPages=all.Count }, "C9_SPAY6_ORDER_DETAIL_V1"));
        }
    }
    private sealed class Audit : IC9PatientAccessAuditWriter
    {
        public C9PatientAccessAudit? Value { get; private set; }
        public Task WriteAsync(C9PatientAccessAudit audit, CancellationToken token = default) { Value=audit; return Task.CompletedTask; }
    }
}
