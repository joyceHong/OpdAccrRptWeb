using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class SafeNeedleRepositoryTests
{
    [Fact]
    public void Constructor_DoesNotResolveConnectionBeforeAQueryRuns()
    {
        var provider = new ThrowingConnectionStringProvider();

        _ = new SafeNeedleRepository(provider);

        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public void GetBaseSql_Emergency_UsesOnlyFixedOpdTables()
    {
        string sql = SafeNeedleRepository.GetBaseSql(EncounterSources.Emergency);

        Assert.Contains("FROM OpdOrdTbl o", sql);
        Assert.Contains("JOIN OpdBasicTbl b", sql);
        Assert.DoesNotContain("IpdOrdTbl", sql);
        Assert.DoesNotContain("IpdBasicTbl", sql);
        Assert.DoesNotContain("OpdRegPtnTbl", sql);
        Assert.DoesNotContain("chop1room = '0000'", sql);
    }

    [Fact]
    public void GetBaseSql_Inpatient_UsesOnlyFixedIpdTables()
    {
        string sql = SafeNeedleRepository.GetBaseSql(EncounterSources.Inpatient);

        Assert.Contains("FROM IpdOrdTbl o", sql);
        Assert.Contains("JOIN IpdBasicTbl b", sql);
        Assert.DoesNotContain("OpdOrdTbl", sql);
        Assert.DoesNotContain("OpdBasicTbl", sql);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Outpatient")]
    public void GetBaseSql_UnknownSource_Throws(string? source)
    {
        Assert.Throws<ArgumentException>(() => SafeNeedleRepository.GetBaseSql(source));
    }

    [Theory]
    [InlineData(EncounterSources.Emergency)]
    [InlineData(EncounterSources.Inpatient)]
    public void GetBaseSql_PreservesSafeNeedleSelectionAndRowProjection(string source)
    {
        string sql = SafeNeedleRepository.GetBaseSql(source);
        string[] approvedCodes =
        [
            "SICPU24", "SICPU22", "SICPU20", "SICPU19", "SICPU18", "SICTEF20",
            "SDS3", "SDS0.5", "SSN23S", "SDS1", "SDS3B"
        ];

        Assert.All(approvedCodes, code => Assert.Contains($"'{code}'", sql));
        Assert.Contains("THEN 'X'", sql);
        Assert.Contains("ELSE 'Y'", sql);
        Assert.Contains("o.chop4idate BETWEEN :orderDateStart AND :orderDateEnd", sql);
        Assert.Contains("o.chop4dcdate NOT BETWEEN :orderDateStart AND :orderDateEnd", sql);
        Assert.Contains("o.chop4stat <> 'DC'", sql);
        Assert.Contains("o.chop1date = b.chop1date", sql);
        Assert.Contains("o.chop1time = b.chop1time", sql);
        Assert.Contains("o.chop1room = b.chop1room", sql);
        Assert.Contains("o.intop1no = b.intop1no", sql);
        Assert.Contains("SELECT DISTINCT", sql);
        Assert.Contains("b.chop1ebid LIKE :stationPrefix", sql);
        Assert.DoesNotContain("ROW_NUMBER", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FULL OUTER JOIN", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SafeNeedleTbl", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Crystal", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chop4idate_1", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chop4idate_2", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chop4idate_3", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateParameters_UsesBoundDateRangeAndQuotedPrefixAsData()
    {
        object parameters = SafeNeedleRepository.CreateParameters(new SearchReportCondition
        {
            EncounterSource = EncounterSources.Inpatient,
            StartDate = "1150824",
            StationOrBedPrefix = " 7'A "
        });

        Assert.Equal("11508240000", ReadProperty<string>(parameters, "orderDateStart"));
        Assert.Equal("11508249999", ReadProperty<string>(parameters, "orderDateEnd"));
        Assert.Equal("7'A%", ReadProperty<string>(parameters, "stationPrefix"));
        Assert.Equal(0L, ReadProperty<long>(parameters, "rowOffset"));
        Assert.Equal(10, ReadProperty<int>(parameters, "pageSize"));
    }

    [Theory]
    [InlineData(EncounterSources.Emergency)]
    [InlineData(EncounterSources.Inpatient)]
    public void CountAndPageSql_WrapTheSameBaseQuery(string source)
    {
        string baseSql = SafeNeedleRepository.GetBaseSql(source);
        string countSql = SafeNeedleRepository.GetCountSql(source);
        string pageSql = SafeNeedleRepository.GetPageSql(source);

        Assert.Contains(baseSql, countSql);
        Assert.Contains(baseSql, pageSql);
        Assert.Contains("SELECT COUNT(*)", countSql);
        Assert.Contains("ORDER BY BedNumber, MedicalRecordNumber, Category, OrderCode, OrderDate", pageSql);
        Assert.Contains("OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY", pageSql);
        Assert.DoesNotContain("ROW_NUMBER", pageSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("FULL OUTER JOIN", pageSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chop4idate_1", pageSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateParameters_UsesOneBasedPageAndLongOffset()
    {
        object parameters = SafeNeedleRepository.CreateParameters(new SearchReportCondition
        {
            EncounterSource = EncounterSources.Emergency,
            StartDate = "1150824",
            PageNumber = int.MaxValue,
            PageSize = 50
        });

        Assert.Equal(((long)int.MaxValue - 1) * 50, ReadProperty<long>(parameters, "rowOffset"));
        Assert.Equal(50, ReadProperty<int>(parameters, "pageSize"));
    }

    [Fact]
    public void CreateParameters_EmptyPrefixUsesNullAndMissingDateThrows()
    {
        object parameters = SafeNeedleRepository.CreateParameters(new SearchReportCondition
        {
            EncounterSource = EncounterSources.Emergency,
            StartDate = "1150824",
            StationOrBedPrefix = " "
        });

        Assert.Null(ReadProperty<string?>(parameters, "stationPrefix"));
        Assert.Throws<ArgumentException>(() => SafeNeedleRepository.CreateParameters(new SearchReportCondition
        {
            EncounterSource = EncounterSources.Emergency
        }));
    }

    private static T ReadProperty<T>(object value, string name) =>
        (T)value.GetType().GetProperty(name)!.GetValue(value)!;

    private sealed class ThrowingConnectionStringProvider : IConnectionStringProvider
    {
        public int Calls { get; private set; }

        public string GetConnectionString()
        {
            Calls++;
            throw new InvalidOperationException();
        }
    }
}
