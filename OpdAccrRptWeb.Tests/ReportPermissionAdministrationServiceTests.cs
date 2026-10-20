using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportPermissionAdministrationServiceTests
{
    [Fact]
    public async Task NewGrantDefaultsToQueryExcelAndPrint()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        await service.SaveGrantAsync(new PermissionGrantEditRequest
        {
            TargetType = 2, Target = "E100", ReportCode = "C21"
        }, "F1482");

        Assert.Equal(7, repository.LastMasks!["C21"]);
        Assert.Equal("F1482", repository.LastActor);
    }

    [Fact]
    public async Task ExistingGrantCanClearExcelIndependently()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        await service.SaveGrantAsync(new PermissionGrantEditRequest
        {
            TargetType = 2, Target = "E100", ReportCode = "C21",
            Query = true, Excel = false, Print = true, Rebuild = false
        }, "F1482");

        Assert.Equal(5, repository.LastMasks!["C21"]);
    }

    [Fact]
    public async Task ExplicitRebuildCanBeSaved()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        await service.SaveGrantAsync(new PermissionGrantEditRequest
        {
            TargetType = 0, Target = "F1482", ReportCode = "C21",
            Rebuild = true
        }, "F1482");

        Assert.Equal(15, repository.LastMasks!["C21"]);
    }

    [Fact]
    public async Task UnknownReportOrTargetDoesNotWrite()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveGrantAsync(new PermissionGrantEditRequest
            {
                TargetType = 2, Target = "E100", ReportCode = "UNKNOWN"
            }, "F1482"));
        repository.TargetExists = false;
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.SaveGrantAsync(new PermissionGrantEditRequest
            {
                TargetType = 2, Target = "E100", ReportCode = "C21"
            }, "F1482"));

        Assert.Null(repository.LastMasks);
    }

    [Fact]
    public async Task BatchPreviewAndApplyAffectOnlySelectedReports()
    {
        var repository = new FakeRepository
        {
            Existing =
            [
                new("C21", 0, "A123", 15),
                new("C24", 0, "A123", 7)
            ]
        };
        var service = CreateService(repository);
        var request = new PermissionBatchRequest
        {
            TargetType = 0,
            Target = "A123",
            ReportCodes = ["C21", "C22", "C23"]
        };

        IReadOnlyList<PermissionBatchPreviewRow> preview =
            await service.PreviewBatchAsync(request);
        await service.ApplyBatchAsync(request, "F1482");

        Assert.Equal(["更新", "新增", "新增"], preview.Select(row => row.Operation));
        Assert.Equal(1, repository.SaveCount);
        Assert.Equal(3, repository.LastMasks!.Count);
        Assert.DoesNotContain("C24", repository.LastMasks.Keys);
        Assert.All(repository.LastMasks.Values, mask => Assert.Equal(7, mask));
    }

    [Fact]
    public async Task InvalidBatchRejectsAllWrites()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);
        var request = new PermissionBatchRequest
        {
            TargetType = 0,
            Target = "A123",
            ReportCodes = ["C21", "UNKNOWN"]
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ApplyBatchAsync(request, "F1482"));
        Assert.Equal(0, repository.SaveCount);
    }

    [Fact]
    public async Task CopySelectedAdministratorReportsPreservesOtherTargetReports()
    {
        var repository = new FakeRepository
        {
            TargetGrants = new Dictionary<string, IReadOnlyList<ReportPermissionGrant>>
            {
                ["A123"] = [new("C21", 0, "A123", 15), new("C22", 0, "A123", 7)],
                ["B456"] = [new("C21", 0, "B456", 7), new("C23", 0, "B456", 7)]
            }
        };
        var service = CreateService(repository);
        var request = new PermissionCopyRequest
        {
            SourceUserId = "A123",
            TargetUserId = "B456",
            ReportCodes = ["C21", "C22"]
        };

        IReadOnlyList<PermissionCopyPreviewRow> preview =
            await service.PreviewCopyAsync(request);
        await service.ApplyCopyAsync(request, "F1482");

        Assert.Equal(["覆寫", "新增"], preview.Select(row => row.Operation));
        Assert.Equal(15, repository.LastMasks!["C21"]);
        Assert.Equal(7, repository.LastMasks["C22"]);
        Assert.DoesNotContain("C23", repository.LastMasks.Keys);
        Assert.Equal("B456", repository.LastTarget);
        Assert.Equal("F1482", repository.LastActor);
    }

    [Fact]
    public async Task CopyRejectsSameAccountAndAbsentSourceReport()
    {
        var repository = new FakeRepository
        {
            TargetGrants = new Dictionary<string, IReadOnlyList<ReportPermissionGrant>>
            {
                ["A123"] = [new("C21", 0, "A123", 15)],
                ["B456"] = []
            }
        };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ApplyCopyAsync(new PermissionCopyRequest
            {
                SourceUserId = "A123", TargetUserId = "A123", ReportCodes = ["C21"]
            }, "F1482"));
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ApplyCopyAsync(new PermissionCopyRequest
            {
                SourceUserId = "A123", TargetUserId = "B456", ReportCodes = ["C22"]
            }, "F1482"));
        Assert.Equal(0, repository.SaveCount);
    }

    private static ReportPermissionAdministrationService CreateService(FakeRepository repository) =>
        new(repository, new ReportCatalogService());

    private sealed class FakeRepository : IReportPermissionRepository
    {
        public bool TargetExists { get; set; } = true;
        public IReadOnlyDictionary<string, int>? LastMasks { get; private set; }
        public string? LastActor { get; private set; }
        public string? LastTarget { get; private set; }
        public int SaveCount { get; private set; }
        public IReadOnlyList<ReportPermissionGrant> Existing { get; set; } = [];
        public IReadOnlyDictionary<string, IReadOnlyList<ReportPermissionGrant>>? TargetGrants { get; set; }

        public Task<bool> TargetExistsAsync(
            int targetType, string target, CancellationToken cancellationToken = default) =>
            Task.FromResult(TargetExists);

        public Task SaveGrantsAsync(
            int targetType, string target, IReadOnlyDictionary<string, int> reportMasks,
            string actor, CancellationToken cancellationToken = default)
        {
            LastMasks = reportMasks;
            LastActor = actor;
            LastTarget = target;
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<ReportPermissionGrant>> FindTargetGrantsAsync(
            int targetType, string target, CancellationToken cancellationToken = default) =>
            Task.FromResult(TargetGrants is not null && TargetGrants.TryGetValue(target, out var grants)
                ? grants : Existing);

        public Task<IReadOnlyList<ReportPermissionUser>> FindUsersAsync(
            string loginName, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<IReadOnlyList<ReportPermissionGrant>> FindGrantsAsync(
            string userId, string sector, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
