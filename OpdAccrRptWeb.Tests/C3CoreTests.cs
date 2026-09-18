using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Tests;

public sealed class DepartmentFilterResolverTests
{
    [Theory]
    [InlineData("0575", DepartmentFilterMode.Station)]
    [InlineData("1OR27", DepartmentFilterMode.Station)]
    [InlineData("15137", DepartmentFilterMode.Sys56)]
    [InlineData("15WD1", DepartmentFilterMode.Sys53Wound)]
    [InlineData("12710", DepartmentFilterMode.Dispensing)]
    [InlineData("0512", DepartmentFilterMode.GeneralLocation)]
    [InlineData("0511", DepartmentFilterMode.Er)]
    [InlineData("0410", DepartmentFilterMode.Pharmacy)]
    public async Task ResolveAsync_MapsFixedCodes(string code, DepartmentFilterMode expected) =>
        Assert.Equal(expected, await new DepartmentFilterResolver(new FakeMappings()).ResolveAsync(code));

    [Fact]
    public void NormalizeDepartmentCode_TrimsAndUppercasesLegacyCode() =>
        Assert.Equal("ABC01", DepartmentFilterResolver.NormalizeDepartmentCode(" abc01 "));

    [Fact]
    public async Task ResolveAsync_UsesLocationAndRejectsUnknown()
    {
        var resolver = new DepartmentFilterResolver(new FakeMappings { ValidLocation = "CUSTOM" });
        Assert.Equal(DepartmentFilterMode.GeneralLocation, await resolver.ResolveAsync(" custom "));
        await Assert.ThrowsAsync<ArgumentException>(() => resolver.ResolveAsync("missing"));
    }
}

public sealed class C3ReportRepositoryTests
{
    [Fact]
    public void GetSql_Hd3EmitsOnlySelectedPredicateInBothMovementBranches()
    {
        string sql = C3Sql.Get(CareSource.O, DepartmentFilterMode.Hd3);
        const string hd3 = "((C.chOp1Sec='0212*' AND C.chOp1Room LIKE '3J%') OR B.chOp4Sys='43')";

        Assert.Equal(2, CountOccurrences(sql, hd3));
        Assert.DoesNotContain(":department_filter_mode", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("B.chOp4Sys='36'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("GENERAL_LOCATION", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void GetSql_NoneAddsNoDepartmentPredicate()
    {
        string sql = C3Sql.Get(CareSource.O, DepartmentFilterMode.None);

        Assert.DoesNotContain(":department_filter_mode", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("B.chOp4Sys='43'", sql, StringComparison.Ordinal);
        Assert.Contains("B.chOp4IDate BETWEEN :day_begin AND :day_end", sql, StringComparison.Ordinal);
        Assert.Contains("B.chOp4DCDate BETWEEN :day_begin AND :day_end", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void GetSql_StationKeepsDepartmentCodeAsBindValue()
    {
        const string userDepartment = "UNTRUSTED_DEPARTMENT";
        string sql = C3Sql.Get(CareSource.O, DepartmentFilterMode.Station);
        C3ValidatedRequest request = new C3ReportRequest("2026-05-14", "2026-05-14", CareSource.O,
            DepartmentCode: userDepartment).Validate();
        using var command = new OracleCommand(sql);

        C3ReportRepository.AddParameters(command, request, "1150514", DepartmentFilterMode.Station);

        Assert.Equal(2, CountOccurrences(sql, "B.chStation=:department_code"));
        Assert.DoesNotContain(userDepartment, sql, StringComparison.Ordinal);
        Assert.Equal(userDepartment, command.Parameters["department_code"].Value);
    }

    [Fact]
    public void Sql_UsesSeparateTablesMovementAndFixedBindSlots()
    {
        Assert.Contains("OpdOrdTbl", C3Sql.Opd); Assert.DoesNotContain("IpdOrdTbl", C3Sql.Opd);
        Assert.Contains("IpdOrdTbl", C3Sql.Ipd); Assert.Contains("UNION ALL", C3Sql.Opd);
        Assert.Contains("M.movement_type", C3Sql.Opd); Assert.Contains(":room_50", C3Sql.Opd);
        Assert.Contains(":charge_50", C3Sql.Ipd);
        Assert.Contains("LNNVL", C3Sql.Get(CareSource.O, DepartmentFilterMode.GeneralLocation));
        Assert.Contains("RTRIM(M.chOp1Room) IN", C3Sql.Opd);
        Assert.Contains("RTRIM(M.chOp4OrdNo) IN", C3Sql.Opd);
        Assert.Equal(2, CountOccurrences(C3Sql.Ipd, "RTRIM(B.chStation)=:department_code"));
        Assert.Contains("B.chOp4IDate BETWEEN :day_begin AND :day_end", C3Sql.Opd);
        Assert.Contains("B.chOp4DCDate BETWEEN :day_begin AND :day_end", C3Sql.Ipd);
        Assert.DoesNotContain("SUBSTR(B.chOp4IDate", C3Sql.Opd, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SUBSTR(B.chOp4DCDate", C3Sql.Opd, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4, CountOccurrences(C3Sql.Get(CareSource.O, DepartmentFilterMode.GeneralLocation),
            "SELECT chSecNo FROM GenSectionTbl WHERE RTRIM(chLocation)=:department_code"));
    }

    private static int CountOccurrences(string value, string search)
    {
        int count = 0;
        for (int index = 0; (index = value.IndexOf(search, index, StringComparison.Ordinal)) >= 0;
             index += search.Length) count++;
        return count;
    }

    [Fact]
    public void AddParameters_BindsCodesAndNullSlotsWithoutChangingSql()
    {
        C3ValidatedRequest request = new C3ReportRequest("2026-05-14", "2026-05-14", CareSource.O,
            RoomCodes: ["A", "B"], ChargeCodes: ["X", "Y", "Z"]).Validate();
        using var command = new OracleCommand(C3Sql.Opd);
        C3ReportRepository.AddParameters(command, request, "1150514", DepartmentFilterMode.None);
        Assert.True(command.BindByName == false || !command.CommandText.Contains("A", StringComparison.Ordinal));
        Assert.Equal("A", command.Parameters["room_01"].Value);
        Assert.Equal(DBNull.Value, command.Parameters["room_03"].Value);
        Assert.Equal("Z", command.Parameters["charge_03"].Value);
        Assert.Equal("11505140000", command.Parameters["day_begin"].Value);
        Assert.Equal("11505149999", command.Parameters["day_end"].Value);
        Assert.Equal(107, command.Parameters.Count);
        Assert.False(command.Parameters.Contains("department_code"));
    }
}

public sealed class DepartmentAssignmentServiceTests
{
    [Fact]
    public async Task AssignAsync_StationWinsAndUnknownInpatientFallsBack()
    {
        var mappings = new FakeMappings { Translation = new("NEW", "Station Name") };
        var service = new DepartmentAssignmentService(mappings);
        DepartmentAssignment opd = await service.AssignAsync(CareSource.O, Row(station: "S1", system: "56"));
        Assert.Equal(new DepartmentAssignment("門診", "Station Name", "NEW"), opd);
        DepartmentAssignment ipd = await service.AssignAsync(CareSource.I, Row(station: "UNKNOWN"));
        Assert.Equal(new DepartmentAssignment("住院", "全院", "19999"), ipd);
    }

    [Theory]
    [InlineData("4F85", "", "", "美容中心", "12120")]
    [InlineData("4F8", "", "", "", "")]
    [InlineData("5D103", "", "", "傷造口科", "15WD1")]
    public async Task AssignAsync_PreservesSpecialOrder(string room, string section, string system,
        string name, string code)
    {
        var service = new DepartmentAssignmentService(new FakeMappings());
        DepartmentAssignment actual = await service.AssignAsync(CareSource.O, Row(room, section: section, system: system));
        Assert.Equal(name, actual.Dispensary); Assert.Equal(code, actual.Section);
    }

    private static C3MovementRow Row(string room = "A1", string station = "", string section = "",
        string system = "", string runDate = "1150514", string charge = "X") =>
        new(1, runDate, room, station, charge, section, system, null, null, null, null, null,
            "name", "material", "0", null, null, 1);
}

public sealed class SectionMappingRepositoryTests
{
    [Theory]
    [InlineData("15HD3", "15HD3", "血液透析室3樓")]
    [InlineData("1OR27", "1OR27", "三樓手術室_27")]
    [InlineData("1XA04", "1XA04", "影像醫學科-放射組04")]
    [InlineData("1ED20", "1ED20", "1ED20")]
    [InlineData("15WD1", "15WD1", "傷造口科")]
    public void Constants_ContainCompleteBoundaryMappings(string oldCode, string newCode, string name)
    {
        SectionMapping actual = SectionMappingRepository.Constants[oldCode];
        Assert.Equal(newCode, actual.NewCode); Assert.Equal(name, actual.DisplayName);
    }
}

public sealed class OrganizationUnitMappingRepositoryTests
{
    [Fact]
    public void FindByNewCodeSql_CombinesBothTablesAndAppliesActivePlacePolicy()
    {
        string sql = OrganizationUnitMappingRepository.FindByNewCodeSql;

        Assert.Contains("GenSectionTbl", sql);
        Assert.Contains("GenPlaceTbl", sql);
        Assert.Contains("RTRIM(chNewSecNo)", sql);
        Assert.Contains("RTRIM(chNewPlaNo)", sql);
        Assert.Contains("chPlaWork = '1'", sql);
        Assert.Contains(":new_code", sql);
        Assert.Contains(":active_place_only", sql);
    }

    [Fact]
    public void LegacyAndSearchSql_AreBoundAndCoverSectionAndPlace()
    {
        Assert.Contains(":legacy_code", OrganizationUnitMappingRepository.FindByLegacyCodeSql);
        Assert.Contains(":include_places", OrganizationUnitMappingRepository.FindByLegacyCodeSql);
        Assert.Contains("GenSectionTbl", OrganizationUnitMappingRepository.SearchSql);
        Assert.Contains("GenPlaceTbl", OrganizationUnitMappingRepository.SearchSql);
        Assert.Contains("chPlaWork = '1'", OrganizationUnitMappingRepository.SearchSql);
        Assert.Equal(@"A\%B\_C\\D", OrganizationUnitMappingRepository.EscapeLike(@"A%B_C\D"));
    }
}

public sealed class OrganizationUnitCodeServiceTests
{
    [Fact]
    public async Task ResolveLegacyCodeAsync_NormalizesInputAndReturnsMappingShape()
    {
        var repository = new FakeOrganizationUnitMappings
        {
            Results = [new(OrganizationUnitSource.Place, "0512", "15012", "門診護理站", true)]
        };

        OrganizationUnitMapping? actual = await new OrganizationUnitCodeService(repository)
            .ResolveLegacyCodeAsync(" 15012 ", activePlaceOnly: true);

        Assert.Equal("15012", repository.RequestedCode);
        Assert.True(repository.ActivePlaceOnly);
        Assert.Equal(new OrganizationUnitMapping(
            OrganizationUnitSource.Place, "0512", "15012", "門診護理站", true), actual);
    }

    [Fact]
    public async Task ResolveLegacyCodeAsync_ReturnsFixedMapping()
    {
        OrganizationUnitMapping? actual = await new OrganizationUnitCodeService(
            new FakeOrganizationUnitMappings()).ResolveLegacyCodeAsync("15hd3", activePlaceOnly: true);

        Assert.Equal(new OrganizationUnitMapping(
            OrganizationUnitSource.Fixed, "15HD3", "15HD3", "血液透析室3樓", true), actual);
    }

    [Fact]
    public async Task ResolveLegacyCodeAsync_AcceptsDuplicateSourcesWithSameLegacyCode()
    {
        var repository = new FakeOrganizationUnitMappings
        {
            Results =
            [
                new(OrganizationUnitSource.Section, "0512", "15012", "門診護理站", true),
                new(OrganizationUnitSource.Place, "0512", "15012", "門診護理站", true)
            ]
        };

        OrganizationUnitMapping? actual = await new OrganizationUnitCodeService(repository)
            .ResolveLegacyCodeAsync("15012", activePlaceOnly: true);

        Assert.Equal("0512", actual?.LegacyCode);
    }

    [Fact]
    public async Task ResolveLegacyCodeAsync_ReturnsNullWhenMappingDoesNotExist()
    {
        OrganizationUnitMapping? actual = await new OrganizationUnitCodeService(
            new FakeOrganizationUnitMappings()).ResolveLegacyCodeAsync("MISSING", activePlaceOnly: true);

        Assert.Null(actual);
    }

    [Fact]
    public async Task ResolveLegacyCodeAsync_RejectsDifferentLegacyCodes()
    {
        var repository = new FakeOrganizationUnitMappings
        {
            Results =
            [
                new(OrganizationUnitSource.Section, "OLD01", "NEW01", "Section", true),
                new(OrganizationUnitSource.Place, "OLD02", "NEW01", "Place", true)
            ]
        };

        await Assert.ThrowsAsync<OrganizationUnitMappingAmbiguousException>(() =>
            new OrganizationUnitCodeService(repository).ResolveLegacyCodeAsync("NEW01", activePlaceOnly: true));
    }

    [Theory]
    [InlineData("0201", "11910")]
    [InlineData("0281", "11920")]
    [InlineData("0220", "11930")]
    [InlineData("0221", "11930")]
    [InlineData("0230", "11309")]
    public async Task ResolveNewCodeAsync_AppliesEmergencyMappings(string oldCode, string newCode)
    {
        OrganizationUnitMapping? actual = await new OrganizationUnitCodeService(
            new FakeOrganizationUnitMappings()).ResolveNewCodeAsync(oldCode, "E",
                OrganizationUnitMappingScope.SectionAndPlace);
        Assert.Equal(newCode, actual?.NewCode);
    }

    [Fact]
    public async Task ResolveNewCodeAsync_RejectsDistinctNewCodes()
    {
        var repository = new FakeOrganizationUnitMappings
        {
            LegacyResults = [
                new(OrganizationUnitSource.Section, "A001", "N001", "S", true),
                new(OrganizationUnitSource.Place, "A001", "N002", "P", false)]
        };
        await Assert.ThrowsAsync<OrganizationUnitLegacyMappingAmbiguousException>(() =>
            new OrganizationUnitCodeService(repository).ResolveNewCodeAsync("a001", "R",
                OrganizationUnitMappingScope.SectionAndPlace));
    }

    [Fact]
    public async Task SearchAsync_PrefersSectionForDuplicateLegacyCode()
    {
        var repository = new FakeOrganizationUnitMappings
        {
            SearchResults = [
                new(OrganizationUnitSource.Place, "A001", "P001", "Place", true),
                new(OrganizationUnitSource.Section, "A001", "S001", "Section", true)]
        };
        IReadOnlyList<OrganizationUnitMapping> actual = await new OrganizationUnitCodeService(repository)
            .SearchAsync("a", true, true, true);
        Assert.Single(actual);
        Assert.Equal(OrganizationUnitSource.Section, actual[0].Source);
    }
}

public sealed class C3ReportServiceTests
{
    [Fact]
    public async Task QueryAsync_SummaryAggregatesFinalDisplayKeysAcrossDatesAndMovements()
    {
        var repository = new FakeC3Repository
        {
            RowsFactory = runDate =>
            [
                Movement(runDate, movementType: 1, quantity: 5, room: "A1"),
                Movement(runDate, movementType: 2, quantity: runDate == "1150514" ? -2 : 3, room: "A2"),
                Movement(runDate, movementType: 1, quantity: 4, charge: "OTHER", room: "A3")
            ]
        };
        var mappings = new FakeMappings { Translation = new("15012", "門診護理站") };

        C3ReportResult result = await Create(repository, mappings).QueryAsync(new(
            "2026-05-14", "2026-05-15", CareSource.O, ReportDetailType.Summary, PageSize: 10));

        Assert.Equal(2, result.AllRows.Count);
        Assert.Equal(11, result.AllRows.Single(row => row.ChargeCode == "C1").TotalSum);
        Assert.Equal(8, result.AllRows.Single(row => row.ChargeCode == "OTHER").TotalSum);
        Assert.Equal(2, result.Page.TotalCount);
        Assert.Equal(2, result.Page.Data?.Count);
    }

    [Fact]
    public async Task QueryAsync_DetailKeepsEquivalentMovementRowsUnaggregated()
    {
        var repository = new FakeC3Repository
        {
            RowsFactory = runDate =>
            [
                Movement(runDate, movementType: 1, quantity: 5, room: "A1"),
                Movement(runDate, movementType: 2, quantity: -2, room: "A2")
            ]
        };
        var mappings = new FakeMappings { Translation = new("15012", "門診護理站") };

        C3ReportResult result = await Create(repository, mappings).QueryAsync(new(
            "2026-05-14", "2026-05-15", CareSource.O, ReportDetailType.Detail, PageSize: 10));

        Assert.Equal(4, result.AllRows.Count);
        Assert.Equal(["1150514", "1150514", "1150515", "1150515"],
            result.AllRows.Select(row => row.Op1Date));
        Assert.Equal(4, result.Page.TotalCount);
    }

    [Fact]
    public async Task QueryAsync_QueriesEachDayPagesAndMapsDetail()
    {
        var repository = new FakeC3Repository();
        var mappings = new FakeMappings { Translation = new("15012", "門診護理站") };
        var service = Create(repository, mappings);
        C3ReportResult result = await service.QueryAsync(new C3ReportRequest("2026-05-14", "2026-05-15",
            CareSource.O, ReportDetailType.Detail, PageSize: 10));
        Assert.Equal(["1150514", "1150515"], repository.Dates);
        Assert.Equal(28, result.Page.TotalCount); Assert.NotNull(result.Page.Data);
        Assert.Equal(10, result.Page.Data.Count);
        Assert.Equal("王小明(轉住)", result.Page.Data[0].PName);
        Assert.Equal("AK庫", result.Page.Data[0].InventoryType);
    }

    [Fact]
    public void Title_ComposesExactLogisticsDetailTitle()
    {
        C3ValidatedRequest request = new C3ReportRequest("2026-05-14", "2026-05-14", CareSource.I,
            ReportDetailType.Detail, LogisticsType.Logistics).Validate();
        Assert.Equal("亞東紀念醫院住院各護理站計價品明細表__物流", C3ReportService.Title(request));
    }

    [Fact]
    public async Task QueryAsync_CacheIdentityIgnoresPageButSeparatesFilters()
    {
        var repository = new FakeC3Repository();
        var mappings = new FakeMappings { Translation = new("15012", "門診護理站") };
        var cache = new CountingCache();
        var service = new C3ReportService(repository, new(mappings), new FakeOrganizationUnitCodeService(),
            new(mappings), cache,
            NullLogger<C3ReportService>.Instance);
        await service.QueryAsync(new("2026-05-14", "2026-05-14", CareSource.O, PageNumber: 1));
        await service.QueryAsync(new("2026-05-14", "2026-05-14", CareSource.O, PageNumber: 2));
        await service.QueryAsync(new("2026-05-14", "2026-05-14", CareSource.O,
            LogisticsType: LogisticsType.Logistics));
        Assert.Equal(2, cache.Keys.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task QueryAsync_NormalizesDisplayedDepartmentCodeBeforeRepository()
    {
        var repository = new FakeC3Repository();

        await Create(repository, new FakeMappings(), new FakeOrganizationUnitCodeService
        {
            Mapping = new(OrganizationUnitSource.Place, "0512", "15012", "門診護理站", true)
        }).QueryAsync(new(
            "2026-05-14", "2026-05-14", CareSource.O, DepartmentCode: "15012"));

        Assert.Equal("0512", repository.DepartmentCodes.Single());
        Assert.Equal(DepartmentFilterMode.GeneralLocation, repository.DepartmentModes.Single());
    }

    [Fact]
    public async Task QueryAsync_UsesResolvedLegacyCodeForInpatientRepository()
    {
        var repository = new FakeC3Repository();
        var codeService = new FakeOrganizationUnitCodeService
        {
            Mapping = new(OrganizationUnitSource.Place, "WARD01", "NEW01", "病房", true)
        };

        await Create(repository, new FakeMappings(), codeService).QueryAsync(new(
            "2026-05-14", "2026-05-14", CareSource.I, DepartmentCode: "new01"));

        Assert.Equal("WARD01", repository.DepartmentCodes.Single());
        Assert.Equal(DepartmentFilterMode.None, repository.DepartmentModes.Single());
        Assert.True(codeService.ActivePlaceOnly);
    }

    [Fact]
    public async Task QueryAsync_WhitespaceDepartmentDoesNotResolveOrFilter()
    {
        var repository = new FakeC3Repository();
        var codeService = new FakeOrganizationUnitCodeService();

        await Create(repository, new FakeMappings(), codeService).QueryAsync(new(
            "2026-05-14", "2026-05-14", CareSource.O, DepartmentCode: "   "));

        Assert.Null(repository.DepartmentCodes.Single());
        Assert.Equal(0, codeService.CallCount);
    }

    [Fact]
    public async Task QueryAsync_MissingMappingRejectsBeforeRepositoryQuery()
    {
        var repository = new FakeC3Repository();

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            Create(repository, new FakeMappings(), new FakeOrganizationUnitCodeService()).QueryAsync(new(
                "2026-05-14", "2026-05-14", CareSource.O, DepartmentCode: "MISSING")));

        Assert.Equal("請輸入正確科別代碼", exception.Message);
        Assert.Empty(repository.Dates);
    }

    [Fact]
    public async Task QueryAsync_AmbiguousMappingRejectsBeforeRepositoryQuery()
    {
        var repository = new FakeC3Repository();
        var codeService = new FakeOrganizationUnitCodeService
        {
            Exception = new OrganizationUnitMappingAmbiguousException("NEW01")
        };

        await Assert.ThrowsAsync<OrganizationUnitMappingAmbiguousException>(() =>
            Create(repository, new FakeMappings(), codeService).QueryAsync(new(
                "2026-05-14", "2026-05-14", CareSource.O, DepartmentCode: "NEW01")));

        Assert.Empty(repository.Dates);
    }

    private static C3ReportService Create(IC3ReportRepository repository, ISectionMappingRepository mappings,
        IOrganizationUnitCodeService? organizationUnitCodeService = null) =>
        new(repository, new DepartmentFilterResolver(mappings),
            organizationUnitCodeService ?? new FakeOrganizationUnitCodeService(),
            new DepartmentAssignmentService(mappings),
            new ReportTotalCountCache(new MemoryCache(new MemoryCacheOptions())), NullLogger<C3ReportService>.Instance);

    private static C3MovementRow Movement(string runDate, int movementType, decimal quantity,
        string charge = "C1", string room = "A1") =>
        new(movementType, runDate, room, "S1", charge, "SEC", "", "07", "M1", "王小明",
            "醫師", "N", "材料", "MAT1", "1", "I", null, quantity);
}

public sealed class C3DependencyInjectionTests
{
    [Fact]
    public void AddC3ReportServices_ResolvesReportServiceWithRegisteredDependencies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddC3ReportServices();
        services.AddScoped<IC3ReportRepository>(_ => new FakeC3Repository());
        services.AddScoped<ISectionMappingRepository>(_ => new FakeMappings());
        services.AddScoped<IOrganizationUnitMappingRepository>(_ => new FakeOrganizationUnitMappings());
        services.AddSingleton<IReportTotalCountCache, ReportTotalCountCache>();
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        IC3ReportService service = scope.ServiceProvider.GetRequiredService<IC3ReportService>();

        Assert.IsType<C3ReportService>(service);
    }
}

internal sealed class CountingCache : IReportTotalCountCache
{
    private readonly Dictionary<string, int> _values = [];
    public List<string> Keys { get; } = [];
    public int GetOrCreate(string reportCode, IReadOnlyDictionary<string, string?> normalizedFilters, Func<int> countFactory)
    {
        string key = ReportTotalCountCache.CreateCacheKey(reportCode, normalizedFilters); Keys.Add(key);
        if (_values.TryGetValue(key, out int value)) return value;
        value = countFactory(); if (value >= 0) _values[key] = value; return value;
    }
    public void Invalidate(string reportCode) => _values.Clear();
}

internal sealed class FakeC3Repository : IC3ReportRepository
{
    public Func<string, IReadOnlyList<C3MovementRow>>? RowsFactory { get; init; }
    public List<string> Dates { get; } = [];
    public List<string?> DepartmentCodes { get; } = [];
    public List<DepartmentFilterMode> DepartmentModes { get; } = [];
    public Task<IReadOnlyList<C3MovementRow>> QueryDayAsync(C3ValidatedRequest request, string runDate,
        DepartmentFilterMode departmentMode, CancellationToken cancellationToken = default)
    {
        Dates.Add(runDate);
        DepartmentCodes.Add(request.DepartmentCode);
        DepartmentModes.Add(departmentMode);
        if (RowsFactory is not null) return Task.FromResult(RowsFactory(runDate));
        IReadOnlyList<C3MovementRow> rows = Enumerable.Range(1, 14).Select(i => new C3MovementRow(1,
            runDate, "A1", "S1", $"C{i}", "SEC", "", "07", "M1", "王小明", "醫師", "N",
            "材料", $"MAT{i}", "1", "I", null, i)).ToList();
        return Task.FromResult(rows);
    }
}

internal sealed class FakeMappings : ISectionMappingRepository
{
    public string? ValidLocation { get; init; }
    public SectionMapping Translation { get; init; } = new("", "");
    public Task<bool> LocationExistsAsync(string location, CancellationToken cancellationToken = default) => Task.FromResult(location == ValidLocation);
    public Task<SectionMapping?> FindPlaceAsync(string code, CancellationToken cancellationToken = default) => Task.FromResult<SectionMapping?>(null);
    public Task<SectionMapping?> FindSectionAsync(string code, CancellationToken cancellationToken = default) => Task.FromResult<SectionMapping?>(null);
    public Task<SectionMapping?> FindLocationAsync(string sectionCode, CancellationToken cancellationToken = default) => Task.FromResult<SectionMapping?>(null);
    public Task<SectionMapping> TranslateAsync(string oldCode, string roomType = "", CancellationToken cancellationToken = default) => Task.FromResult(Translation);
}

internal sealed class FakeOrganizationUnitMappings : IOrganizationUnitMappingRepository
{
    public IReadOnlyList<OrganizationUnitMapping> Results { get; init; } = [];
    public IReadOnlyList<OrganizationUnitMapping> LegacyResults { get; init; } = [];
    public IReadOnlyList<OrganizationUnitMapping> SearchResults { get; init; } = [];
    public string? RequestedCode { get; private set; }
    public bool ActivePlaceOnly { get; private set; }

    public Task<IReadOnlyList<OrganizationUnitMapping>> FindByNewCodeAsync(string newCode,
        bool activePlaceOnly, CancellationToken cancellationToken = default)
    {
        RequestedCode = newCode;
        ActivePlaceOnly = activePlaceOnly;
        return Task.FromResult(Results);
    }

    public Task<IReadOnlyList<OrganizationUnitMapping>> FindByLegacyCodeAsync(string legacyCode,
        OrganizationUnitMappingScope scope, CancellationToken cancellationToken = default) =>
        Task.FromResult(LegacyResults);

    public Task<IReadOnlyList<OrganizationUnitMapping>> SearchAsync(string query, bool includeSections,
        bool includePlaces, bool activePlaceOnly, int limit, CancellationToken cancellationToken = default) =>
        Task.FromResult(SearchResults);
}

internal sealed class FakeOrganizationUnitCodeService : IOrganizationUnitCodeService
{
    public OrganizationUnitMapping? Mapping { get; init; }
    public Exception? Exception { get; init; }
    public int CallCount { get; private set; }
    public bool ActivePlaceOnly { get; private set; }

    public Task<OrganizationUnitMapping?> ResolveLegacyCodeAsync(string newCode, bool activePlaceOnly,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        ActivePlaceOnly = activePlaceOnly;
        if (Exception is not null) throw Exception;
        return Task.FromResult(Mapping);
    }

    public Task<OrganizationUnitMapping?> ResolveNewCodeAsync(string legacyCode, string roomType,
        OrganizationUnitMappingScope scope, CancellationToken cancellationToken = default) =>
        Task.FromResult(Mapping);

    public Task<IReadOnlyList<OrganizationUnitMapping>> SearchAsync(string query, bool includeSections,
        bool includePlaces, bool activePlaceOnly, int limit = 20,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OrganizationUnitMapping>>([]);
}
