using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class InpatientAdvancePaymentBalanceReportServiceTests
{
    [Fact]
    public void ReportDataAndColumns_C25_ReturnsColumnsRowsAndPageMetadata()
    {
        var repository = new FakeRepository { TotalCount = 73, PageRowCount = 30 };

        var result = CreateService(repository, new PassthroughReportTotalCountCache())
            .ReportDataAndColumns<InpatientAdvancePaymentBalanceReportViewModel>(
                Condition("2026-08-01", "2026-08-31", 2, 30));

        Assert.Equal(6, result.Columns!.Count);
        Assert.Equal(30, result.Data!.Count);
        Assert.Equal(73, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(30, result.PageSize);
        Assert.Equal("1150801", repository.LastStartDate);
        Assert.Equal("1150831", repository.LastEndDate);
    }

    [Fact]
    public void ReportDataAndColumns_C25_SameDatesAcrossPageAndSize_ReusesCount()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeRepository { TotalCount = 73 };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<InpatientAdvancePaymentBalanceReportViewModel>(
            Condition("2026-08-01", "2026-08-31", 1, 10));
        service.ReportDataAndColumns<InpatientAdvancePaymentBalanceReportViewModel>(
            Condition("2026-08-01", "2026-08-31", 2, 30));

        Assert.Equal(1, repository.CountCalls);
        Assert.Equal(2, repository.PageCalls);
    }

    [Fact]
    public void ReportDataAndColumns_C25_ChangedStartOrEndDate_RecomputesCount()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeRepository { TotalCount = 73 };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<InpatientAdvancePaymentBalanceReportViewModel>(
            Condition("2026-08-01", "2026-08-31", 1, 10));
        service.ReportDataAndColumns<InpatientAdvancePaymentBalanceReportViewModel>(
            Condition("2026-08-02", "2026-08-31", 1, 10));
        service.ReportDataAndColumns<InpatientAdvancePaymentBalanceReportViewModel>(
            Condition("2026-08-01", "2026-08-30", 1, 10));

        Assert.Equal(3, repository.CountCalls);
    }

    [Fact]
    public void ReportDataAndColumns_C25_FailedCountIsRetried()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeRepository
        {
            TotalCount = 1,
            CountException = new InvalidOperationException("count failed")
        };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        Assert.Throws<InvalidOperationException>(() =>
            service.ReportDataAndColumns<InpatientAdvancePaymentBalanceReportViewModel>(
                Condition("2026-08-01", "2026-08-31", 1, 10)));
        repository.CountException = null;

        var result = service.ReportDataAndColumns<InpatientAdvancePaymentBalanceReportViewModel>(
            Condition("2026-08-01", "2026-08-31", 1, 10));

        Assert.Equal(2, repository.CountCalls);
        Assert.Equal(1, result.TotalCount);
    }

    private static ReportService CreateService(
        IInpatientAdvancePaymentBalanceRepository repository,
        IReportTotalCountCache cache) => new(
            new FakeHealthCenterRepository(),
            new FakeReferralMemberRepository(),
            new FakeSafeNeedleRepository(),
            cache,
            NullLogger<ReportService>.Instance,
            inpatientAdvancePaymentBalanceRepository: repository);

    private static SearchReportCondition Condition(
        string startDate,
        string endDate,
        int pageNumber,
        int pageSize) => new()
        {
            ReportCode = "C25",
            StartDate = startDate,
            EndDate = endDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

    private sealed class FakeRepository : IInpatientAdvancePaymentBalanceRepository
    {
        public int TotalCount { get; init; }
        public int PageRowCount { get; init; }
        public Exception? CountException { get; set; }
        public int CountCalls { get; private set; }
        public int PageCalls { get; private set; }
        public string? LastStartDate { get; private set; }
        public string? LastEndDate { get; private set; }

        public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
            ModelDescriptionsHelper.GetPropertyDescriptions<InpatientAdvancePaymentBalanceReportViewModel>();

        public int GetCount(SearchReportCondition searchCondition)
        {
            CountCalls++;
            LastStartDate = searchCondition.StartDate;
            LastEndDate = searchCondition.EndDate;
            if (CountException is not null)
            {
                throw CountException;
            }
            return TotalCount;
        }

        public List<InpatientAdvancePaymentBalanceReportViewModel> GetPage(SearchReportCondition searchCondition)
        {
            PageCalls++;
            LastStartDate = searchCondition.StartDate;
            LastEndDate = searchCondition.EndDate;
            return Enumerable.Range(0, PageRowCount)
                .Select(_ => new InpatientAdvancePaymentBalanceReportViewModel())
                .ToList();
        }
    }
}
