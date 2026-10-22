using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportPermissionAdministrationServiceTests
{
    [Fact]
    public async Task TargetListUsesCatalogFilterAndReturnsOnePagedTarget()
    {
        var repository = new FakeRepository
        {
            TargetPage = new(
                [new(2, "E100", "王小明", 3, "F1482", new DateTime(2026, 10, 20))],
                2, 20, 27)
        };

        PermissionTargetListPage page = await CreateService(repository)
            .GetTargetListAsync("批價會計", "outpatient", 2, 20);

        Assert.Equal(27, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(3, Assert.Single(page.Items).ReportCount);
        Assert.Contains("C21", repository.LastNameCodes!);
        Assert.DoesNotContain("M1", repository.LastCategoryCodes!);
        Assert.Equal("批價會計", repository.LastSearch);
    }

    [Fact]
    public async Task ChangePreviewShowsReportRevocationAndRequiresExpectedMaskOnApply()
    {
        var repository = new FakeRepository
        {
            Existing = [new("C21", 2, "E100", 7)]
        };
        var request = new PermissionChangeRequest
        {
            TargetType = 2, Target = "E100",
            Grants = [new() { ReportCode = "C21", Revoke = true }]
        };
        var service = CreateService(repository);

        PermissionChangePreviewRow preview = Assert.Single(await service.PreviewChangesAsync(request));
        Assert.Equal("撤銷", preview.Operation);
        Assert.Equal(7, preview.RemovedMask);
        await Assert.ThrowsAsync<ArgumentException>(() => service.ApplyChangesAsync(request, "F1482"));
        Assert.Null(repository.LastChanges);

        request.Grants[0].ExpectedMask = preview.OldMask;
        await service.ApplyChangesAsync(request, "F1482");
        Assert.True(Assert.Single(repository.LastChanges!).Revoke);
    }

    [Fact]
    public async Task ExistingInactiveTargetCanBeReadAndRevokedButNotGivenNewGrants()
    {
        var repository = new FakeRepository
        {
            TargetExists = false,
            Existing = [new("C21", 2, "E100", 7)]
        };
        var service = CreateService(repository);

        Assert.Single(await service.GetGrantsAsync(2, "E100"));
        var revoke = new PermissionChangeRequest
        {
            TargetType = 2, Target = "E100",
            Grants = [new() { ReportCode = "C21", Revoke = true, ExpectedMask = 7 }]
        };
        Assert.Equal("撤銷", Assert.Single(await service.PreviewChangesAsync(revoke)).Operation);
        await service.ApplyChangesAsync(revoke, "F1482");
        Assert.True(Assert.Single(repository.LastChanges!).Revoke);

        revoke.Grants.Add(new() { ReportCode = "M1", ActionMask = 7 });
        await Assert.ThrowsAsync<ArgumentException>(() => service.PreviewChangesAsync(revoke));
    }

    [Fact]
    public async Task GrantListFiltersCatalogBeforePagingAndKeepsTotalCount()
    {
        var repository = new FakeRepository
        {
            ListPage = new(
                [new("C21", 2, "E100", 7, "王小明", "F1482", new DateTime(2026, 10, 20))], 27)
        };
        var service = CreateService(repository);

        PermissionGrantListPage page = await service.GetGrantListAsync("批價會計", "outpatient", 2, 20);

        Assert.Equal(27, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Single(page.Items);
        Assert.Equal("批價會計總表", page.Items[0].ReportName);
        Assert.Equal("門診批價統計報表", page.Items[0].CategoryName);
        Assert.Contains("C21", repository.LastNameCodes!);
        Assert.DoesNotContain("M1", repository.LastCategoryCodes!);
        Assert.Equal("批價會計", repository.LastSearch);
    }

    [Fact]
    public async Task TargetAutocompleteRequiresSearchTermAndReturnsRepositoryCandidates()
    {
        var repository = new FakeRepository
        {
            TargetOptions = [new("0691", "會計室")]
        };
        var service = CreateService(repository);

        IReadOnlyList<PermissionTargetOption> result =
            await service.SearchTargetsAsync(1, " 0691 ");

        Assert.Equal("0691", Assert.Single(result).Code);
        Assert.Equal("0691", repository.LastTargetSearch);
        await Assert.ThrowsAsync<ArgumentException>(() => service.SearchTargetsAsync(1, "0"));
        await Assert.ThrowsAsync<ArgumentException>(() => service.SearchTargetsAsync(3, "0691"));
    }

    [Fact]
    public async Task ChangePreviewShowsRemovedExcelAndApplyChecksExpectedMask()
    {
        var repository = new FakeRepository
        {
            Existing = [new("C21", 2, "E100", 7)]
        };
        var service = CreateService(repository);
        var request = new PermissionChangeRequest
        {
            TargetType = 2, Target = "E100",
            Grants = [new() { ReportCode = "C21", ActionMask = 5, ExpectedMask = 7 }]
        };

        PermissionChangePreviewRow preview = Assert.Single(await service.PreviewChangesAsync(request));
        Assert.Equal("變更", preview.Operation);
        Assert.Equal(7, preview.OldMask);
        Assert.Equal(5, preview.NewMask);
        Assert.Equal(0, preview.AddedMask);
        Assert.Equal(2, preview.RemovedMask);

        await service.ApplyChangesAsync(request, "F1482");
        Assert.Equal(5, Assert.Single(repository.LastChanges!).ActionMask);
        Assert.Equal(7, Assert.Single(repository.LastChanges!).ExpectedMask);
    }

    [Fact]
    public async Task ChangeApplyRejectsStalePreviewWithoutWriting()
    {
        var repository = new FakeRepository { ChangeConflict = true };
        var service = CreateService(repository);
        var request = new PermissionChangeRequest
        {
            TargetType = 2, Target = "E100",
            Grants = [new() { ReportCode = "C21", ActionMask = 5, ExpectedMask = 7 }]
        };

        await Assert.ThrowsAsync<PermissionConflictException>(() =>
            service.ApplyChangesAsync(request, "F1482"));
        Assert.Null(repository.LastChanges);
    }

    [Fact]
    public async Task RevocationPreservesTargetCodeAndActorAndRejectsInvalidMask()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);
        var request = new PermissionRevokeRequest
        {
            ReportCode = "C21", TargetType = 2, Target = "E100", ExpectedMask = 7
        };

        await service.RevokeGrantAsync(request, "F1482");
        Assert.Equal("E100", repository.LastRevoke!.Target);
        Assert.Equal("F1482", repository.LastRevokeActor);

        request.ExpectedMask = 16;
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.RevokeGrantAsync(request, "F1482"));
    }

    [Fact]
    public async Task DeleteTargetRequiresSnapshotAndForwardsAllExpectedMasks()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);
        var request = new PermissionDeleteTargetRequest
        {
            TargetType = 2, Target = "E100 ",
            ExpectedMasks = new() { ["C21"] = 7, ["M1"] = 1 }
        };

        await service.DeleteTargetAsync(request, "F1482");

        Assert.Equal("E100", repository.LastDeleteTarget!.Target);
        Assert.Equal(2, repository.LastDeleteTarget.ExpectedMasks.Count);
        Assert.Equal("F1482", repository.LastActor);

        request.ExpectedMasks.Clear();
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.DeleteTargetAsync(request, "F1482"));
    }

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
    public async Task LineBotGrantAppliesOnlyToC141()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);
        await service.SaveGrantAsync(new PermissionGrantEditRequest
        {
            TargetType = 2, Target = "E100", ReportCode = "C141", LineBot = true
        }, "F1482");
        Assert.Equal(23, repository.LastMasks!["C141"]);

        await service.SaveGrantAsync(new PermissionGrantEditRequest
        {
            TargetType = 2, Target = "E100", ReportCode = "C21", LineBot = true
        }, "F1482");
        Assert.Equal(7, repository.LastMasks!["C21"]);
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
            ReportCodes = ["C21", "C22"],
            PreviewSourceMasks = new() { ["C21"] = 15, ["C22"] = 7 },
            ExpectedTargetMasks = new() { ["C21"] = 7, ["C22"] = null }
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
        Assert.Equal(8, Assert.Single(preview, row => row.ReportCode == "C21").AddedMask);
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

    [Fact]
    public async Task CopyRejectsSourceChangedAfterPreview()
    {
        var repository = new FakeRepository
        {
            TargetGrants = new Dictionary<string, IReadOnlyList<ReportPermissionGrant>>
            {
                ["A123"] = [new("C21", 0, "A123", 15)],
                ["B456"] = []
            }
        };
        var request = new PermissionCopyRequest
        {
            SourceUserId = "A123", TargetUserId = "B456", ReportCodes = ["C21"],
            PreviewSourceMasks = new() { ["C21"] = 7 },
            ExpectedTargetMasks = new() { ["C21"] = null }
        };

        await Assert.ThrowsAsync<PermissionConflictException>(() =>
            CreateService(repository).ApplyCopyAsync(request, "F1482"));
        Assert.Null(repository.LastChanges);
    }

    private static ReportPermissionAdministrationService CreateService(FakeRepository repository) =>
        new(repository, new ReportCatalogService());

    private sealed class FakeRepository : IReportPermissionRepository
    {
        public PermissionGrantRecordPage ListPage { get; set; } = new([], 0);
        public PermissionTargetListPage TargetPage { get; set; } = new([], 1, 20, 0);
        public IReadOnlyList<string>? LastCategoryCodes { get; private set; }
        public IReadOnlyList<string>? LastNameCodes { get; private set; }
        public string? LastSearch { get; private set; }
        public string? LastTargetSearch { get; private set; }
        public IReadOnlyList<PermissionTargetOption> TargetOptions { get; set; } = [];
        public bool ChangeConflict { get; set; }
        public IReadOnlyList<PermissionChangeItem>? LastChanges { get; private set; }
        public PermissionRevokeRequest? LastRevoke { get; private set; }
        public PermissionDeleteTargetRequest? LastDeleteTarget { get; private set; }
        public string? LastRevokeActor { get; private set; }
        public bool TargetExists { get; set; } = true;
        public IReadOnlyDictionary<string, int>? LastMasks { get; private set; }
        public string? LastActor { get; private set; }
        public string? LastTarget { get; private set; }
        public int SaveCount { get; private set; }
        public IReadOnlyList<ReportPermissionGrant> Existing { get; set; } = [];
        public IReadOnlyDictionary<string, IReadOnlyList<ReportPermissionGrant>>? TargetGrants { get; set; }

        public Task<PermissionGrantRecordPage> FindActiveGrantPageAsync(
            IReadOnlyList<string> categoryCodes, IReadOnlyList<string> matchingNameCodes,
            string search, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            LastCategoryCodes = categoryCodes;
            LastNameCodes = matchingNameCodes;
            LastSearch = search;
            return Task.FromResult(ListPage);
        }

        public Task<PermissionTargetListPage> FindActiveTargetPageAsync(
            IReadOnlyList<string> categoryCodes, IReadOnlyList<string> matchingNameCodes,
            string search, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            LastCategoryCodes = categoryCodes;
            LastNameCodes = matchingNameCodes;
            LastSearch = search;
            return Task.FromResult(TargetPage);
        }

        public Task<IReadOnlyList<PermissionTargetOption>> SearchTargetsAsync(
            int targetType, string search, CancellationToken cancellationToken = default)
        {
            LastTargetSearch = search;
            return Task.FromResult(TargetOptions);
        }

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

        public Task SaveGrantsIfUnchangedAsync(
            int targetType, string target, IReadOnlyList<PermissionChangeItem> grants,
            string actor, CancellationToken cancellationToken = default,
            string? sourceUserId = null)
        {
            if (ChangeConflict) throw new PermissionConflictException();
            LastChanges = grants;
            LastMasks = grants.ToDictionary(grant => grant.ReportCode,
                grant => grant.ActionMask, StringComparer.Ordinal);
            LastTarget = target;
            LastActor = actor;
            return Task.CompletedTask;
        }

        public Task RevokeGrantIfUnchangedAsync(
            PermissionRevokeRequest request, string actor,
            CancellationToken cancellationToken = default)
        {
            LastRevoke = request;
            LastRevokeActor = actor;
            return Task.CompletedTask;
        }

        public Task RevokeTargetIfUnchangedAsync(
            PermissionDeleteTargetRequest request, string actor,
            CancellationToken cancellationToken = default)
        {
            LastDeleteTarget = request;
            LastActor = actor;
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
