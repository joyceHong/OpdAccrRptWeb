using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class C5ReportServiceTests
{
    [Fact]
    public async Task QueryAsync_QueriesEveryDay_AndSummaryDailyUseAggregate()
    {
        var repository = new FakeRepository();
        C5ReportService service = Create(repository);
        await service.QueryAsync(new("2026-09-18", "2026-09-20", DetailType: C5DetailType.Daily));

        Assert.Equal(new[] { "1150918", "1150919", "1150920" }, repository.Dates);
        Assert.All(repository.Ids, id => Assert.Equal(C5QueryId.OpdDrugAggregate, id));
        Assert.Equal(1, repository.OpenCount);
        Assert.Equal(1, repository.DisposeCount);
        Assert.Equal(1, repository.MaximumConcurrentQueries);
    }

    [Fact]
    public async Task QueryAsync_366Days_UsesOneSessionAndSequentialDailyCommands()
    {
        var repository = new FakeRepository();
        C5ReportService service = Create(repository);

        await service.QueryAsync(new("2008-09-18", "2009-09-18"));

        Assert.Equal(366, repository.Dates.Count);
        Assert.Equal("0970918", repository.Dates[0]);
        Assert.Equal("0980918", repository.Dates[^1]);
        Assert.Equal(1, repository.OpenCount);
        Assert.Equal(1, repository.DisposeCount);
        Assert.Equal(1, repository.MaximumConcurrentQueries);
    }

    [Fact]
    public async Task QueryAsync_Selects0430Detail_AndDoesNotSwallowFailures()
    {
        var repository = new FakeRepository { Failure = new InvalidOperationException("db") };
        C5ReportService service = Create(repository);
        var request = new C5ReportRequest("2026-09-18", "2026-09-18",
            DetailType: C5DetailType.PatientDetail, ChargeKind: C5ChargeKind.Order,
            LegacySectionCode: "0430");

        InvalidOperationException error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.QueryAsync(request));
        Assert.Equal("db", error.Message);
        Assert.Equal(C5QueryId.OpdOrder0430Detail, repository.Ids.Single());
    }

    [Fact]
    public async Task QueryAsync_MemoizesOutputMappingByNormalizedCodeAndRoomType()
    {
        var repository = new FakeRepository
        {
            RowsFactory = _ =>
            [
                Row("R", " 0201 "),
                Row("R", "0201"),
                Row("E", "0201")
            ]
        };
        var organizations = new FakeOrganizationService();
        C5ReportService service = Create(repository, organizations);

        var result = await service.QueryAsync(new("2026-09-18", "2026-09-19"));

        Assert.Equal(6, result.AllRows.Count);
        Assert.Equal(2, organizations.NewCodeCalls.Count);
        Assert.Contains(("0201", "R"), organizations.NewCodeCalls);
        Assert.Contains(("0201", "E"), organizations.NewCodeCalls);
    }

    [Fact]
    public async Task QueryAsync_DoesNotHideMappingFailures()
    {
        var repository = new FakeRepository { RowsFactory = _ => [Row("R", "AMB")] };
        var organizations = new FakeOrganizationService { NewCodeFailure = new OrganizationUnitLegacyMappingAmbiguousException("AMB") };

        await Assert.ThrowsAsync<OrganizationUnitLegacyMappingAmbiguousException>(() =>
            Create(repository, organizations).QueryAsync(new("2026-09-18", "2026-09-18")));
    }

    [Fact]
    public async Task QueryAsync_SummaryPageTwoUsesCompletedResultCache()
    {
        var repository = new FakeRepository
        {
            RowsFactory = _ => Enumerable.Range(1, 12).Select(index =>
                Row("R", "0201") with { ChargeCode = $"D{index:00}" }).ToArray()
        };
        var organizations = new FakeOrganizationService();
        using var cache = new C5ReportResultCache();
        C5ReportService service = Create(repository, organizations, cache);
        var request = new C5ReportRequest("2026-09-18", "2026-09-18",
            DetailType: C5DetailType.Summary, NewOrganizationUnitCode: "11910",
            PageNumber: 1, PageSize: 10);

        var first = await service.QueryAsync(request);
        var second = await service.QueryAsync(request with { PageNumber = 2 });
        var resized = await service.QueryAsync(request with { PageSize = 30 });

        Assert.Equal(12, first.Page.TotalCount);
        Assert.Equal(12, second.Page.TotalCount);
        Assert.NotNull(second.Page.Data);
        Assert.NotNull(resized.Page.Data);
        Assert.Equal(2, second.Page.Data!.Count);
        Assert.Equal(12, resized.Page.Data!.Count);
        Assert.Equal(1, repository.OpenCount);
        Assert.Single(repository.Dates);
        Assert.Single(organizations.LegacyCodeCalls);
        Assert.Single(organizations.NewCodeCalls);
    }

    [Fact]
    public async Task QueryAsync_CacheKeySeparatesResultChangingFilters()
    {
        var repository = new FakeRepository();
        using var cache = new C5ReportResultCache();
        C5ReportService service = Create(repository, cache: cache);

        await service.QueryAsync(new("2026-09-18", "2026-09-18", ChargeCode: "A"));
        await service.QueryAsync(new("2026-09-18", "2026-09-18", ChargeCode: "B"));

        Assert.Equal(2, repository.OpenCount);
    }

    [Fact]
    public async Task QueryAsync_PatientDetailNeverUsesCrossRequestCache()
    {
        var repository = new FakeRepository();
        using var cache = new C5ReportResultCache();
        C5ReportService service = Create(repository, cache: cache);
        var request = new C5ReportRequest("2026-09-18", "2026-09-18",
            DetailType: C5DetailType.PatientDetail);

        await service.QueryAsync(request);
        await service.QueryAsync(request with { PageNumber = 2 });

        Assert.Equal(2, repository.OpenCount);
        Assert.Equal(2, repository.Dates.Count);
    }

    [Fact]
    public async Task QueryAsync_FailureDoesNotPopulateResultCache()
    {
        var repository = new FakeRepository { FailuresRemaining = 1 };
        using var cache = new C5ReportResultCache();
        C5ReportService service = Create(repository, cache: cache);
        var request = new C5ReportRequest("2026-09-18", "2026-09-18");

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.QueryAsync(request));
        await service.QueryAsync(request);

        Assert.Equal(2, repository.OpenCount);
        Assert.Equal(2, repository.Dates.Count);
    }

    [Fact]
    public async Task QueryAsync_CancellationDoesNotPopulateResultCache()
    {
        var repository = new FakeRepository();
        using var cache = new C5ReportResultCache();
        C5ReportService service = Create(repository, cache: cache);
        var request = new C5ReportRequest("2026-09-18", "2026-09-18");

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.QueryAsync(request, new CancellationToken(canceled: true)));
        await service.QueryAsync(request);

        Assert.Equal(2, repository.OpenCount);
        Assert.Single(repository.Dates);
        Assert.Equal(2, repository.DisposeCount);
    }

    [Fact]
    public async Task QueryAsync_LogsPrivacySafePerformanceForMissAndHit()
    {
        var repository = new FakeRepository { RowsFactory = _ => [Row("R", "0201")] };
        var organizations = new FakeOrganizationService();
        using var cache = new C5ReportResultCache();
        var logger = new CapturingLogger<C5ReportService>();
        C5ReportService service = Create(repository, organizations, cache, logger);
        var request = new C5ReportRequest("2026-09-18", "2026-09-19", ChargeCode: "SECRET");

        await service.QueryAsync(request);
        await service.QueryAsync(request with { PageNumber = 2 });

        Assert.Equal(2, logger.Entries.Count);
        string[] allowed = ["CacheHit", "DaysQueried", "DailyQueryCount", "MappingLookupCount",
            "ReturnedRows", "DatabaseElapsedMs", "MappingElapsedMs", "TotalElapsedMs"];
        Assert.All(logger.Entries, entry =>
        {
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Null(entry.Exception);
            Assert.Equal(allowed.Order(), entry.Properties.Keys.Order());
            Assert.DoesNotContain("SECRET", entry.Message, StringComparison.Ordinal);
            Assert.True(Convert.ToInt64(entry.Properties["DatabaseElapsedMs"]) >= 0);
            Assert.True(Convert.ToInt64(entry.Properties["MappingElapsedMs"]) >= 0);
            Assert.True(Convert.ToInt64(entry.Properties["TotalElapsedMs"]) >= 0);
        });
        Assert.False((bool)logger.Entries[0].Properties["CacheHit"]!);
        Assert.Equal(2, logger.Entries[0].Properties["DailyQueryCount"]);
        Assert.Equal(1, logger.Entries[0].Properties["MappingLookupCount"]);
        Assert.True((bool)logger.Entries[1].Properties["CacheHit"]!);
        Assert.Equal(0, logger.Entries[1].Properties["DailyQueryCount"]);
        Assert.Equal(0, logger.Entries[1].Properties["MappingLookupCount"]);
    }

    private static C5ReportService Create(IC5ReportRepository repository,
        IOrganizationUnitCodeService? organizations = null, IC5ReportResultCache? cache = null,
        ILogger<C5ReportService>? logger = null) =>
        new(repository, organizations ?? new FakeOrganizationService(),
            cache ?? new C5ReportResultCache(), logger ?? NullLogger<C5ReportService>.Instance);

    private static C5SourceRow Row(string roomType, string section) => new(roomType, section, "D1",
        "Drug", "1150918", null, null, null, null, "1", 1, 1, 1, 1, 1, null);

    private sealed class FakeRepository : IC5ReportRepository, IC5ReportQuerySession
    {
        public List<string> Dates { get; } = [];
        public List<C5QueryId> Ids { get; } = [];
        public Exception? Failure { get; init; }
        public int FailuresRemaining { get; set; }
        public Func<string, IReadOnlyList<C5SourceRow>> RowsFactory { get; init; } = _ => [];
        public int OpenCount { get; private set; }
        public int DisposeCount { get; private set; }
        public int MaximumConcurrentQueries { get; private set; }
        private int _activeQueries;
        public Task<IC5ReportQuerySession> OpenSessionAsync(CancellationToken cancellationToken = default)
        {
            OpenCount++;
            return Task.FromResult<IC5ReportQuerySession>(this);
        }
        public async Task<IReadOnlyList<C5SourceRow>> QueryDayAsync(C5ValidatedRequest request,
            string runDate, C5QueryId queryId, CancellationToken cancellationToken = default)
        {
            int active = Interlocked.Increment(ref _activeQueries);
            MaximumConcurrentQueries = Math.Max(MaximumConcurrentQueries, active);
            try
            {
                await Task.Yield();
                Dates.Add(runDate); Ids.Add(queryId);
                if (Failure is not null) throw Failure;
                if (FailuresRemaining-- > 0) throw new InvalidOperationException("transient");
                return RowsFactory(runDate);
            }
            finally { Interlocked.Decrement(ref _activeQueries); }
        }
        public ValueTask DisposeAsync() { DisposeCount++; return ValueTask.CompletedTask; }
    }

    private sealed class FakeOrganizationService : IOrganizationUnitCodeService
    {
        public List<string> LegacyCodeCalls { get; } = [];
        public List<(string LegacyCode, string RoomType)> NewCodeCalls { get; } = [];
        public Exception? NewCodeFailure { get; init; }
        public Task<OrganizationUnitMapping?> ResolveLegacyCodeAsync(string newCode, bool activePlaceOnly,
            CancellationToken cancellationToken = default)
        {
            LegacyCodeCalls.Add(newCode);
            return Task.FromResult<OrganizationUnitMapping?>(
                new(OrganizationUnitSource.Section, "0430", newCode, "科別", true));
        }
        public Task<OrganizationUnitMapping?> ResolveNewCodeAsync(string legacyCode, string roomType,
            OrganizationUnitMappingScope scope, CancellationToken cancellationToken = default)
        {
            NewCodeCalls.Add((legacyCode.Trim().ToUpperInvariant(), roomType.Trim().ToUpperInvariant()));
            if (NewCodeFailure is not null) throw NewCodeFailure;
            return Task.FromResult<OrganizationUnitMapping?>(new(OrganizationUnitSource.Section,
                legacyCode, "N" + legacyCode.Trim(), "科別", true));
        }
        public Task<IReadOnlyList<OrganizationUnitMapping>> SearchAsync(string query, bool includeSections,
            bool includePlaces, bool activePlaceOnly, int limit = 20, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationUnitMapping>>([]);
    }
}
