using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class InpatientReceivableBalanceReportServiceTests
{
    [Fact]
    public void ReportDataAndColumns_C28_ReturnsColumnsRowsAndPageMetadata()
    {
        var repository = new FakeRepository { TotalCount = 73, PageRowCount = 30 };

        var result = CreateService(repository, new PassthroughReportTotalCountCache())
            .ReportDataAndColumns<InpatientReceivableBalanceReportViewModel>(
                Condition("2026-08-31", 2, 30));

        Assert.Equal(8, result.Columns!.Count);
        Assert.Equal(30, result.Data!.Count);
        Assert.Equal(73, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(30, result.PageSize);
        Assert.Null(repository.LastStartDate);
        Assert.Equal("1150831", repository.LastEndDate);
    }

    [Fact]
    public void ReportDataAndColumns_C28_SameCutoffAcrossPageAndSize_ReusesCount()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeRepository { TotalCount = 73 };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<InpatientReceivableBalanceReportViewModel>(
            Condition("2026-08-31", 1, 10));
        service.ReportDataAndColumns<InpatientReceivableBalanceReportViewModel>(
            Condition("2026-08-31", 2, 30));

        Assert.Equal(1, repository.CountCalls);
        Assert.Equal(2, repository.PageCalls);
    }

    [Fact]
    public void ReportDataAndColumns_C28_DifferentCutoffs_UseSeparateCountEntries()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeRepository { TotalCount = 73 };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<InpatientReceivableBalanceReportViewModel>(
            Condition("2026-08-31", 1, 10));
        service.ReportDataAndColumns<InpatientReceivableBalanceReportViewModel>(
            Condition("2026-08-30", 1, 10));

        Assert.Equal(2, repository.CountCalls);
    }

    private static ReportService CreateService(
        IInpatientReceivableBalanceRepository repository,
        IReportTotalCountCache cache) => new(
            new FakeHealthCenterRepository(),
            new FakeReferralMemberRepository(),
            new FakeSafeNeedleRepository(),
            cache,
            NullLogger<ReportService>.Instance,
            inpatientReceivableBalanceRepository: repository);

    private static SearchReportCondition Condition(
        string endDate,
        int pageNumber,
        int pageSize) => new()
        {
            ReportCode = "C28",
            EndDate = endDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

    private sealed class FakeRepository : IInpatientReceivableBalanceRepository
    {
        public int TotalCount { get; init; }
        public int PageRowCount { get; init; }
        public int CountCalls { get; private set; }
        public int PageCalls { get; private set; }
        public string? LastStartDate { get; private set; }
        public string? LastEndDate { get; private set; }

        public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
            ModelDescriptionsHelper.GetPropertyDescriptions<InpatientReceivableBalanceReportViewModel>();

        public int GetCount(SearchReportCondition searchCondition)
        {
            CountCalls++;
            CaptureDates(searchCondition);
            return TotalCount;
        }

        public List<InpatientReceivableBalanceReportViewModel> GetPage(SearchReportCondition searchCondition)
        {
            PageCalls++;
            CaptureDates(searchCondition);
            return Enumerable.Range(0, PageRowCount)
                .Select(_ => new InpatientReceivableBalanceReportViewModel())
                .ToList();
        }

        private void CaptureDates(SearchReportCondition searchCondition)
        {
            LastStartDate = searchCondition.StartDate;
            LastEndDate = searchCondition.EndDate;
        }
    }
}
