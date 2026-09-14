using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C23RebuildServiceTests
{
    [Fact]
    public void EnsureData_MissingIntermediateDataRunsAutomaticRebuild()
    {
        var repository = new FakeRepository();
        var service = Create(repository, enabled: false);
        Assert.True(service.EnsureData(Condition()));
        Assert.Equal("1150908", repository.RebuiltDate);
    }

    [Fact]
    public void EnsureData_DisabledForcedRequestPerformsNoDml()
    {
        var repository = new FakeRepository { HasData = true };
        var condition = Condition();
        condition.ForceRebuild = true;
        Assert.Throws<C23RebuildForbiddenException>(() => Create(repository, enabled: false).EnsureData(condition));
        Assert.Null(repository.RebuiltDate);
    }

    [Theory]
    [InlineData("EncounterDate", "2026-09-08")]
    [InlineData("General", "2026-09-09")]
    public void EnsureData_IneligibleModeOrRangeDoesNotRebuild(string mode, string endDate)
    {
        var repository = new FakeRepository();
        var condition = Condition();
        condition.DateMode = mode;
        condition.EndDate = endDate;
        Assert.False(Create(repository, enabled: true).EnsureData(condition));
        Assert.Null(repository.RebuiltDate);
    }

    private static C23RebuildService Create(FakeRepository repository, bool enabled)
    {
        var identity = new ConfiguredC23UserIdentityProvider(Options.Create(new C23Options { CurrentUserId = "C23-USER" }));
        var auth = new C23RebuildAuthorizationService(identity, Options.Create(new C23Options
        {
            CurrentUserId = "C23-USER", RebuildEnabled = enabled
        }));
        return new(repository, auth, identity, NullLogger<C23RebuildService>.Instance);
    }

    private static SearchReportCondition Condition() => new()
    {
        StartDate = "2026-09-08", EndDate = "2026-09-08", EncounterSource = C23EncounterSources.Outpatient,
        DateMode = C23DateModes.General
    };

    private sealed class FakeRepository : IC23ContractAccountingRepository
    {
        public bool HasData { get; init; }
        public string? RebuiltDate { get; private set; }
        public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() => [];
        public IReadOnlyList<C23ContractOption> GetContracts() => [];
        public int GetCount(SearchReportCondition condition) => 0;
        public List<C23ContractAccountingReportViewModel> GetPage(SearchReportCondition condition) => [];
        public bool HasIntermediateData(string encounterSource, string rocDate) => HasData;
        public void RebuildSingleDay(string encounterSource, string rocDate, string? contractCode) => RebuiltDate = rocDate;
    }
}
