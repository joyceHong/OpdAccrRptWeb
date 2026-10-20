using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportPermissionManagerFilterTests
{
    [Fact]
    public async Task TypeZeroManagerCanOpenSettings()
    {
        var context = CreateContext("F1482");
        var filter = CreateFilter(new ReportPermissionSnapshot(
            "F1482", true, new Dictionary<string, ReportAction> { ["C1"] = ReportAction.Query }));

        await filter.OnAuthorizationAsync(context);

        Assert.Null(context.Result);
    }

    [Fact]
    public async Task OrdinaryReportUserCannotOpenSettings()
    {
        var context = CreateContext("E100");
        var filter = CreateFilter(new ReportPermissionSnapshot(
            "E100", false, new Dictionary<string, ReportAction> { ["C1"] = ReportAction.Query }));

        await filter.OnAuthorizationAsync(context);

        Assert.Equal(403, Assert.IsType<StatusCodeResult>(context.Result).StatusCode);
    }

    [Fact]
    public async Task PermissionSourceFailureCannotOpenSettings()
    {
        var context = CreateContext("F1482");
        var filter = new ReportPermissionManagerFilter(
            new FakeService(null, new InvalidOperationException("database unavailable")),
            NullLogger<ReportPermissionManagerFilter>.Instance);

        await filter.OnAuthorizationAsync(context);

        Assert.Equal(503, Assert.IsType<StatusCodeResult>(context.Result).StatusCode);
    }

    [Fact]
    public async Task UnauthenticatedUserCannotOpenSettings()
    {
        var context = CreateContext(null);
        var filter = CreateFilter(new ReportPermissionSnapshot(
            null, false, new Dictionary<string, ReportAction>()));

        await filter.OnAuthorizationAsync(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    private static ReportPermissionManagerFilter CreateFilter(ReportPermissionSnapshot snapshot) =>
        new(new FakeService(snapshot, null), NullLogger<ReportPermissionManagerFilter>.Instance);

    private static AuthorizationFilterContext CreateContext(string? userId)
    {
        var httpContext = new DefaultHttpContext();
        if (userId is not null)
        {
            httpContext.User = new ClaimsPrincipal(
                new ClaimsIdentity([new Claim(ClaimTypes.Name, userId)], "test"));
        }
        return new AuthorizationFilterContext(
            new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
            []);
    }

    private sealed class FakeService(
        ReportPermissionSnapshot? snapshot,
        Exception? exception) : IReportPermissionService
    {
        public Task<ReportPermissionSnapshot> ResolveAsync(
            string? loginName, CancellationToken cancellationToken = default) =>
            exception is null
                ? Task.FromResult(snapshot!)
                : Task.FromException<ReportPermissionSnapshot>(exception);
    }
}
