using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C5ReportControllerTests
{
    [Fact]
    public async Task Query_PatientDetailWithoutPermission_ReturnsForbidAndDoesNotQuery()
    {
        var service = new FakeService();
        C5ReportController controller = Create(service, new FakeAudit(), []);
        IActionResult result = await controller.Query(new("2026-09-18", "2026-09-18",
            DetailType: C5DetailType.PatientDetail), default);
        Assert.IsType<ForbidResult>(result);
        Assert.Equal(0, service.Calls);
    }

    [Fact]
    public async Task Query_AuthorizedPatientDetail_WritesDeidentifiedAudit()
    {
        var service = new FakeService(); var audit = new FakeAudit();
        C5ReportController controller = Create(service, audit,
            [new Claim("permission", "C5.PatientDetail"), new Claim(ClaimTypes.Name, "tester")]);
        IActionResult result = await controller.Query(new("2026-09-18", "2026-09-18",
            DetailType: C5DetailType.PatientDetail), default);
        Assert.IsType<OkObjectResult>(result);
        Assert.Equal("tester", audit.Value!.UserId);
        Assert.Equal(0, audit.Value.RowCount);
        Assert.DoesNotContain("Patient", string.Join('|', audit.Value.GetType().GetProperties().Select(x => x.Name)));
    }

    [Fact]
    public void Query_RequiresAntiforgery()
    {
        var method = typeof(C5ReportController).GetMethod(nameof(C5ReportController.Query))!;
        Assert.NotNull(method.GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).SingleOrDefault());
    }

    [Fact]
    public void Catalog_ExposesC5AsAvailableEntry()
    {
        ReportDefinitionViewModel entry = new ReportCatalogService().GetReportIndex().Categories
            .SelectMany(x => x.Groups).SelectMany(x => x.Reports).Single(x => x.Code == "C5");
        Assert.Equal("批價數量查詢表", entry.Name);
        string app = File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "wwwroot", "js", "report-app.js")));
        Assert.Contains("C5: window.ReportComponents.C5Report", app);
    }

    [Fact]
    public void Catalog_ExposesC6ThroughSharedC5Ui()
    {
        ReportDefinitionViewModel entry = new ReportCatalogService().GetReportIndex().Categories
            .SelectMany(x => x.Groups).SelectMany(x => x.Reports).Single(x => x.Code == "C6");
        Assert.Equal("急診特殊檢查治療查詢表", entry.Name);
        string app = File.ReadAllText(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
            "..", "..", "..", "..", "wwwroot", "js", "report-app.js")));
        Assert.Contains("C6: window.ReportComponents.C5Report", app);
    }

    private static C5ReportController Create(IC5ReportService service, IC5PatientAccessAuditWriter audit,
        Claim[] claims)
    {
        var controller = new C5ReportController(service, new FakeOrganization(), audit);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
        { User = new ClaimsPrincipal(new ClaimsIdentity(claims, claims.Length == 0 ? null : "test")) } };
        return controller;
    }
    private sealed class FakeService : IC5ReportService
    {
        public int Calls { get; private set; }
        public Task<C5ReportResult> QueryAsync(C5ReportRequest request, CancellationToken cancellationToken = default)
        {
            Calls++; var validated=request.Validate();
            return Task.FromResult(new C5ReportResult(validated, [], new()
            { Data=[], Columns=[], TotalCount=0, PageNumber=1, PageSize=10, TotalPages=0 }, [C5QueryId.OpdDrugDetail]));
        }
    }
    private sealed class FakeAudit : IC5PatientAccessAuditWriter
    { public C5PatientAccessAudit? Value { get; private set; } public Task WriteAsync(C5PatientAccessAudit audit, CancellationToken cancellationToken=default){Value=audit;return Task.CompletedTask;} }
    private sealed class FakeOrganization : IOrganizationUnitCodeService
    {
        public Task<OrganizationUnitMapping?> ResolveLegacyCodeAsync(string newCode,bool activePlaceOnly,CancellationToken cancellationToken=default)=>Task.FromResult<OrganizationUnitMapping?>(null);
        public Task<OrganizationUnitMapping?> ResolveNewCodeAsync(string legacyCode,string roomType,OrganizationUnitMappingScope scope,CancellationToken cancellationToken=default)=>Task.FromResult<OrganizationUnitMapping?>(null);
        public Task<IReadOnlyList<OrganizationUnitMapping>> SearchAsync(string query,bool includeSections,bool includePlaces,bool activePlaceOnly,int limit=20,CancellationToken cancellationToken=default)=>Task.FromResult<IReadOnlyList<OrganizationUnitMapping>>([]);
    }
}
