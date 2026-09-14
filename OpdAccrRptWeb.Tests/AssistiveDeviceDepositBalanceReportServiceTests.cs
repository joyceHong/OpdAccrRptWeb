using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class AssistiveDeviceDepositBalanceReportServiceTests
{
    [Fact]
    public void ReportDataAndColumns_C27_ReturnsColumnsRowsAndPageMetadata()
    {
        var repository = new FakeRepository { TotalCount = 73, PageRowCount = 30 };

        var result = CreateService(repository, new PassthroughReportTotalCountCache())
            .ReportDataAndColumns<AssistiveDeviceDepositBalanceReportViewModel>(
                Condition("2026-08-31", 2, 30));

        Assert.Equal(6, result.Columns!.Count);
        Assert.Equal(30, result.Data!.Count);
        Assert.Equal(73, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(2, result.PageNumber);
        Assert.Equal(30, result.PageSize);
        Assert.Null(repository.LastStartDate);
        Assert.Equal("1150831", repository.LastEndDate);
    }

    [Fact]
    public void ReportDataAndColumns_C27_SameCutoffAcrossPageAndSize_ReusesCount()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeRepository { TotalCount = 73 };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<AssistiveDeviceDepositBalanceReportViewModel>(
            Condition("2026-08-31", 1, 10));
        service.ReportDataAndColumns<AssistiveDeviceDepositBalanceReportViewModel>(
            Condition("2026-08-31", 2, 30));

        Assert.Equal(1, repository.CountCalls);
        Assert.Equal(2, repository.PageCalls);
    }

    [Fact]
    public void ReportDataAndColumns_C27_DifferentCutoffs_UseSeparateCountEntries()
    {
        using var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeRepository { TotalCount = 73 };
        var service = CreateService(repository, new ReportTotalCountCache(memoryCache));

        service.ReportDataAndColumns<AssistiveDeviceDepositBalanceReportViewModel>(
            Condition("2026-08-31", 1, 10));
        service.ReportDataAndColumns<AssistiveDeviceDepositBalanceReportViewModel>(
            Condition("2026-08-30", 1, 10));

        Assert.Equal(2, repository.CountCalls);
    }

    private static ReportService CreateService(
        IAssistiveDeviceDepositBalanceRepository repository,
        IReportTotalCountCache cache) => new(
            new FakeHealthCenterRepository(),
            new FakeReferralMemberRepository(),
            new FakeSafeNeedleRepository(),
            cache,
            NullLogger<ReportService>.Instance,
            assistiveDeviceDepositBalanceRepository: repository);

    private static SearchReportCondition Condition(
        string endDate,
        int pageNumber,
        int pageSize) => new()
        {
            ReportCode = "C27",
            EndDate = endDate,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

    private sealed class FakeRepository : IAssistiveDeviceDepositBalanceRepository
    {
        public int TotalCount { get; init; }
        public int PageRowCount { get; init; }
        public int CountCalls { get; private set; }
        public int PageCalls { get; private set; }
        public string? LastStartDate { get; private set; }
        public string? LastEndDate { get; private set; }

        public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
            ModelDescriptionsHelper.GetPropertyDescriptions<AssistiveDeviceDepositBalanceReportViewModel>();

        public int GetCount(SearchReportCondition searchCondition)
        {
            CountCalls++;
            CaptureDates(searchCondition);
            return TotalCount;
        }

        public List<AssistiveDeviceDepositBalanceReportViewModel> GetPage(SearchReportCondition searchCondition)
        {
            PageCalls++;
            CaptureDates(searchCondition);
            return Enumerable.Range(0, PageRowCount)
                .Select(_ => new AssistiveDeviceDepositBalanceReportViewModel())
                .ToList();
        }

        private void CaptureDates(SearchReportCondition searchCondition)
        {
            LastStartDate = searchCondition.StartDate;
            LastEndDate = searchCondition.EndDate;
        }
    }
}
