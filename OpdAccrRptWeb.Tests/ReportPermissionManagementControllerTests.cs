using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Controllers;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportPermissionManagementControllerTests
{
    [Fact]
    public async Task ListReturnsPageAndRejectsInvalidPagination()
    {
        var repository = new ListRepository();
        var controller = CreateController(repository);

        var valid = Assert.IsType<OkObjectResult>(
            await controller.List(null, null, 1, 20));
        var page = Assert.IsType<PermissionGrantListPage>(valid.Value);
        Assert.Equal(4, page.TotalCount);
        Assert.Equal(20, page.PageSize);
        Assert.IsType<BadRequestObjectResult>(await controller.List(null, null, 0, 20));
        Assert.IsType<BadRequestObjectResult>(await controller.List(null, null, 1, 101));
    }

    [Fact]
    public async Task ListReturnsServiceUnavailableOnRepositoryFailure()
    {
        var controller = CreateController(new ListRepository { Fail = true });

        var result = Assert.IsType<StatusCodeResult>(
            await controller.List(null, null, 1, 20));
        Assert.Equal(503, result.StatusCode);
    }

    [Fact]
    public async Task TargetListReturnsPagedTargetsAndValidatesParameters()
    {
        var controller = CreateController(new ListRepository());

        var result = Assert.IsType<OkObjectResult>(
            await controller.TargetList(null, null, 1, 20));
        var page = Assert.IsType<PermissionTargetListPage>(result.Value);
        Assert.Equal(4, page.TotalCount);
        Assert.Equal(20, page.PageSize);
        Assert.IsType<BadRequestObjectResult>(
            await controller.TargetList(null, null, 0, 20));

        controller = CreateController(new ListRepository { Fail = true });
        Assert.Equal(503, Assert.IsType<StatusCodeResult>(
            await controller.TargetList(null, null, 1, 20)).StatusCode);
    }

    [Fact]
    public async Task TargetSearchValidatesInputAndSurfacesRepositoryFailure()
    {
        var controller = CreateController(new ListRepository());
        Assert.IsType<BadRequestObjectResult>(await controller.Targets(1, "0", default));
        var found = Assert.IsType<OkObjectResult>(await controller.Targets(1, "0691", default));
        Assert.Single(Assert.IsAssignableFrom<IReadOnlyList<PermissionTargetOption>>(found.Value));

        controller = CreateController(new ListRepository { Fail = true });
        Assert.Equal(503, Assert.IsType<StatusCodeResult>(
            await controller.Targets(1, "0691", default)).StatusCode);
    }

    [Fact]
    public void ListIsProtectedByManagerFilter()
    {
        var attribute = Assert.Single(typeof(ReportPermissionManagementController)
            .GetCustomAttributes(typeof(ServiceFilterAttribute), true)
            .Cast<ServiceFilterAttribute>());
        Assert.Equal(typeof(ReportPermissionManagerFilter), attribute.ServiceType);
    }

    [Fact]
    public async Task RevokeRequiresManagerSnapshotAndAntiforgery()
    {
        var repository = new ListRepository();
        var controller = CreateController(repository);
        var request = new PermissionRevokeRequest
        {
            ReportCode = "C21", TargetType = 2, Target = "E100", ExpectedMask = 7
        };

        Assert.Equal(403, Assert.IsType<StatusCodeResult>(
            await controller.Revoke(request, default)).StatusCode);
        controller.HttpContext.Items[ReportPermissionManagerFilter.SnapshotItemKey] =
            new ReportPermissionSnapshot("F1482", true,
                new Dictionary<string, ReportAction>());
        Assert.IsType<NoContentResult>(await controller.Revoke(request, default));
        Assert.Equal("F1482", repository.LastRevokeActor);
        Assert.NotNull(typeof(ReportPermissionManagementController)
            .GetMethod(nameof(ReportPermissionManagementController.Revoke))!
            .GetCustomAttributes(typeof(ValidateAntiForgeryTokenAttribute), true)
            .SingleOrDefault());
    }

    private static ReportPermissionManagementController CreateController(ListRepository repository)
    {
        var catalog = new ReportCatalogService();
        return new(new ReportPermissionAdministrationService(repository, catalog), catalog,
            NullLogger<ReportPermissionManagementController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
    }

    private sealed class ListRepository : IReportPermissionRepository
    {
        public bool Fail { get; set; }
        public string? LastRevokeActor { get; private set; }

        public Task<PermissionGrantRecordPage> FindActiveGrantPageAsync(
            IReadOnlyList<string> categoryCodes, IReadOnlyList<string> matchingNameCodes,
            string search, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Fail ? Task.FromException<PermissionGrantRecordPage>(new InvalidOperationException())
                : Task.FromResult(new PermissionGrantRecordPage([], 4));

        public Task<PermissionTargetListPage> FindActiveTargetPageAsync(
            IReadOnlyList<string> categoryCodes, IReadOnlyList<string> matchingNameCodes,
            string search, int page, int pageSize, CancellationToken cancellationToken = default) =>
            Fail ? Task.FromException<PermissionTargetListPage>(new InvalidOperationException())
                : Task.FromResult(new PermissionTargetListPage([], page, pageSize, 4));

        public Task<IReadOnlyList<PermissionTargetOption>> SearchTargetsAsync(
            int targetType, string search, CancellationToken cancellationToken = default) =>
            Fail ? Task.FromException<IReadOnlyList<PermissionTargetOption>>(
                new InvalidOperationException())
                : Task.FromResult<IReadOnlyList<PermissionTargetOption>>(
                    [new("0691", "會計室")]);

        public Task<IReadOnlyList<ReportPermissionUser>> FindUsersAsync(
            string loginName, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<ReportPermissionGrant>> FindGrantsAsync(
            string userId, string sector, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<bool> TargetExistsAsync(
            int targetType, string target, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<ReportPermissionGrant>> FindTargetGrantsAsync(
            int targetType, string target, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task SaveGrantsAsync(
            int targetType, string target, IReadOnlyDictionary<string, int> reportMasks,
            string actor, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task SaveGrantsIfUnchangedAsync(
            int targetType, string target, IReadOnlyList<PermissionChangeItem> grants,
            string actor, CancellationToken cancellationToken = default,
            string? sourceUserId = null) =>
            throw new NotImplementedException();

        public Task RevokeGrantIfUnchangedAsync(
            PermissionRevokeRequest request, string actor,
            CancellationToken cancellationToken = default)
        {
            LastRevokeActor = actor;
            return Task.CompletedTask;
        }

        public Task RevokeTargetIfUnchangedAsync(
            PermissionDeleteTargetRequest request, string actor,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
