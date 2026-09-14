using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class CashierCashReportServiceTests
{
    [Fact]
    public void ReportDataAndColumns_C22_ReturnsPageMetadataAndColumns()
    {
        var repository = new FakeCashierCashRepository { TotalCount = 73, PageRowCount = 30 };
        var service = CreateService(repository, new PassthroughReportTotalCountCache());
        var result = service.ReportDataAndColumns<CashierCashReportViewModel>(Condition(1, 30));
        Assert.Equal(17, result.Columns!.Count);
        Assert.Equal(30, result.Data!.Count);
        Assert.Equal(73, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public void ReportDataAndColumns_C22_CountCacheIgnoresPageAndSortButSeparatesCashier()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeCashierCashRepository { TotalCount = 73 };
        var service = CreateService(repository, new ReportTotalCountCache(cache));
        service.ReportDataAndColumns<CashierCashReportViewModel>(Condition(1, 10, "A123", CashierCashSortTypes.Cashier));
        service.ReportDataAndColumns<CashierCashReportViewModel>(Condition(2, 30, "A123", CashierCashSortTypes.Encounter));
        service.ReportDataAndColumns<CashierCashReportViewModel>(Condition(1, 10, "B456", CashierCashSortTypes.Cashier));
        Assert.Equal(2, repository.CountCalls);
        Assert.Equal(3, repository.PageCalls);
    }

    [Fact]
    public void ReportDataAndColumns_C22_EmptyFourthPagePreservesTotalMetadata()
    {
        var repository = new FakeCashierCashRepository { TotalCount = 73, PageRowCount = 0 };
        var result = CreateService(repository, new PassthroughReportTotalCountCache())
            .ReportDataAndColumns<CashierCashReportViewModel>(Condition(4, 30));
        Assert.Empty(result.Data!);
        Assert.Equal(73, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(4, result.PageNumber);
    }

    private static ReportService CreateService(ICashierCashRepository repository, IReportTotalCountCache cache) => new(
        new FakeHealthCenterRepository(), new FakeReferralMemberRepository(), new FakeSafeNeedleRepository(),
        cache, NullLogger<ReportService>.Instance, cashierCashRepository: repository);

    private static SearchReportCondition Condition(int page, int size, string? cashier = null, string sort = CashierCashSortTypes.Cashier) => new()
    {
        ReportCode = "C22", StartDate = "2026-08-01", EndDate = "2026-08-31",
        CashierUserId = cashier, CashierCashSortType = sort, PageNumber = page, PageSize = size
    };

    private sealed class FakeCashierCashRepository : ICashierCashRepository
    {
        public int TotalCount { get; init; }
        public int PageRowCount { get; init; }
        public int CountCalls { get; private set; }
        public int PageCalls { get; private set; }
        public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
            ModelDescriptionsHelper.GetPropertyDescriptions<CashierCashReportViewModel>();
        public int GetCount(SearchReportCondition condition) { CountCalls++; return TotalCount; }
        public List<CashierCashReportViewModel> GetPage(SearchReportCondition condition)
        {
            PageCalls++;
            return Enumerable.Range(0, PageRowCount).Select(_ => new CashierCashReportViewModel()).ToList();
        }
    }
}
