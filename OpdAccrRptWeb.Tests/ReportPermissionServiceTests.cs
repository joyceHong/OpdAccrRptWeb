using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportPermissionServiceTests
{
    [Fact]
    public async Task AdministratorGrantOverridesIndividualAndDepartment()
    {
        var repository = new FakeRepository(
            [new("A123", "D10")],
            [new("C21", 1, "D10", 7), new("C21", 2, "A123", 7),
                new("C21", 0, "A123", 15)]);
        ReportPermissionSnapshot result = await CreateService(repository).ResolveAsync("A123");

        Assert.True(result.CanManage);
        Assert.True(result.Allows("C21", ReportAction.Rebuild));
    }

    [Fact]
    public async Task IndividualGrantCanRemoveDepartmentRebuild()
    {
        var repository = new FakeRepository(
            [new("E100", "D10")],
            [new("C21", 1, "D10", 15), new("C21", 2, "E100", 7)]);
        ReportPermissionSnapshot result = await CreateService(repository).ResolveAsync("E100");

        Assert.True(result.Allows("C21", ReportAction.Query));
        Assert.False(result.Allows("C21", ReportAction.Rebuild));
        Assert.False(result.CanManage);
    }

    [Fact]
    public async Task AdministratorWithZeroMaskCanManageButCannotQuery()
    {
        var repository = new FakeRepository(
            [new("F1482", "D10")],
            [new("C1", 0, "F1482", 0)]);
        ReportPermissionSnapshot result = await CreateService(repository).ResolveAsync("F1482");

        Assert.True(result.CanManage);
        Assert.False(result.Allows("C1", ReportAction.Query));
    }

    [Fact]
    public async Task ExtraActionsDoNotApplyWithoutQuery()
    {
        var repository = new FakeRepository(
            [new("E100", "D10")],
            [new("C21", 2, "E100", 14)]);
        ReportPermissionSnapshot result = await CreateService(repository).ResolveAsync("E100");

        Assert.False(result.Allows("C21", ReportAction.Excel));
        Assert.False(result.Allows("C21", ReportAction.Print));
        Assert.False(result.Allows("C21", ReportAction.Rebuild));
    }

    [Fact]
    public async Task C141LineBotGrantIsIndependentOfPrint()
    {
        var repository = new FakeRepository(
            [new("E100", "D10")],
            [new("C141", 2, "E100", 17)]);
        ReportPermissionSnapshot result = await CreateService(repository).ResolveAsync("E100");

        Assert.True(result.Allows("C141", ReportAction.LineBot));
        Assert.False(result.Allows("C141", ReportAction.Print));
    }

    [Fact]
    public async Task UnmappedOrDuplicateIdentityIsDenied()
    {
        var missing = new FakeRepository([], [new("C21", 2, "E100", 15)]);
        var duplicate = new FakeRepository(
            [new("E100", "D10"), new("E100", "D11")],
            [new("C21", 2, "E100", 15)]);

        Assert.False((await CreateService(missing).ResolveAsync("DOMAIN\\E100")).CanManage);
        Assert.False((await CreateService(duplicate).ResolveAsync("E100")).Allows("C21", ReportAction.Query));
        Assert.Equal(0, missing.GrantReadCount + duplicate.GrantReadCount);
    }

    [Fact]
    public async Task UnknownReportGrantCannotQualifyAsAdministrator()
    {
        var repository = new FakeRepository(
            [new("F1482", "D10")],
            [new("UNKNOWN", 0, "F1482", 15)]);
        ReportPermissionSnapshot result = await CreateService(repository).ResolveAsync("F1482");

        Assert.False(result.CanManage);
        Assert.False(result.Allows("UNKNOWN", ReportAction.Query));
    }

    [Fact]
    public async Task DepartmentGrantMatchesAfterRemovingTrailingCharSpaces()
    {
        var repository = new FakeRepository(
            [new("E100    ", "D10    ")],
            [new("C21", 1, "D10    ", 7)]);
        ReportPermissionSnapshot result = await CreateService(repository).ResolveAsync("E100");

        Assert.Equal("E100", result.UserId);
        Assert.True(result.Allows("C21", ReportAction.Excel));
        Assert.False(result.CanManage);
    }

    [Fact]
    public async Task InvalidMaskIsDenied()
    {
        var repository = new FakeRepository(
            [new("F1482", "D10")],
            [new("C1", 0, "F1482", 16)]);
        ReportPermissionSnapshot result = await CreateService(repository).ResolveAsync("F1482");

        Assert.False(result.CanManage);
        Assert.False(result.Allows("C1", ReportAction.Query));
    }

    private static ReportPermissionService CreateService(FakeRepository repository) =>
        new(repository, new ReportCatalogService(), NullLogger<ReportPermissionService>.Instance);

    private sealed class FakeRepository(
        IReadOnlyList<ReportPermissionUser> users,
        IReadOnlyList<ReportPermissionGrant> grants) : IReportPermissionRepository
    {
        public Task<PermissionGrantRecordPage> FindActiveGrantPageAsync(
            IReadOnlyList<string> categoryCodes, IReadOnlyList<string> matchingNameCodes,
            string search, int page, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<PermissionTargetListPage> FindActiveTargetPageAsync(
            IReadOnlyList<string> categoryCodes, IReadOnlyList<string> matchingNameCodes,
            string search, int page, int pageSize, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<PermissionTargetOption>> SearchTargetsAsync(
            int targetType, string search, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public int GrantReadCount { get; private set; }

        public Task<IReadOnlyList<ReportPermissionUser>> FindUsersAsync(
            string loginName, CancellationToken cancellationToken = default) =>
            Task.FromResult(users);

        public Task<IReadOnlyList<ReportPermissionGrant>> FindGrantsAsync(
            string userId, string sector, CancellationToken cancellationToken = default)
        {
            GrantReadCount++;
            return Task.FromResult(grants);
        }

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
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task RevokeTargetIfUnchangedAsync(
            PermissionDeleteTargetRequest request, string actor,
            CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
