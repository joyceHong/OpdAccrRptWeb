using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C4MaterialReportRequestTests
{
    [Fact]
    public void Validate_ConvertsGregorianDatesAndNormalizesPrefix()
    {
        C4ValidatedRequest actual = new C4MaterialReportRequest(
            "2026-09-01", "2026-09-03", " 0201 ").Validate();
        Assert.Equal("1150901", actual.RocStartDate);
        Assert.Equal("1150903", actual.RocEndDate);
        Assert.Equal("0201", actual.SectionPrefix);
        Assert.DoesNotContain(typeof(C4MaterialReportRequest).GetProperties(),
            property => property.Name.Contains("ReRun", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validate_RejectsReverseRange() => Assert.Throws<ArgumentException>(() =>
        new C4MaterialReportRequest("2026-09-03", "2026-09-01").Validate());
}

public sealed class C4MaterialReportRepositoryTests
{
    [Fact]
    public void DailySql_PreservesLegacyContract()
    {
        string sql = C4MaterialReportRepository.DailySql;
        Assert.Contains(":run_date", sql); Assert.Contains(":section_prefix", sql);
        Assert.Contains("O.chOp4Stat <> 'DC'", sql); Assert.Contains("('50', '99')", sql);
        Assert.Contains("RTRIM(G.chOrdDetal) IS NOT NULL", sql);
        Assert.Contains("ORDER BY\r\n    O.chOp4PSec,\r\n    G.chOrdInv", sql.Replace("\n", "\r\n"));
        Assert.DoesNotContain("MaterialUseTbl", sql); Assert.DoesNotContain("OpdBasicTbl", sql);
        Assert.DoesNotContain("Ipd", sql); Assert.DoesNotContain("NVL(O.chOp4Stat", sql);
        Assert.DoesNotContain("chOp4Proj", sql);
    }
}

public sealed class C4MaterialReportServiceTests
{
    [Fact]
    public async Task QueryAsync_ProcessesDaysInOrderAndTransformsEveryField()
    {
        var repository = new FakeC4Repository { Factory = day => day == "1150901"
            ? [Source("E", "1", " M1 ", "A", 2.5m, "0201"), Source("R", "2", " M2 ", "B", 3m, "X")]
            : [Source("R", "1", " M3 ", "C", 4m, "X")] };
        var mapping = new FakeC4OrganizationService();
        C4MaterialReportResult result = await Create(repository, mapping).QueryAsync(
            new("2026-09-01", "2026-09-02", PageSize: 10));
        Assert.Equal(["1150901", "1150902"], repository.Days);
        Assert.Equal([1, 2, 3], result.AllRows.Select(row => row.Id));
        Assert.Equal(["新品", "舊品", "新品"], result.AllRows.Select(row => row.Type));
        Assert.Equal("M1", result.AllRows[0].MaterialCode);
        Assert.Equal(2.5m, result.AllRows[0].Total);
        Assert.Equal("11910", result.AllRows[0].SectionCode);
        Assert.Equal(2, mapping.CallCount); // distinct (legacy, room)
    }

    [Fact]
    public async Task QueryAsync_RetriesTransientWithoutDuplicatingRows()
    {
        var repository = new FakeC4Repository { FailuresRemaining = 1,
            Factory = _ => [Source("R", "1", "M", "A", 1m, "X")] };
        C4MaterialReportResult result = await Create(repository, new FakeC4OrganizationService()).QueryAsync(
            new("2026-09-01", "2026-09-01"));
        Assert.Equal(2, repository.CallCount);
        Assert.Single(result.AllRows);
    }

    [Fact]
    public async Task QueryAsync_DoesNotRetryPermanentFailure()
    {
        var repository = new FakeC4Repository { PermanentFailure = true };
        await Assert.ThrowsAsync<InvalidOperationException>(() => Create(repository,
            new FakeC4OrganizationService()).QueryAsync(new("2026-09-01", "2026-09-01")));
        Assert.Equal(1, repository.CallCount);
    }

    [Fact]
    public async Task QueryAsync_ConcurrentRequestsRemainIsolated()
    {
        var repository = new FakeC4Repository { Factory = day =>
            [Source("R", "1", day, "A", 1m, "X")] };
        C4MaterialReportService service = Create(repository, new FakeC4OrganizationService());
        C4MaterialReportResult[] results = await Task.WhenAll(
            service.QueryAsync(new("2026-09-01", "2026-09-01")),
            service.QueryAsync(new("2026-09-02", "2026-09-02")));
        Assert.Equal("1150901", results[0].AllRows.Single().MaterialCode);
        Assert.Equal("1150902", results[1].AllRows.Single().MaterialCode);
    }

    [Fact]
    public async Task QueryAsync_PagesButKeepsFullTotal()
    {
        var repository = new FakeC4Repository { Factory = _ => Enumerable.Range(1, 12)
            .Select(i => Source("R", "1", $"M{i}", "A", i, "X")).ToArray() };
        C4MaterialReportResult result = await Create(repository, new FakeC4OrganizationService()).QueryAsync(
            new("2026-09-01", "2026-09-01", PageNumber: 2, PageSize: 10));
        Assert.Equal(12, result.Page.TotalCount); Assert.Equal(2, result.Page.Data!.Count);
    }

    private static C4MaterialReportService Create(IC4MaterialReportRepository repository,
        IOrganizationUnitCodeService mapping) => new(repository, mapping,
            new PassthroughReportTotalCountCache(), new FakeFailurePolicy(),
            new FixedTimeProvider(new DateTimeOffset(2026, 9, 17, 10, 30, 0, TimeSpan.FromHours(8))),
            NullLogger<C4MaterialReportService>.Instance);

    internal static C4MaterialSourceRow Source(string room, string detail, string material,
        string claim, decimal total, string legacy) => new(room, detail, material, claim,
            " Name ", " EA ", " CHG ", legacy, total);
}

public sealed class C4MaterialReportGoldenFixtureTests
{
    [Fact]
    public void Reducer_MatchesLegacyRowsFieldByField()
    {
        C4MaterialReportRow actual = C4MaterialReportService.ToReportRow(1,
            C4MaterialReportServiceTests.Source("R", "1", " M001 ", " NHI ", 12.75m, "0512"), "15012");
        Assert.Equal(new C4MaterialReportRow(1, "新品", "M001", "NHI", "Name", "EA",
            "CHG", 12.75m, "15012"), actual);
        // Integration gate: verify GenOrdBasicTbl dictionary, FLOAT(126) extremes,
        // >32,767 rows, and the same Oracle snapshot against VB6 before release.
    }
}

internal sealed class FakeC4Repository : IC4MaterialReportRepository
{
    public Func<string, IReadOnlyList<C4MaterialSourceRow>> Factory { get; init; } = _ => [];
    public int FailuresRemaining { get; set; }
    public bool PermanentFailure { get; init; }
    public int CallCount { get; private set; }
    public List<string> Days { get; } = [];
    public Task<IReadOnlyList<C4MaterialSourceRow>> QueryDayAsync(string runDate,
        string? sectionPrefix, CancellationToken cancellationToken = default)
    {
        CallCount++; Days.Add(runDate);
        if (PermanentFailure) throw new InvalidOperationException("permanent");
        if (FailuresRemaining-- > 0) throw new TimeoutException("transient");
        return Task.FromResult(Factory(runDate));
    }
}

internal sealed class FakeFailurePolicy : IC4TransientFailurePolicy
{
    public bool IsTransient(Exception exception) => exception is TimeoutException;
    public int MaxAttempts => 3;
}

internal sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => value.ToUniversalTime();
    public override TimeZoneInfo LocalTimeZone { get; } = TimeZoneInfo.CreateCustomTimeZone(
        "TestTaipei", TimeSpan.FromHours(8), "TestTaipei", "TestTaipei");
}

internal sealed class FakeC4OrganizationService : IOrganizationUnitCodeService
{
    public int CallCount { get; private set; }
    public Task<OrganizationUnitMapping?> ResolveLegacyCodeAsync(string newCode, bool activePlaceOnly,
        CancellationToken cancellationToken = default) => Task.FromResult<OrganizationUnitMapping?>(null);
    public Task<OrganizationUnitMapping?> ResolveNewCodeAsync(string legacyCode, string roomType,
        OrganizationUnitMappingScope scope, CancellationToken cancellationToken = default)
    {
        CallCount++;
        string code = roomType == "E" && legacyCode == "0201" ? "11910" : "15012";
        return Task.FromResult<OrganizationUnitMapping?>(new(OrganizationUnitSource.Section,
            legacyCode, code, string.Empty, true));
    }
    public Task<IReadOnlyList<OrganizationUnitMapping>> SearchAsync(string query, bool includeSections,
        bool includePlaces, bool activePlaceOnly, int limit = 20,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganizationUnitMapping>>([]);
}
