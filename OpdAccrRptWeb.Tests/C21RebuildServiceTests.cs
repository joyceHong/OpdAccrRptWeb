using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C21RebuildServiceTests
{
    [Fact]
    public void EnsureData_ZeroRoom23RowsRunsRebuildWithRocDate()
    {
        var repository = new FakeRepository { HasData = false };
        var service = Create(repository, "C21-DEMO", rebuildEnabled: false);

        Assert.True(service.EnsureData(Condition()));
        Assert.Equal("1150908", repository.RebuiltDate);
    }

    [Fact]
    public void EnsureData_ExistingRoom23RowReusesDataUnlessForced()
    {
        var repository = new FakeRepository { HasData = true };
        var service = Create(repository, "C21-DEMO");

        Assert.False(service.EnsureData(Condition()));
        var forced = Condition();
        forced.ForceRebuild = true;
        Assert.True(service.EnsureData(forced));
    }

    [Fact]
    public void EnsureData_MissingConfiguredIdentityDeniesRequiredRebuild()
    {
        var repository = new FakeRepository { HasData = false };
        var service = Create(repository, null);

        Assert.Throws<C21RebuildForbiddenException>(() => service.EnsureData(Condition()));
        Assert.Null(repository.RebuiltDate);
    }

    [Fact]
    public void EnsureData_DisabledManualRebuildDeniesForcedRequestWithoutDml()
    {
        var repository = new FakeRepository { HasData = true };
        var service = Create(repository, "C21-DEMO", rebuildEnabled: false);
        var condition = Condition();
        condition.ForceRebuild = true;

        Assert.Throws<C21RebuildForbiddenException>(() => service.EnsureData(condition));
        Assert.Null(repository.RebuiltDate);
    }

    [Fact]
    public void EnsureData_MissingIdentityDeniesForcedRequestWithoutDml()
    {
        var repository = new FakeRepository { HasData = true };
        var service = Create(repository, null, rebuildEnabled: true);
        var condition = Condition();
        condition.ForceRebuild = true;

        Assert.Throws<C21RebuildForbiddenException>(() => service.EnsureData(condition));
        Assert.Null(repository.RebuiltDate);
    }

    private static C21RebuildService Create(
        FakeRepository repository,
        string? userId,
        bool rebuildEnabled = true)
    {
        var identity = new ConfiguredC21UserIdentityProvider(
            Options.Create(new C21Options { CurrentUserId = userId }));
        return new(repository, new C21RebuildAuthorizationService(identity,
                Options.Create(new C21Options
                {
                    CurrentUserId = userId,
                    RebuildEnabled = rebuildEnabled
                })), identity,
            NullLogger<C21RebuildService>.Instance);
    }

    private static SearchReportCondition Condition() => new()
    {
        StartDate = "2026-09-08", EndDate = "2026-09-08",
        EncounterSource = C21EncounterSources.Inpatient, AccountingScope = 4
    };

    private sealed class FakeRepository : IC21AccountingSummaryRepository
    {
        public bool HasData { get; init; }
        public string? RebuiltDate { get; private set; }
        public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() => [];
        public IReadOnlyList<C21BillingItem> GetBillingItems() => [];
        public IReadOnlyList<C21SourceAmount> GetSourceAmounts(SearchReportCondition condition) => [];
        public bool HasInpatientRoom23Data(string rocDate) => HasData;
        public void RebuildSingleInpatientDay(string rocDate) => RebuiltDate = rocDate;
    }
}
