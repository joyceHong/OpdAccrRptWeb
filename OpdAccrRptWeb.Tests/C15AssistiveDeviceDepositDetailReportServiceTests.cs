using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C15AssistiveDeviceDepositDetailReportServiceTests
{
    [Fact]
    public async Task ReportC15Async_ReducesBeforeCountingSortingAndPaging()
    {
        var repository = new FakeRepository
        {
            Rows =
            [
                Source("696-001", 10m, 1m), Source("696-002", 20m, 1m),
                Source("696-003", 30m, 1m), Source("696-004", 40m, 1m),
                Source("696-008", 500m, 2m)
            ]
        };
        ReportService service = CreateService(repository, new PassthroughReportTotalCountCache());

        ReportDataAndColumns<C15AssistiveDeviceDepositDetailReportViewModel> result =
            await service.ReportC15Async(Condition(pageSize: 10));

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(1, result.TotalPages);
        Assert.Equal(["1", "2"], result.Data!.Select(row => row.Type));
        Assert.Equal(10, result.Columns!.Count);
        Assert.Equal(2, Assert.IsType<C15ReportSummary>(result.Summary).Groups.Count);
    }

    [Fact]
    public async Task ReportC15Async_SameDatesAcrossPagesReuseCountButQueryEveryTime()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new CountingCache(new ReportTotalCountCache(memory));
        var repository = new FakeRepository { Rows = [Source("696-001", 10m, 1m)] };
        ReportService service = CreateService(repository, cache);

        await service.ReportC15Async(Condition(pageNumber: 1, pageSize: 10));
        await service.ReportC15Async(Condition(pageNumber: 2, pageSize: 30));

        Assert.Equal(2, repository.QueryCalls);
        Assert.Equal(2, cache.Calls);
        Assert.Equal(1, cache.FactoryCalls);
    }

    [Fact]
    public async Task ReportC15Async_DifferentDatesUseDifferentCountKeys()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new CountingCache(new ReportTotalCountCache(memory));
        var repository = new FakeRepository { Rows = [Source("696-001", 10m, 1m)] };
        ReportService service = CreateService(repository, cache);

        await service.ReportC15Async(Condition());
        SearchReportCondition other = Condition();
        other.EndDate = "2026-09-15";
        await service.ReportC15Async(other);

        Assert.Equal(2, cache.FactoryCalls);
    }

    [Fact]
    public async Task ReportC15Async_RepositoryFailureDoesNotPopulateCache()
    {
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var cache = new CountingCache(new ReportTotalCountCache(memory));
        var repository = new FakeRepository { Failure = new InvalidOperationException("sensitive") };
        ReportService service = CreateService(repository, cache);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReportC15Async(Condition()));
        repository.Failure = null;
        repository.Rows = [Source("696-001", 10m, 1m)];
        ReportDataAndColumns<C15AssistiveDeviceDepositDetailReportViewModel> result =
            await service.ReportC15Async(Condition());

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(1, cache.FactoryCalls);
    }

    [Fact]
    public async Task ReportC15Async_CancelledRequestDoesNotQueryOrCache()
    {
        var repository = new FakeRepository();
        var cache = new CountingCache(new PassthroughReportTotalCountCache());
        ReportService service = CreateService(repository, cache);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.ReportC15Async(Condition(), source.Token));

        Assert.Equal(0, repository.QueryCalls);
        Assert.Equal(0, cache.Calls);
    }

    [Fact]
    public async Task ReportC15Async_ConcurrentRequestsKeepCanonicalRowsIsolated()
    {
        var repository = new FakeRepository
        {
            RowFactory = condition =>
            [
                new C15SourceRow("1150901", "1", "000001", 1m,
                    condition.StartDate!, condition.StartDate!, string.Empty, "696-001", 10m)
            ]
        };
        ReportService service = CreateService(repository, new PassthroughReportTotalCountCache());
        SearchReportCondition first = Condition();
        SearchReportCondition second = Condition();
        second.StartDate = "2026-09-02";

        var results = await Task.WhenAll(
            Task.Run(() => service.ReportC15Async(first)),
            Task.Run(() => service.ReportC15Async(second)));

        Assert.Equal("2026-09-01", Assert.Single(results[0].Data!).MedicalRecordNumber);
        Assert.Equal("2026-09-02", Assert.Single(results[1].Data!).MedicalRecordNumber);
    }

    private static ReportService CreateService(
        IC15AssistiveDeviceDepositDetailRepository repository,
        IReportTotalCountCache cache) => new(
            new FakeHealthCenterRepository(),
            new FakeReferralMemberRepository(),
            new FakeSafeNeedleRepository(),
            cache,
            NullLogger<ReportService>.Instance,
            c15Repository: repository,
            c15Reducer: new C15LegacyReducer());

    private static SearchReportCondition Condition(int pageNumber = 1, int pageSize = 10) => new()
    {
        ReportCode = "C15",
        StartDate = "2026-09-01",
        EndDate = "2026-09-16",
        PageNumber = pageNumber,
        PageSize = pageSize
    };

    private static C15SourceRow Source(string code, decimal amount, decimal encounter) =>
        new("1150901", "1", "000001", encounter, $"MR{encounter}", "Patient", "11509160000", code, amount);

    private sealed class FakeRepository : IC15AssistiveDeviceDepositDetailRepository
    {
        public IReadOnlyList<C15SourceRow> Rows { get; set; } = [];
        public Func<SearchReportCondition, IReadOnlyList<C15SourceRow>>? RowFactory { get; init; }
        public Exception? Failure { get; set; }
        public int QueryCalls { get; private set; }

        public IReadOnlyList<C15SourceRow> Query(
            SearchReportCondition condition,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            QueryCalls++;
            if (Failure is not null) throw Failure;
            return RowFactory?.Invoke(condition) ?? Rows;
        }
    }

    private sealed class CountingCache(IReportTotalCountCache inner) : IReportTotalCountCache
    {
        public int Calls { get; private set; }
        public int FactoryCalls { get; private set; }

        public int GetOrCreate(
            string reportCode,
            IReadOnlyDictionary<string, string?> normalizedFilters,
            Func<int> countFactory)
        {
            Calls++;
            return inner.GetOrCreate(reportCode, normalizedFilters, () =>
            {
                FactoryCalls++;
                return countFactory();
            });
        }

        public void Invalidate(string reportCode) => inner.Invalidate(reportCode);
    }
}
