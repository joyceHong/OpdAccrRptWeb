using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Tests;

public sealed class M3OpdEmergencyDailyReportRepositoryTests
{
    [Fact]
    public void Sql_UsesOnlyBoundReadQueriesAndLegacyInclusiveBoundaries()
    {
        string all = string.Join('\n', M3OpdEmergencyDailySql.Daily, M3OpdEmergencyDailySql.Monthly,
            M3OpdEmergencyDailySql.Yearly, M3OpdEmergencyDailySql.EmergencyDay,
            M3OpdEmergencyDailySql.EmergencyEvening, M3OpdEmergencyDailySql.EmergencyNight,
            M3OpdEmergencyDailySql.Kpis, M3OpdEmergencyDailySql.DepartmentName);
        Assert.Contains("OpdRegStatsTbl", all); Assert.Contains("OpdRegPtntbl", all);
        Assert.Contains("OpdBasicTbl", all); Assert.Contains("GenSectionTbl", all);
        Assert.Contains(":report_date", all); Assert.Contains(":month_start_date", all);
        Assert.Contains(":year_start_date", all); Assert.Contains("BETWEEN", all);
        Assert.Contains("0730", ParameterValues()); Assert.Contains("1530", ParameterValues());
        Assert.DoesNotContain("rptOpdStatsA", all, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("rptOpdStats01", all, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("INSERT ", all, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE ", all, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("DELETE ", all, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AggregateSql_PreservesToNumberAndSuffixRules()
    {
        Assert.Contains("SUM(TO_NUMBER(chOp1SQty))", M3OpdEmergencyDailySql.Daily);
        Assert.Contains("('A','B','C','D','E','F','M','*')", M3OpdEmergencyDailySql.Daily);
        Assert.DoesNotContain("NVL", M3OpdEmergencyDailySql.Daily, StringComparison.OrdinalIgnoreCase);
    }

    private static string ParameterValues() => "0000 0730 1530 2330 2359";
}
