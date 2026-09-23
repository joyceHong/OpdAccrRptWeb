using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C9ReportServiceTests
{
    [Fact]
    public async Task QueryAsync_QueriesEachDayAndMapsAmountsExactly()
    {
        var repository = new Repository();
        var service = new C9ReportService(repository, new NoCache());
        C9ReportResult result = await service.QueryAsync(
            new(new(2026, 2, 27), new(2026, 3, 1), 1, 30), "alice");

        Assert.Equal(["1150227", "1150228", "1150301"], repository.Dates);
        Assert.Equal(3, result.AllRows.Count);
        Assert.All(result.AllRows, row =>
        {
            Assert.Equal(string.Empty, row.PatientName);
            Assert.Equal(0m, row.DiscountAmount);
            Assert.Equal(-12.5m, row.PayableAmount);
            Assert.Equal(-12.5m, row.TotalAmount);
        });
    }

    [Theory]
    [InlineData(null, null, "0", "0", "0")]
    [InlineData("10.25", null, "10.25", "0", "10.25")]
    [InlineData("-3", "7.5", "-3", "7.5", "4.5")]
    public void Map_PreservesSpecificationExamples(string? sub3Text, string? sub5Text,
        string discountText, string payableText, string totalText)
    {
        decimal? sub3 = sub3Text is null ? null : decimal.Parse(sub3Text, System.Globalization.CultureInfo.InvariantCulture);
        decimal? sub5 = sub5Text is null ? null : decimal.Parse(sub5Text, System.Globalization.CultureInfo.InvariantCulture);
        C9ReportRow row = C9ReportService.Map(new(null, null, null, null, null, null,
            null, null, sub5, sub3));
        Assert.Equal(decimal.Parse(discountText, System.Globalization.CultureInfo.InvariantCulture), row.DiscountAmount);
        Assert.Equal(decimal.Parse(payableText, System.Globalization.CultureInfo.InvariantCulture), row.PayableAmount);
        Assert.Equal(decimal.Parse(totalText, System.Globalization.CultureInfo.InvariantCulture), row.TotalAmount);
    }

    [Fact]
    public async Task Cache_IsolatesActorAndQueryAndSupportsPaging()
    {
        var repository = new Repository();
        using var cache = new C9ReportResultCache();
        var service = new C9ReportService(repository, cache);
        var query = new C9ReportRequest(new(2026, 1, 1), new(2026, 1, 1));

        C9ReportResult alice = await service.QueryAsync(query, "alice");
        await service.QueryAsync(query with { PageNumber = 2 }, "alice");
        await service.QueryAsync(query, "bob");

        Assert.Equal(2, repository.Dates.Count);
        Assert.Equal(1, alice.Page.TotalCount);
        Assert.Empty((await service.QueryAsync(query with { PageNumber = 2 }, "alice")).Page.Data!);
    }

    [Fact]
    public async Task QueryAsync_CancellationStopsLaterDatesAndDoesNotCache()
    {
        var repository = new CancellingRepository();
        var cache = new RecordingCache();
        var service = new C9ReportService(repository, cache);
        await Assert.ThrowsAsync<OperationCanceledException>(() => service.QueryAsync(
            new(new(2026, 1, 1), new(2026, 1, 3)), "alice"));
        Assert.Equal(["1150101", "1150102"], repository.Dates);
        Assert.False(cache.WasSet);
    }

    [Fact]
    public async Task QueryAsync_RetriesOnlyBoundedTransientReadFailures()
    {
        var transient = new FailingRepository(new TimeoutException(), failures: 2);
        var policy = new TestPolicy();
        var service = new C9ReportService(transient, new NoCache(), policy);
        await service.QueryAsync(new(new(2026, 1, 1), new(2026, 1, 1)), "alice");
        Assert.Equal(3, transient.Calls);

        var permanent = new FailingRepository(new InvalidOperationException("mapping"), failures: 1);
        service = new C9ReportService(permanent, new NoCache(), policy);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.QueryAsync(new(new(2026, 1, 1), new(2026, 1, 1)), "alice"));
        Assert.Equal(1, permanent.Calls);
    }

    private sealed class Repository : IC9ReportRepository
    {
        public List<string> Dates { get; } = [];
        public Task<IReadOnlyList<C9SourceRow>> QueryDayAsync(string date, CancellationToken token = default)
        {
            Dates.Add(date);
            return Task.FromResult<IReadOnlyList<C9SourceRow>>([
                new(date, " M1 ", null, " D ", " S ", " U ", " A ", " Item ", -12.5m, null)
            ]);
        }
    }

    private sealed class CancellingRepository : IC9ReportRepository
    {
        public List<string> Dates { get; } = [];
        public Task<IReadOnlyList<C9SourceRow>> QueryDayAsync(string date, CancellationToken token = default)
        {
            Dates.Add(date);
            if (Dates.Count == 2) throw new OperationCanceledException(token);
            return Task.FromResult<IReadOnlyList<C9SourceRow>>([]);
        }
    }

    private sealed class NoCache : IC9ReportResultCache
    {
        public bool TryGet(string actor, C9ValidatedRequest request, out C9CachedResult result) { result = null!; return false; }
        public void Set(string actor, C9ValidatedRequest request, C9CachedResult result) { }
    }

    private sealed class RecordingCache : IC9ReportResultCache
    {
        public bool WasSet { get; private set; }
        public bool TryGet(string actor, C9ValidatedRequest request, out C9CachedResult result) { result = null!; return false; }
        public void Set(string actor, C9ValidatedRequest request, C9CachedResult result) => WasSet = true;
    }
    private sealed class FailingRepository(Exception exception, int failures) : IC9ReportRepository
    {
        public int Calls { get; private set; }
        public Task<IReadOnlyList<C9SourceRow>> QueryDayAsync(string date, CancellationToken token = default)
        {
            Calls++;
            if (Calls <= failures) throw exception;
            return Task.FromResult<IReadOnlyList<C9SourceRow>>([]);
        }
    }
    private sealed class TestPolicy : IC9TransientFailurePolicy
    {
        public int MaxAttempts => 3;
        public TimeSpan RetryDelay => TimeSpan.Zero;
        public bool IsTransient(Exception exception) => exception is TimeoutException;
    }
}
