using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C13ReportServiceIntegrationTests
{
    [Fact]
    public void SameDates_CachesCountButQueriesEveryPage()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeC13Repository { TotalCount = 28 };
        ReportService service = Create(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<C13HighRiskEmergencyReportViewModel>(Condition(1, 10));
        var result = service.ReportDataAndColumns<C13HighRiskEmergencyReportViewModel>(Condition(2, 30));

        Assert.Equal(1, repository.CountCalls);
        Assert.Equal(2, repository.PageCalls);
        Assert.Equal(28, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(30, result.PageSize);
    }

    [Fact]
    public void ChangedDate_UsesDifferentCountCacheEntry()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeC13Repository { TotalCount = 1 };
        ReportService service = Create(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<C13HighRiskEmergencyReportViewModel>(Condition(1, 10));
        SearchReportCondition changed = Condition(1, 10);
        changed.EndDate = "2026-09-16";
        service.ReportDataAndColumns<C13HighRiskEmergencyReportViewModel>(changed);

        Assert.Equal(2, repository.CountCalls);
    }

    [Fact]
    public void CountFailure_IsNotCachedAndPageIsNotQueried()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeC13Repository { CountException = new InvalidOperationException("patient sentinel") };
        ReportService service = Create(repository, new ReportTotalCountCache(memoryCache));

        Assert.Throws<InvalidOperationException>(() =>
            service.ReportDataAndColumns<C13HighRiskEmergencyReportViewModel>(Condition(1, 10)));
        Assert.Throws<InvalidOperationException>(() =>
            service.ReportDataAndColumns<C13HighRiskEmergencyReportViewModel>(Condition(1, 10)));

        Assert.Equal(2, repository.CountCalls);
        Assert.Equal(0, repository.PageCalls);
    }

    [Fact]
    public void EmptyResult_ReturnsZeroPages()
    {
        var repository = new FakeC13Repository();
        var result = Create(repository, new PassthroughReportTotalCountCache())
            .ReportDataAndColumns<C13HighRiskEmergencyReportViewModel>(Condition(1, 10));

        Assert.Empty(result.Data!);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    private static ReportService Create(FakeC13Repository repository, IReportTotalCountCache cache) => new(
        new FakeHealthCenterRepository(), new FakeReferralMemberRepository(), new FakeSafeNeedleRepository(),
        cache, NullLogger<ReportService>.Instance, c13Repository: repository);

    private static SearchReportCondition Condition(int pageNumber, int pageSize) => new()
    {
        ReportCode = "C13",
        StartDate = "2026-09-14",
        EndDate = "2026-09-15",
        PageNumber = pageNumber,
        PageSize = pageSize
    };

    private sealed class FakeC13Repository : IC13HighRiskEmergencyRepository
    {
        public int TotalCount { get; init; }
        public Exception? CountException { get; init; }
        public int CountCalls { get; private set; }
        public int PageCalls { get; private set; }
        public int GetCount(SearchReportCondition condition, CancellationToken cancellationToken = default)
        {
            CountCalls++;
            if (CountException is not null) throw CountException;
            return TotalCount;
        }
        public List<C13HighRiskEmergencyReportViewModel> GetPage(SearchReportCondition condition, CancellationToken cancellationToken = default)
        {
            PageCalls++;
            return [];
        }
        public List<C13HighRiskEmergencyReportViewModel> GetAllForPreview(SearchReportCondition condition, CancellationToken cancellationToken = default) => [];
        public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
            ModelDescriptionsHelper.GetPropertyDescriptions<C13HighRiskEmergencyReportViewModel>();
    }
}
