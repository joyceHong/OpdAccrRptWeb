using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C144DebtDetailReportServiceTests
{
    [Theory]
    [InlineData("OpdEr")]
    [InlineData("Inpatient")]
    public async Task QueryAsync_ConvertsGregorianDatesAndDispatchesSource(string source)
    {
        var repository = new FakeRepository();
        var service = new C144DebtDetailReportService(repository, new TrackingCache());

        await service.QueryAsync(Condition(source, 2, 30));

        Assert.Equal("1150901", repository.LastQuery!.StartDate);
        Assert.Equal("1150916", repository.LastQuery.EndDate);
        Assert.Equal(source, repository.LastQuery.Source);
        Assert.Equal(30, repository.LastOffset);
        Assert.Equal(30, repository.LastPageSize);
    }

    [Theory]
    [InlineData(null, "2026-09-01", "2026-09-16", 1, 10)]
    [InlineData("Other", "2026-09-01", "2026-09-16", 1, 10)]
    [InlineData("OpdEr", "bad", "2026-09-16", 1, 10)]
    [InlineData("OpdEr", "2026-09-17", "2026-09-16", 1, 10)]
    [InlineData("OpdEr", "2026-09-01", "2026-09-16", 0, 10)]
    [InlineData("OpdEr", "2026-09-01", "2026-09-16", 1, 20)]
    public async Task QueryAsync_RejectsInvalidContractWithoutRepositoryCall(
        string? source, string start, string end, int page, int size)
    {
        var repository = new FakeRepository();
        var service = new C144DebtDetailReportService(repository, new TrackingCache());

        await Assert.ThrowsAsync<ArgumentException>(() => service.QueryAsync(new SearchReportCondition
        {
            ReportCode = "C144", Source = source, StartDate = start, EndDate = end,
            PageNumber = page, PageSize = size
        }));

        Assert.Null(repository.LastQuery);
    }

    [Fact]
    public async Task QueryAsync_ReturnsExactColumnsAndCachesCountAcrossPageInputs()
    {
        var repository = new FakeRepository
        {
            Rows = [new() { OutstandingAmount = -5m, DrugCopaymentAmount = null }]
        };
        var cache = new TrackingCache();
        var service = new C144DebtDetailReportService(repository, cache);

        ReportDataAndColumns<C144DebtDetailReportViewModel> first =
            await service.QueryAsync(Condition("OpdEr", 1, 10));
        await service.QueryAsync(Condition("OpdEr", 2, 30));

        Assert.Equal(31, first.Columns!.Count);
        Assert.Equal("診別", first.Columns[0].Label);
        Assert.Equal("健保身分掛號費", first.Columns[^1].Label);
        Assert.Equal(-5m, first.Data![0].OutstandingAmount);
        Assert.Null(first.Data[0].DrugCopaymentAmount);
        Assert.Equal(1, repository.CountCalls);
        Assert.DoesNotContain(cache.Keys, key => key.Contains("Page", StringComparison.Ordinal));
        Assert.Contains(cache.Keys, key => key.Contains("Source=OpdEr", StringComparison.Ordinal));
    }

    private static SearchReportCondition Condition(string source, int page, int size) => new()
    {
        ReportCode = "C144", StartDate = "2026-09-01", EndDate = "2026-09-16",
        Source = source, PageNumber = page, PageSize = size
    };

    private sealed class FakeRepository : IC144DebtDetailReportRepository
    {
        public C144Query? LastQuery { get; private set; }
        public int LastOffset { get; private set; }
        public int LastPageSize { get; private set; }
        public int CountCalls { get; private set; }
        public List<C144DebtDetailReportViewModel> Rows { get; init; } = [];
        public int GetCount(C144Query query, CancellationToken cancellationToken = default)
        { LastQuery = query; CountCalls++; return Rows.Count; }
        public List<C144DebtDetailReportViewModel> GetPage(C144Query query, int offset, int pageSize,
            CancellationToken cancellationToken = default)
        { LastQuery = query; LastOffset = offset; LastPageSize = pageSize; return Rows.Skip(offset).Take(pageSize).ToList(); }
        public List<C144DebtDetailReportViewModel> GetAll(C144Query query,
            CancellationToken cancellationToken = default)
        { LastQuery = query; return Rows; }
    }

    private sealed class TrackingCache : IReportTotalCountCache
    {
        private readonly Dictionary<string, int> _values = [];
        public List<string> Keys { get; } = [];
        public int GetOrCreate(string reportCode, IReadOnlyDictionary<string, string?> filters, Func<int> factory)
        {
            string key = reportCode + "|" + string.Join("|", filters.OrderBy(x => x.Key)
                .Select(x => $"{x.Key}={x.Value}"));
            Keys.Add(key);
            if (_values.TryGetValue(key, out int value)) return value;
            value = factory();
            _values[key] = value;
            return value;
        }
        public void Invalidate(string reportCode) { }
    }
}
