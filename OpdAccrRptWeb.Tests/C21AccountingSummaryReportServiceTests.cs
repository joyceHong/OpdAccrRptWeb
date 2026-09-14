using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C21AccountingSummaryReportServiceTests
{
    [Fact]
    public void ReportDataAndColumns_PaginatesOneCanonicalCrossGroupResult()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository, new PassthroughReportTotalCountCache(), false);

        var result = service.ReportDataAndColumns<C21AccountingSummaryReportViewModel>(Condition(2, 10));

        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Equal(10, result.Data!.Count);
        Assert.Equal(11, result.Data[0].RowOrder);
        Assert.Equal(1, repository.SourceCalls);
    }

    [Fact]
    public void ReportDataAndColumns_CountCacheSeparatesFiltersAndStillQueriesEveryPage()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var repository = new FakeRepository();
        var calculation = new CountingCalculationService();
        var service = CreateService(repository, new ReportTotalCountCache(memory), false, calculation);

        service.ReportDataAndColumns<C21AccountingSummaryReportViewModel>(Condition(1, 10));
        service.ReportDataAndColumns<C21AccountingSummaryReportViewModel>(Condition(2, 10));
        var different = Condition(1, 10);
        different.AccountingScope = 2;
        service.ReportDataAndColumns<C21AccountingSummaryReportViewModel>(different);

        Assert.Equal(3, repository.SourceCalls);
        Assert.Equal(3, calculation.Calls);
    }

    [Fact]
    public void ReportDataAndColumns_SuccessfulRebuildInvalidatesCachedCount()
    {
        var cache = new RecordingCache();
        var service = CreateService(new FakeRepository(), cache, true);

        service.ReportDataAndColumns<C21AccountingSummaryReportViewModel>(Condition(1, 10));

        Assert.Equal("C21", cache.InvalidatedReport);
    }

    [Fact]
    public void ReportDataAndColumns_CacheIdentityContainsFiltersButNotPagingOrForceRebuild()
    {
        var cache = new RecordingCache();
        var service = CreateService(new FakeRepository(), cache, false);
        var condition = Condition(3, 50);
        condition.BillingCode = "49";
        condition.ForceRebuild = true;

        service.ReportDataAndColumns<C21AccountingSummaryReportViewModel>(condition);

        Assert.Equal("2026-09-08", cache.Filters![nameof(SearchReportCondition.StartDate)]);
        Assert.Equal("Outpatient", cache.Filters[nameof(SearchReportCondition.EncounterSource)]);
        Assert.Equal("0", cache.Filters[nameof(SearchReportCondition.AccountingScope)]);
        Assert.Equal("49", cache.Filters[nameof(SearchReportCondition.BillingCode)]);
        Assert.DoesNotContain(nameof(SearchReportCondition.PageNumber), cache.Filters.Keys);
        Assert.DoesNotContain(nameof(SearchReportCondition.PageSize), cache.Filters.Keys);
        Assert.DoesNotContain(nameof(SearchReportCondition.ForceRebuild), cache.Filters.Keys);
    }

    private static ReportService CreateService(
        IC21AccountingSummaryRepository repository,
        IReportTotalCountCache cache,
        bool rebuilt,
        IC21AccountingSummaryCalculationService? calculation = null) => new(
            new FakeHealthCenterRepository(), new FakeReferralMemberRepository(), new FakeSafeNeedleRepository(),
            cache, NullLogger<ReportService>.Instance,
            c21AccountingSummaryRepository: repository,
            c21CalculationService: calculation ?? new FixedCalculationService(),
            c21RebuildService: new FixedRebuildService(rebuilt));

    private static SearchReportCondition Condition(int page, int size) => new()
    {
        ReportCode = "C21", StartDate = "2026-09-08", EndDate = "2026-09-08",
        EncounterSource = C21EncounterSources.Outpatient, AccountingScope = 0,
        PageNumber = page, PageSize = size
    };

    private sealed class FakeRepository : IC21AccountingSummaryRepository
    {
        public int SourceCalls { get; private set; }
        public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
            ModelDescriptionsHelper.GetPropertyDescriptions<C21AccountingSummaryReportViewModel>();
        public IReadOnlyList<C21BillingItem> GetBillingItems() => [];
        public IReadOnlyList<C21SourceAmount> GetSourceAmounts(SearchReportCondition condition)
        {
            SourceCalls++;
            return [];
        }
        public bool HasInpatientRoom23Data(string rocDate) => true;
        public void RebuildSingleInpatientDay(string rocDate) { }
    }

    private class FixedCalculationService : IC21AccountingSummaryCalculationService
    {
        public virtual IReadOnlyList<C21AccountingSummaryReportViewModel> Calculate(
            SearchReportCondition condition, IReadOnlyCollection<C21SourceAmount> sourceAmounts,
            IReadOnlyCollection<C21BillingItem> billingItems) =>
            Enumerable.Range(1, 25).Select(number => new C21AccountingSummaryReportViewModel
            {
                GroupCode = number <= 12 ? "1" : "2", GroupName = "group", RowOrder = number,
                RowType = "Detail", BillingCode = number.ToString("D2"), BillingName = "item"
            }).ToList();
    }

    private sealed class CountingCalculationService : FixedCalculationService
    {
        public int Calls { get; private set; }
        public override IReadOnlyList<C21AccountingSummaryReportViewModel> Calculate(
            SearchReportCondition condition, IReadOnlyCollection<C21SourceAmount> sourceAmounts,
            IReadOnlyCollection<C21BillingItem> billingItems)
        {
            Calls++;
            return base.Calculate(condition, sourceAmounts, billingItems);
        }
    }

    private sealed class FixedRebuildService(bool rebuilt) : IC21RebuildService
    {
        public bool EnsureData(SearchReportCondition condition) => rebuilt;
    }

    private sealed class RecordingCache : IReportTotalCountCache
    {
        public string? InvalidatedReport { get; private set; }
        public IReadOnlyDictionary<string, string?>? Filters { get; private set; }
        public int GetOrCreate(string reportCode, IReadOnlyDictionary<string, string?> normalizedFilters,
            Func<int> countFactory)
        {
            Filters = normalizedFilters;
            return countFactory();
        }
        public void Invalidate(string reportCode) => InvalidatedReport = reportCode;
    }
}
