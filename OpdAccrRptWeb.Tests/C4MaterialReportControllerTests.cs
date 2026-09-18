using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C4MaterialReportControllerTests
{
    [Fact]
    public void Application_ConfiguresAuthenticationForAuthorizedC4Endpoints()
    {
        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
        string program = File.ReadAllText(Path.Combine(projectRoot, "Program.cs"));
        int authenticationRegistration = program.IndexOf("AddAuthentication", StringComparison.Ordinal);
        int authenticationMiddleware = program.IndexOf("UseAuthentication", StringComparison.Ordinal);
        int authorizationMiddleware = program.IndexOf("UseAuthorization", StringComparison.Ordinal);

        Assert.True(authenticationRegistration >= 0, "Authorized C4 endpoints require an authentication handler.");
        Assert.True(authenticationMiddleware >= 0 && authenticationMiddleware < authorizationMiddleware,
            "Authentication middleware must run before authorization middleware.");
    }

    [Fact]
    public void ReportShell_RequiresAuthenticationBeforeIssuingAntiForgeryToken()
    {
        Assert.NotNull(typeof(ReportController).GetMethod(nameof(ReportController.Index))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), true).SingleOrDefault());
    }

    [Fact]
    public void Controller_IsAuthorizedAndPostActionsUseAntiForgery()
    {
        Assert.NotNull(typeof(C4MaterialReportController).GetCustomAttributes(typeof(AuthorizeAttribute), true).SingleOrDefault());
        foreach (string method in new[] { "Query", "Preview", "ExportPdf" })
            Assert.NotNull(typeof(C4MaterialReportController).GetMethod(method)!
                .GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true).SingleOrDefault());
        Assert.DoesNotContain(typeof(C4MaterialReportController).GetMethods(), method =>
            method.Name.Contains("Rebuild", StringComparison.OrdinalIgnoreCase)
            || method.Name.Contains("Lock", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Query_ReturnsPagedResult()
    {
        var service = new StubC4ReportService();
        var controller = new C4MaterialReportController(service, new FakeC4OrganizationService(),
            new C4MaterialReportRenderer(), NullLogger<C4MaterialReportController>.Instance);
        OkObjectResult actual = Assert.IsType<OkObjectResult>(await controller.Query(
            new("2026-09-01", "2026-09-01"), default));
        Assert.IsType<ReportDataAndColumns<C4MaterialReportViewModel>>(actual.Value);
    }

    [Fact]
    public async Task ResolveOrganizationUnit_DelegatesNewToLegacyConversionToSharedService()
    {
        var organizationService = new CapturingOrganizationService
        {
            Mapping = new(OrganizationUnitSource.Section, "0201", "11910", "急診", true)
        };
        var controller = new C4MaterialReportController(new StubC4ReportService(), organizationService,
            new C4MaterialReportRenderer(), NullLogger<C4MaterialReportController>.Instance);

        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.ResolveOrganizationUnit(
            " 11910 ", default));
        OrganizationUnitMapping mapping = Assert.IsType<OrganizationUnitMapping>(result.Value);

        Assert.Equal("11910", organizationService.NewCode);
        Assert.True(organizationService.ActivePlaceOnly);
        Assert.Equal("0201", mapping.LegacyCode);
    }

    [Fact]
    public async Task ResolveOrganizationUnit_RejectsMissingMappingInsteadOfFallingBackToAll()
    {
        var controller = new C4MaterialReportController(new StubC4ReportService(),
            new CapturingOrganizationService(), new C4MaterialReportRenderer(),
            NullLogger<C4MaterialReportController>.Instance);

        BadRequestObjectResult result = Assert.IsType<BadRequestObjectResult>(
            await controller.ResolveOrganizationUnit("MISSING", default));

        Assert.Equal("查無對應的科別／部門舊代碼。", result.Value);
    }

    [Fact]
    public async Task ResolveOrganizationUnit_ReturnsSharedAmbiguityError()
    {
        var controller = new C4MaterialReportController(new StubC4ReportService(),
            new CapturingOrganizationService
            {
                Exception = new OrganizationUnitMappingAmbiguousException("11910")
            }, new C4MaterialReportRenderer(), NullLogger<C4MaterialReportController>.Instance);

        BadRequestObjectResult result = Assert.IsType<BadRequestObjectResult>(
            await controller.ResolveOrganizationUnit("11910", default));

        Assert.Contains("對應到多個舊代碼", Assert.IsType<string>(result.Value));
    }

    [Fact]
    public async Task Preview_ReturnsStructuredModelForInShellModal()
    {
        var controller = new C4MaterialReportController(new StubC4ReportService(),
            new CapturingOrganizationService(), new C4MaterialReportRenderer(),
            NullLogger<C4MaterialReportController>.Instance);

        OkObjectResult result = Assert.IsType<OkObjectResult>(await controller.Preview(
            new("2026-09-01", "2026-09-01"), default));

        Assert.IsType<C4MaterialPreviewViewModel>(result.Value);
    }

    private sealed class CapturingOrganizationService : IOrganizationUnitCodeService
    {
        public OrganizationUnitMapping? Mapping { get; init; }
        public Exception? Exception { get; init; }
        public string? NewCode { get; private set; }
        public bool ActivePlaceOnly { get; private set; }

        public Task<OrganizationUnitMapping?> ResolveLegacyCodeAsync(string newCode,
            bool activePlaceOnly, CancellationToken cancellationToken = default)
        {
            NewCode = newCode;
            ActivePlaceOnly = activePlaceOnly;
            if (Exception is not null) throw Exception;
            return Task.FromResult(Mapping);
        }

        public Task<OrganizationUnitMapping?> ResolveNewCodeAsync(string legacyCode, string roomType,
            OrganizationUnitMappingScope scope, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<OrganizationUnitMapping>> SearchAsync(string query,
            bool includeSections, bool includePlaces, bool activePlaceOnly, int limit = 20,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}

internal sealed class StubC4ReportService : IC4MaterialReportService
{
    private static readonly C4ValidatedRequest Validated = new C4MaterialReportRequest(
        "2026-09-01", "2026-09-01").Validate();
    public Task<C4MaterialReportResult> QueryAsync(C4MaterialReportRequest request,
        CancellationToken cancellationToken = default) => Task.FromResult(new C4MaterialReportResult(
            Validated, [], new() { Data = [], Columns = [], TotalCount = 0, PageNumber = 1, PageSize = 10, TotalPages = 0 }));
    public Task<C4MaterialPreviewViewModel?> CreatePreviewAsync(C4MaterialReportRequest request,
        string userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<C4MaterialPreviewViewModel?>(new(new("115/09/01", "115/09/01", userId,
            "115/09/17  10:30:00"), []));
}
