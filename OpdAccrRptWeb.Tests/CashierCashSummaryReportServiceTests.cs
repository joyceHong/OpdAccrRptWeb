using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class CashierCashSummaryReportServiceTests
{
    [Fact]
    public void ReportDataAndColumns_C213_ReturnsEightColumnsAndPagedMetadata()
    {
        var repository = new FakeCashierCashSummaryRepository
        {
            TotalCount = 24,
            Data = Enumerable.Range(0, 10)
                .Select(_ => new CashierCashSummaryReportViewModel())
                .ToList()
        };

        var result = CreateService(repository, new PassthroughReportTotalCountCache())
            .ReportDataAndColumns<CashierCashSummaryReportViewModel>(Condition("2026-08-01", "2026-08-31", 1, 10));

        Assert.Equal(8, result.Columns!.Count);
        Assert.Equal(10, result.Data!.Count);
        Assert.Equal(24, result.TotalCount);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal("1150801", repository.CountConditions.Single().StartDate);
        Assert.Equal("1150831", repository.CountConditions.Single().EndDate);
    }

    [Fact]
    public void ReportDataAndColumns_C213_CachesCountByDatesButAlwaysQueriesPage()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeCashierCashSummaryRepository { TotalCount = 24 };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<CashierCashSummaryReportViewModel>(Condition("2026-08-01", "2026-08-31", 1, 10));
        service.ReportDataAndColumns<CashierCashSummaryReportViewModel>(Condition("2026-08-01", "2026-08-31", 2, 30));
        service.ReportDataAndColumns<CashierCashSummaryReportViewModel>(Condition("2026-08-02", "2026-08-31", 1, 10));

        Assert.Equal(2, repository.CountCalls);
        Assert.Equal(3, repository.PageCalls);
        Assert.Equal(2, repository.PageConditions[1].PageNumber);
        Assert.Equal(30, repository.PageConditions[1].PageSize);
    }

    [Fact]
    public void ReportDataAndColumns_C213_CountFailureIsNotCached()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var failure = new InvalidOperationException("count failed");
        var repository = new FakeCashierCashSummaryRepository
        {
            TotalCount = 1,
            CountException = failure
        };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
            service.ReportDataAndColumns<CashierCashSummaryReportViewModel>(Condition("2026-08-01", "2026-08-31", 1, 10))));
        repository.CountException = null;

        var result = service.ReportDataAndColumns<CashierCashSummaryReportViewModel>(
            Condition("2026-08-01", "2026-08-31", 1, 10));

        Assert.Equal(2, repository.CountCalls);
        Assert.Equal(1, repository.PageCalls);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public void ReportDataAndColumns_C213_PageFailurePropagates()
    {
        var failure = new InvalidOperationException("page failed");
        var repository = new FakeCashierCashSummaryRepository
        {
            TotalCount = 1,
            PageException = failure
        };

        Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
            CreateService(repository, new PassthroughReportTotalCountCache())
                .ReportDataAndColumns<CashierCashSummaryReportViewModel>(Condition("2026-08-01", "2026-08-31", 1, 10))));
        Assert.Equal(1, repository.CountCalls);
        Assert.Equal(1, repository.PageCalls);
    }

    private static ReportService CreateService(
        FakeCashierCashSummaryRepository repository,
        IReportTotalCountCache cache) => new(
            new FakeHealthCenterRepository(),
            new FakeReferralMemberRepository(),
            new FakeSafeNeedleRepository(),
            cache,
            NullLogger<ReportService>.Instance,
            cashierCashSummaryRepository: repository);

    private static SearchReportCondition Condition(
        string startDate,
        string endDate,
        int pageNumber,
        int pageSize) => new()
    {
        ReportCode = "C213",
        StartDate = startDate,
        EndDate = endDate,
        PageNumber = pageNumber,
        PageSize = pageSize
    };
}
