using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportQueryPermissionFilterTests
{
    [Fact]
    public async Task DirectReportUrlWithoutQueryGrantReturnsForbidden()
    {
        var context = CreateContext("Report", "Index",
            new Dictionary<string, object?> { ["reportCode"] = "C21" });
        var filter = CreateFilter(new ReportPermissionSnapshot(
            "E100", false, new Dictionary<string, ReportAction>
            {
                ["C22"] = ReportAction.Query
            }));
        bool actionCalled = false;

        await filter.OnActionExecutionAsync(context, () =>
        {
            actionCalled = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(context.HttpContext, context.RouteData, context.ActionDescriptor),
                [], new object()));
        });

        Assert.False(actionCalled);
        Assert.Equal(403, Assert.IsType<StatusCodeResult>(context.Result).StatusCode);
    }

    [Fact]
    public async Task QueryEndpointWithoutGrantReturnsForbiddenBeforeDispatch()
    {
        var context = CreateContext("Report", "GetReportData",
            new Dictionary<string, object?>
            {
                ["searchCondition"] = new SearchReportCondition { ReportCode = "C21" }
            });
        var filter = CreateFilter(new ReportPermissionSnapshot(
            "E100", false, new Dictionary<string, ReportAction>()));

        await filter.OnActionExecutionAsync(context, () =>
            throw new InvalidOperationException("Query action must not run"));

        Assert.Equal(403, Assert.IsType<StatusCodeResult>(context.Result).StatusCode);
    }

    [Theory]
    [InlineData("Export", "C21", false, ReportAction.Query)]
    [InlineData("GetReportData", "C21", true, ReportAction.Excel)]
    public async Task ReportActionsRequireTheirOwnGrant(
        string action, string code, bool forceRebuild, ReportAction grantedAction)
    {
        var context = CreateContext("Report", action,
            new Dictionary<string, object?>
            {
                ["searchCondition"] = new SearchReportCondition
                {
                    ReportCode = code,
                    ForceRebuild = forceRebuild
                }
            });
        var filter = CreateFilter(new ReportPermissionSnapshot("E100", false,
            new Dictionary<string, ReportAction> { [code] = grantedAction }));

        await filter.OnActionExecutionAsync(context, () =>
            throw new InvalidOperationException("Action must not run"));

        Assert.Equal(403, Assert.IsType<StatusCodeResult>(context.Result).StatusCode);
    }

    [Theory]
    [InlineData("Report", "PreviewC1", "C1")]
    [InlineData("OpdPriceQuery", "Receipt", "Q1")]
    [InlineData("SapInterface", "Run", "SAP")]
    public async Task DedicatedActionsRequireMoreThanQueryGrant(
        string controller, string action, string code)
    {
        var context = CreateContext(controller, action,
            new Dictionary<string, object?>());
        if (controller == "Report")
            context.HttpContext.Request.Path = "/Report/C1/Preview";
        var filter = CreateFilter(new ReportPermissionSnapshot("E100", false,
            new Dictionary<string, ReportAction> { [code] = ReportAction.Query }));

        await filter.OnActionExecutionAsync(context, () =>
            throw new InvalidOperationException("Action must not run"));

        Assert.Equal(403, Assert.IsType<StatusCodeResult>(context.Result).StatusCode);
    }

    [Theory]
    [InlineData("GetExportStatus")]
    [InlineData("DownloadExport")]
    public async Task ExportJobRequiresCurrentExcelGrantAndOwner(string action)
    {
        Guid id = Guid.NewGuid();
        var context = CreateContext("Report", action,
            new Dictionary<string, object?> { ["jobId"] = id.ToString("D") });
        var job = new ReportExportJob
        {
            JobId = id,
            SearchCondition = new SearchReportCondition { ReportCode = "C1" },
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "E100"
        };
        var queryOnly = CreateFilter(new ReportPermissionSnapshot("E100", false,
            new Dictionary<string, ReportAction> { ["C1"] = ReportAction.Query }), job);
        await queryOnly.OnActionExecutionAsync(context, () =>
            throw new InvalidOperationException("Revoked job must not run"));
        Assert.Equal(403, Assert.IsType<StatusCodeResult>(context.Result).StatusCode);

        context.Result = null;
        var otherOwner = new ReportExportJob
        {
            JobId = id,
            SearchCondition = new SearchReportCondition { ReportCode = "C1" },
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = "E200"
        };
        var permitted = new ReportPermissionSnapshot("E100", false,
            new Dictionary<string, ReportAction>
            {
                ["C1"] = ReportAction.Query | ReportAction.Excel
            });
        await CreateFilter(permitted, otherOwner).OnActionExecutionAsync(context, () =>
            throw new InvalidOperationException("Other owner's job must not run"));
        Assert.IsType<NotFoundResult>(context.Result);

        context.Result = null;
        bool executed = false;
        await CreateFilter(permitted, job).OnActionExecutionAsync(context, () =>
        {
            executed = true;
            return Task.FromResult(new ActionExecutedContext(
                new ActionContext(context.HttpContext, context.RouteData, context.ActionDescriptor),
                [], new object()));
        });
        Assert.True(executed);
    }

    [Fact]
    public async Task DefaultLandingSkipsDeniedC1AndUsesFirstGrantedReport()
    {
        var context = CreateContext("Report", "Root", new Dictionary<string, object?>());
        var filter = CreateFilter(new ReportPermissionSnapshot(
            "E100", false, new Dictionary<string, ReportAction>
            {
                ["C21"] = ReportAction.Query
            }));

        await filter.OnActionExecutionAsync(context, () =>
            throw new InvalidOperationException("Root action must not run"));

        Assert.Equal("/Report/C21", Assert.IsType<RedirectResult>(context.Result).Url);
    }

    [Fact]
    public async Task NoGrantedReportRendersEmptyCatalog()
    {
        var context = CreateContext("Report", "Index", new Dictionary<string, object?>());
        var filter = CreateFilter(new ReportPermissionSnapshot(
            "E100", false, new Dictionary<string, ReportAction>()));

        await filter.OnActionExecutionAsync(context, () =>
            throw new InvalidOperationException("Index action must not run"));

        var view = Assert.IsType<ViewResult>(context.Result);
        Assert.Empty(Assert.IsType<OpdAccrRptWeb.ViewModels.ReportIndexViewModel>(
            view.Model).Categories);
    }

    [Fact]
    public async Task PermissionStoreFailureDoesNotLookLikeEmptyPermissions()
    {
        var context = CreateContext("Report", "Root", new Dictionary<string, object?>());
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReportPermissions:Enforce"] = "true"
            }).Build();
        var filter = new ReportQueryPermissionFilter(
            new FakeService(null, new InvalidOperationException("database unavailable")),
            new ReportCatalogService(), new FakeExports(), configuration,
            NullLogger<ReportQueryPermissionFilter>.Instance);

        await filter.OnActionExecutionAsync(context, () =>
            throw new InvalidOperationException("Root action must not run"));

        Assert.Equal(503, Assert.IsType<StatusCodeResult>(context.Result).StatusCode);
    }

    private static ReportQueryPermissionFilter CreateFilter(
        ReportPermissionSnapshot snapshot, ReportExportJob? job = null)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReportPermissions:Enforce"] = "true"
            }).Build();
        return new ReportQueryPermissionFilter(
            new FakeService(snapshot), new ReportCatalogService(), new FakeExports(job), configuration,
            NullLogger<ReportQueryPermissionFilter>.Instance);
    }

    private static ActionExecutingContext CreateContext(
        string controller, string action, IDictionary<string, object?> arguments)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Name, "E100")], "test"))
        };
        var descriptor = new ActionDescriptor();
        descriptor.RouteValues["controller"] = controller;
        descriptor.RouteValues["action"] = action;
        var actionContext = new ActionContext(httpContext, new RouteData(), descriptor);
        return new ActionExecutingContext(actionContext, [], arguments, new object());
    }

    private sealed class FakeService(
        ReportPermissionSnapshot? snapshot, Exception? exception = null) : IReportPermissionService
    {
        public Task<ReportPermissionSnapshot> ResolveAsync(
            string? loginName, CancellationToken cancellationToken = default) =>
            exception is null
                ? Task.FromResult(snapshot!)
                : Task.FromException<ReportPermissionSnapshot>(exception);
    }

    private sealed class FakeExports(ReportExportJob? job = null) : IReportExportService
    {
        public ReportExportDispatchResult Dispatch(
            SearchReportCondition condition, string? ownerIdentity = null) =>
            throw new NotSupportedException();

        public ReportExportJob? GetJob(Guid jobId, string? requesterIdentity = null) =>
            job?.JobId == jobId ? job : null;

        public ReportExportDownloadResult GetDownload(
            Guid jobId, string? requesterIdentity = null) =>
            throw new NotSupportedException();
    }
}
