using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Help;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportServiceTests
{
    [Fact]
    public void ReportDataAndColumns_C1_ReturnsEighteenColumnsAndPagedMetadata()
    {
        var repository = new FakeSurgicalAccountingRepository
        {
            TotalCount = 28,
            Data = [new SurgicalAccountingReportViewModel { SurgicalOrderCode = "64202B(LEFT)(麻醉)" }]
        };
        var service = CreateC1Service(repository, new PassthroughReportTotalCountCache());

        var result = service.ReportDataAndColumns<SurgicalAccountingReportViewModel>(CreateC1Condition(2, 10));

        Assert.Equal(18, result.Columns!.Count);
        Assert.Single(result.Data!);
        Assert.Equal(28, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal("1150801", repository.LastStartDate);
        Assert.Equal("1150803", repository.LastEndDate);
    }

    [Fact]
    public void ReportDataAndColumns_C1SameDates_CachesCountButAlwaysQueriesPage()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeSurgicalAccountingRepository { TotalCount = 28 };
        var service = CreateC1Service(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<SurgicalAccountingReportViewModel>(CreateC1Condition(1, 10));
        var second = service.ReportDataAndColumns<SurgicalAccountingReportViewModel>(CreateC1Condition(2, 30));

        Assert.Equal(1, repository.CountCalls);
        Assert.Equal(2, repository.PageCalls);
        Assert.Equal(1, second.TotalPages);
        Assert.Equal(2, second.PageNumber);
        Assert.Equal(30, second.PageSize);
    }

    [Fact]
    public void ReportDataAndColumns_C1ChangedDates_UsesDifferentCountEntry()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeSurgicalAccountingRepository();
        var service = CreateC1Service(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<SurgicalAccountingReportViewModel>(CreateC1Condition(1, 10));
        var changed = CreateC1Condition(1, 10);
        changed.EndDate = "2026-08-04";
        service.ReportDataAndColumns<SurgicalAccountingReportViewModel>(changed);

        Assert.Equal(2, repository.CountCalls);
        Assert.Equal(2, repository.PageCalls);
    }

    [Fact]
    public void ReportDataAndColumns_C1CountFailure_IsNotCached()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeSurgicalAccountingRepository
        {
            CountException = new InvalidOperationException("count failed")
        };
        var service = CreateC1Service(repository, new ReportTotalCountCache(memoryCache));

        Assert.Throws<InvalidOperationException>(() =>
            service.ReportDataAndColumns<SurgicalAccountingReportViewModel>(CreateC1Condition(1, 10)));
        Assert.Throws<InvalidOperationException>(() =>
            service.ReportDataAndColumns<SurgicalAccountingReportViewModel>(CreateC1Condition(1, 10)));

        Assert.Equal(2, repository.CountCalls);
        Assert.Equal(0, repository.PageCalls);
    }

    private static ReportService CreateC1Service(
        FakeSurgicalAccountingRepository repository,
        IReportTotalCountCache cache) => new(
            new FakeHealthCenterRepository(),
            new FakeReferralMemberRepository(),
            new FakeSafeNeedleRepository(),
            cache,
            NullLogger<ReportService>.Instance,
            repository);

    private static SearchReportCondition CreateC1Condition(int pageNumber, int pageSize) => new()
    {
        ReportCode = "C1",
        StartDate = "2026-08-01",
        EndDate = "2026-08-03",
        PageNumber = pageNumber,
        PageSize = pageSize
    };

    [Fact]
    public void ReportDataAndColumns_C19_ReturnsSixColumnsAndKeepsXAndYAsSeparateRows()
    {
        var safeNeedleRepository = new FakeSafeNeedleRepository
        {
            TotalCount = 2,
            Data =
            [
                new SafeNeedleReportViewModel { Category = "X", OrderCode = "SICPU24" },
                new SafeNeedleReportViewModel { Category = "Y", OrderCode = "SDS3" }
            ]
        };
        var service = new ReportService(
            new FakeHealthCenterRepository(),
            new FakeReferralMemberRepository(),
            safeNeedleRepository,
            new PassthroughReportTotalCountCache(),
            NullLogger<ReportService>.Instance);

        var result = service.ReportDataAndColumns<SafeNeedleReportViewModel>(new SearchReportCondition
        {
            ReportCode = "C19",
            StartDate = "2026-08-24",
            EndDate = "2026-08-24",
            EncounterSource = EncounterSources.Inpatient,
            StationOrBedPrefix = "7A",
            PageNumber = 1,
            PageSize = 10
        });

        Assert.Equal(6, result.Columns!.Count);
        Assert.Collection(
            result.Data!,
            row => Assert.Equal(("X", "SICPU24"), (row.Category, row.OrderCode)),
            row => Assert.Equal(("Y", "SDS3"), (row.Category, row.OrderCode)));
        Assert.Equal("1150824", safeNeedleRepository.LastStartDate);
        Assert.Equal("1150824", safeNeedleRepository.LastEndDate);
        Assert.Equal(EncounterSources.Inpatient, safeNeedleRepository.LastSource);
        Assert.Equal("7A", safeNeedleRepository.LastPrefix);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public void ReportDataAndColumns_C19EmptyResult_ReturnsEmptyUnpagedData()
    {
        var safeNeedleRepository = new FakeSafeNeedleRepository();
        var service = new ReportService(
            new FakeHealthCenterRepository(),
            new FakeReferralMemberRepository(),
            safeNeedleRepository,
            new PassthroughReportTotalCountCache(),
            NullLogger<ReportService>.Instance);

        var result = service.ReportDataAndColumns<SafeNeedleReportViewModel>(new SearchReportCondition
        {
            ReportCode = "C19",
            StartDate = "2026-08-24",
            EndDate = "2026-08-24",
            EncounterSource = EncounterSources.Emergency,
            PageNumber = 1,
            PageSize = 10
        });

        Assert.Empty(result.Data!);
        Assert.Equal(1, safeNeedleRepository.CountCalls);
        Assert.Equal(1, safeNeedleRepository.PageCalls);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void ReportDataAndColumns_C19SameFilters_CachesCountButAlwaysQueriesPage()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeSafeNeedleRepository { TotalCount = 28 };
        var service = new ReportService(
            new FakeHealthCenterRepository(),
            new FakeReferralMemberRepository(),
            repository,
            new ReportTotalCountCache(memoryCache),
            NullLogger<ReportService>.Instance);

        service.ReportDataAndColumns<SafeNeedleReportViewModel>(CreateC19Condition(
            EncounterSources.Emergency, " 7A ", 1, 10));
        var secondPage = service.ReportDataAndColumns<SafeNeedleReportViewModel>(CreateC19Condition(
            EncounterSources.Emergency, "7A", 2, 30));

        Assert.Equal(1, repository.CountCalls);
        Assert.Equal(2, repository.PageCalls);
        Assert.Equal(28, secondPage.TotalCount);
        Assert.Equal(1, secondPage.TotalPages);
        Assert.Equal(2, secondPage.PageNumber);
        Assert.Equal(30, secondPage.PageSize);
    }

    [Fact]
    public void ReportDataAndColumns_C19CacheSeparatesSourceAndPrefix()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeSafeNeedleRepository { TotalCount = 28 };
        var service = new ReportService(
            new FakeHealthCenterRepository(),
            new FakeReferralMemberRepository(),
            repository,
            new ReportTotalCountCache(memoryCache),
            NullLogger<ReportService>.Instance);

        service.ReportDataAndColumns<SafeNeedleReportViewModel>(CreateC19Condition(
            EncounterSources.Emergency, "7A", 1, 10));
        service.ReportDataAndColumns<SafeNeedleReportViewModel>(CreateC19Condition(
            EncounterSources.Inpatient, "7A", 1, 10));
        service.ReportDataAndColumns<SafeNeedleReportViewModel>(CreateC19Condition(
            EncounterSources.Inpatient, "8B", 1, 10));

        Assert.Equal(3, repository.CountCalls);
        Assert.Equal(3, repository.PageCalls);
    }

    [Fact]
    public void ReportDataAndColumns_C19CountFailure_IsRetriedAndNotCached()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var exception = new InvalidOperationException("count failed");
        var repository = new FakeSafeNeedleRepository { CountException = exception };
        var service = new ReportService(
            new FakeHealthCenterRepository(),
            new FakeReferralMemberRepository(),
            repository,
            new ReportTotalCountCache(memoryCache),
            NullLogger<ReportService>.Instance);

        Assert.Same(exception, Assert.Throws<InvalidOperationException>(() =>
            service.ReportDataAndColumns<SafeNeedleReportViewModel>(CreateC19Condition(
                EncounterSources.Emergency, null, 1, 10))));

        repository.CountException = null;
        service.ReportDataAndColumns<SafeNeedleReportViewModel>(CreateC19Condition(
            EncounterSources.Emergency, null, 1, 10));

        Assert.Equal(2, repository.CountCalls);
        Assert.Equal(1, repository.PageCalls);
    }

    private static SearchReportCondition CreateC19Condition(
        string encounterSource,
        string? prefix,
        int pageNumber,
        int pageSize) => new()
    {
        ReportCode = "C19",
        StartDate = "2026-08-24",
        EndDate = "2026-08-24",
        EncounterSource = encounterSource,
        StationOrBedPrefix = prefix,
        PageNumber = pageNumber,
        PageSize = pageSize
    };

    [Fact]
    public void ReferralMemberReportViewModel_ProvidesTwentyColumnsInLegacyOrder()
    {
        var columns = ModelDescriptionsHelper.GetPropertyDescriptions<ReferralMemberReportViewModel>();

        Assert.Equal(20, columns.Count);
        Assert.Equal(
        [
            "診院代碼", "診院名稱", "身分證號", "收案類別", "病患姓名", "病歷號", "就診科別", "病床號", "主治醫師", "住院日期",
            "出院日期", "診斷碼1", "診斷碼2", "診斷碼3", "診斷碼名稱1", "診斷碼名稱2", "診斷碼名稱3", "網路同意", "完整回覆", "轉診註記"
        ],
        columns.Select(column => column.Label));
    }

    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(61, 30, 3)]
    public void CalculateTotalPages_UsesSpecifiedBoundaries(int totalCount, int pageSize, int expected)
    {
        Assert.Equal(expected, ReportService.CalculateTotalPages(totalCount, pageSize));
    }

    [Fact]
    public void ReportDataAndColumns_C171BeyondLastPage_ReturnsEmptyDataWithCountMetadata()
    {
        var repository = new FakeHealthCenterRepository { TotalCount = 28 };
        var service = CreateService(repository);

        var result = service.ReportDataAndColumns<HealthCenterDetailViewModel>(new SearchReportCondition
        {
            ReportCode = "C171",
            PageNumber = 4,
            PageSize = 10
        });

        Assert.Empty(result.Data!);
        Assert.Equal(28, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(4, result.PageNumber);
        Assert.Equal(10, result.PageSize);
    }

    [Fact]
    public void ReportDataAndColumns_C174Page_ReturnsPageAndCountMetadata()
    {
        var repository = new FakeHealthCenterRepository
        {
            C174TotalCount = 61,
            C174Data = [new HealthCenterContractBillingReport()]
        };
        var service = CreateService(repository);

        var result = service.ReportDataAndColumns<HealthCenterContractBillingReport>(new SearchReportCondition
        {
            ReportCode = "C174",
            StartDate = "2026-08-01",
            EndDate = "2026-08-31",
            PageNumber = 2,
            PageSize = 30
        });

        Assert.Single(result.Data!);
        Assert.Equal(61, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(30, result.PageSize);
    }

    [Fact]
    public void ReportDataAndColumns_C174BeyondLastPage_ReturnsEmptyDataWithCountMetadata()
    {
        var repository = new FakeHealthCenterRepository { C174TotalCount = 28 };
        var service = CreateService(repository);

        var result = service.ReportDataAndColumns<HealthCenterContractBillingReport>(new SearchReportCondition
        {
            ReportCode = "C174",
            StartDate = "2026-08-01",
            EndDate = "2026-08-31",
            PageNumber = 4,
            PageSize = 10
        });

        Assert.Empty(result.Data!);
        Assert.Equal(28, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(4, result.PageNumber);
    }

    [Fact]
    public void ReportDataAndColumns_C174PageFailure_PropagatesException()
    {
        var exception = new InvalidOperationException("database failed");
        var repository = new FakeHealthCenterRepository
        {
            C174TotalCount = 1,
            C174PageException = exception
        };
        var service = CreateService(repository);

        var actual = Assert.Throws<InvalidOperationException>(() =>
            service.ReportDataAndColumns<HealthCenterContractBillingReport>(new SearchReportCondition
            {
                ReportCode = "C174",
                StartDate = "2026-08-01",
                EndDate = "2026-08-31",
                PageNumber = 1,
                PageSize = 10
            }));

        Assert.Same(exception, actual);
    }

    [Fact]
    public void ReportDataAndColumns_C174SameDates_CachesCountButAlwaysQueriesPage()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeHealthCenterRepository { C174TotalCount = 28 };
        var service = new ReportService(
            repository,
            new FakeReferralMemberRepository(),
            new FakeSafeNeedleRepository(),
            new ReportTotalCountCache(memoryCache),
            NullLogger<ReportService>.Instance);

        service.ReportDataAndColumns<HealthCenterContractBillingReport>(CreateC174Condition("2026-08-01", 1, 10));
        service.ReportDataAndColumns<HealthCenterContractBillingReport>(CreateC174Condition("2026-08-01", 2, 30));
        service.ReportDataAndColumns<HealthCenterContractBillingReport>(CreateC174Condition("2026-08-02", 1, 10));

        Assert.Equal(2, repository.C174CountCalls);
        Assert.Equal(3, repository.C174PageCalls);
    }

    [Fact]
    public void ReportDataAndColumns_C18Page_ReturnsTwentyColumnsAndCountMetadata()
    {
        var referralRepository = new FakeReferralMemberRepository
        {
            TotalCount = 28,
            PageRowCount = 10
        };
        var service = new ReportService(
            new FakeHealthCenterRepository(),
            referralRepository,
            new FakeSafeNeedleRepository(),
            new PassthroughReportTotalCountCache(),
            NullLogger<ReportService>.Instance);

        var result = service.ReportDataAndColumns<ReferralMemberReportViewModel>(CreateC18Condition(
            EncounterSources.Emergency,
            1,
            10));

        Assert.Equal(20, result.Columns!.Count);
        Assert.Equal(10, result.Data!.Count);
        Assert.Equal(28, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal("1150101", referralRepository.LastCountStartDate);
        Assert.Equal("1151231", referralRepository.LastCountEndDate);
        Assert.Equal(EncounterSources.Emergency, referralRepository.LastPageSource);
    }

    [Fact]
    public void ReportDataAndColumns_C18EmptyResult_ReturnsZeroPages()
    {
        var referralRepository = new FakeReferralMemberRepository();
        var service = new ReportService(
            new FakeHealthCenterRepository(),
            referralRepository,
            new FakeSafeNeedleRepository(),
            new PassthroughReportTotalCountCache(),
            NullLogger<ReportService>.Instance);

        var result = service.ReportDataAndColumns<ReferralMemberReportViewModel>(CreateC18Condition(
            EncounterSources.Inpatient,
            1,
            10));

        Assert.Empty(result.Data!);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public void ReportDataAndColumns_C18CacheSeparatesEncounterSourcesAndAlwaysQueriesPage()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var referralRepository = new FakeReferralMemberRepository { TotalCount = 28 };
        var service = new ReportService(
            new FakeHealthCenterRepository(),
            referralRepository,
            new FakeSafeNeedleRepository(),
            new ReportTotalCountCache(memoryCache),
            NullLogger<ReportService>.Instance);

        service.ReportDataAndColumns<ReferralMemberReportViewModel>(CreateC18Condition(
            EncounterSources.Emergency,
            1,
            10));
        service.ReportDataAndColumns<ReferralMemberReportViewModel>(CreateC18Condition(
            EncounterSources.Emergency,
            2,
            30));
        service.ReportDataAndColumns<ReferralMemberReportViewModel>(CreateC18Condition(
            EncounterSources.Inpatient,
            1,
            10));

        Assert.Equal(2, referralRepository.CountCalls);
        Assert.Equal(3, referralRepository.PageCalls);
    }

    private static SearchReportCondition CreateC174Condition(
        string startDate,
        int pageNumber,
        int pageSize) => new()
    {
        ReportCode = "C174",
        StartDate = startDate,
        EndDate = "2026-08-31",
        PageNumber = pageNumber,
        PageSize = pageSize
    };

    [Fact]
    public void ReportDataAndColumns_C171SameDates_CachesCountAcrossPageInputs()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeHealthCenterRepository { TotalCount = 61 };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<HealthCenterDetailViewModel>(CreateC171Condition("2026-08-01", "2026-08-31", 1, 10));
        var secondPage = service.ReportDataAndColumns<HealthCenterDetailViewModel>(
            CreateC171Condition("2026-08-01", "2026-08-31", 2, 30));

        Assert.Equal(1, repository.C171CountCalls);
        Assert.Equal(2, repository.C171PageCalls);
        Assert.Equal(61, secondPage.TotalCount);
        Assert.Equal(3, secondPage.TotalPages);
        Assert.Equal(2, secondPage.PageNumber);
        Assert.Equal(30, secondPage.PageSize);
    }

    [Fact]
    public void ReportDataAndColumns_C171ChangedDate_UsesDifferentCountCacheEntry()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeHealthCenterRepository { TotalCount = 61 };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<HealthCenterDetailViewModel>(CreateC171Condition("2026-08-01", "2026-08-31", 1, 10));
        service.ReportDataAndColumns<HealthCenterDetailViewModel>(CreateC171Condition("2026-08-02", "2026-08-31", 1, 10));

        Assert.Equal(2, repository.C171CountCalls);
    }

    [Fact]
    public void ReportDataAndColumns_C171CountFailure_IsRetriedAndNotCached()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var exception = new InvalidOperationException("count failed");
        var repository = new FakeHealthCenterRepository { C171CountException = exception };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));
        SearchReportCondition firstCondition = CreateC171Condition("2026-08-01", "2026-08-31", 1, 10);

        Assert.Same(exception, Assert.Throws<InvalidOperationException>(() =>
            service.ReportDataAndColumns<HealthCenterDetailViewModel>(firstCondition)));

        repository.C171CountException = null;
        service.ReportDataAndColumns<HealthCenterDetailViewModel>(
            CreateC171Condition("2026-08-01", "2026-08-31", 1, 10));

        Assert.Equal(2, repository.C171CountCalls);
        Assert.Equal(1, repository.C171PageCalls);
    }

    [Fact]
    public void ReportDataAndColumns_C171CountResolution_LogsCacheMissThenHit()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeHealthCenterRepository { TotalCount = 61 };
        var logger = new CapturingLogger<ReportService>();
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache), logger);

        service.ReportDataAndColumns<HealthCenterDetailViewModel>(
            CreateC171Condition("2026-08-01", "2026-08-31", 1, 10));
        service.ReportDataAndColumns<HealthCenterDetailViewModel>(
            CreateC171Condition("2026-08-01", "2026-08-31", 2, 30));

        Assert.Equal(1, repository.C171CountCalls);
        Assert.Equal(2, repository.C171PageCalls);
        Assert.Collection(
            logger.Entries,
            miss => AssertC171CountResolutionLog(miss, expectedCacheHit: false),
            hit => AssertC171CountResolutionLog(hit, expectedCacheHit: true));
    }

    private static SearchReportCondition CreateC171Condition(
        string startDate,
        string endDate,
        int pageNumber,
        int pageSize) => new()
    {
        ReportCode = "C171",
        StartDate = startDate,
        EndDate = endDate,
        PageNumber = pageNumber,
        PageSize = pageSize
    };

    private static void AssertC171CountResolutionLog(CapturedLog entry, bool expectedCacheHit)
    {
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Equal("C171", entry.Properties["ReportCode"]);
        Assert.Equal("1150801", entry.Properties["StartDate"]);
        Assert.Equal("1150831", entry.Properties["EndDate"]);
        Assert.Equal(expectedCacheHit, entry.Properties["CacheHit"]);
        Assert.Equal(61, entry.Properties["TotalCount"]);
        Assert.True(Assert.IsType<long>(entry.Properties["TotalCountResolutionElapsedMs"]) >= 0);
        Assert.Equal(
            ["CacheHit", "EndDate", "ReportCode", "StartDate", "TotalCount", "TotalCountResolutionElapsedMs"],
            entry.Properties.Keys.Order(StringComparer.Ordinal));
    }

    private static SearchReportCondition CreateC18Condition(
        string encounterSource,
        int pageNumber,
        int pageSize) => new()
    {
        ReportCode = "C18",
        StartDate = "2026-01-01",
        EndDate = "2026-12-31",
        EncounterSource = encounterSource,
        PageNumber = pageNumber,
        PageSize = pageSize
    };

    private static ReportService CreateService(
        FakeHealthCenterRepository repository,
        IReportTotalCountCache? totalCountCache = null,
        Microsoft.Extensions.Logging.ILogger<ReportService>? logger = null) => new(
        repository,
        new FakeReferralMemberRepository(),
        new FakeSafeNeedleRepository(),
        totalCountCache ?? new PassthroughReportTotalCountCache(),
        logger ?? NullLogger<ReportService>.Instance);
}
