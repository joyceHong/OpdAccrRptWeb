using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Tests;

public sealed class M2DoctorMonthlyReportCoreTests
{
    [Fact]
    public void RepositoryNumberOrZero_ReturnsZeroForDatabaseNull()
    {
        Assert.Equal(0, M2DoctorMonthlyReportRepository.NumberOrZero(DBNull.Value));
        Assert.Equal(0, M2DoctorMonthlyReportRepository.NumberOrZero(null));
        Assert.Equal(15, M2DoctorMonthlyReportRepository.NumberOrZero(15m));
    }

    [Fact]
    public void Request_DefaultsToStatisticsAllAll()
    {
        var request = new M2DoctorMonthlyReportRequest("2026-08");
        Assert.Equal(M2CalculationBasis.Statistics, request.CalculationBasis);
        Assert.Equal(M2VisitScope.All, request.VisitScope);
        Assert.Equal(M2TimeSlot.All, request.TimeSlot);
    }

    [Fact]
    public void Row_Requires31DaysAndDerivesSignedTotal()
    {
        int[] counts = new int[31]; counts[0] = 5; counts[1] = -2;
        M2DoctorMonthlyReportRow row = M2DoctorMonthlyReportRow.Create(
            " 0450 ", " 急診 ", " D1 ", " 王醫師 ", counts);
        Assert.Equal("0450", row.SectionNo);
        Assert.Equal(3, row.MonthlyTotal);
        Assert.Throws<ArgumentException>(() => M2DoctorMonthlyReportRow.Create(
            "1", "a", "d", "n", new int[30]));
    }

    [Fact]
    public void StatisticsSql_ContainsFormulaMatrixAndApprovedSources()
    {
        string sql = M2DoctorMonthlySql.Statistics;
        foreach (string value in new[] { "OpdRegStatsTbl", "GenSectionTbl", "GenDoctorTbl",
                     ":report_month", ":visit_scope", ":time_slot", "OUTPATIENT", "EMERGENCY",
                     "MORNING", "AFTERNOON", "NIGHT" }) Assert.Contains(value, sql);
        Assert.Equal(12, System.Text.RegularExpressions.Regex.Matches(sql, "WHEN :visit_scope").Count);
        Assert.DoesNotContain("DocMonthSumTbl", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ActualVisitSql_SelectsHistoricalRulesAndExclusions()
    {
        string oldSql = M2DoctorMonthlySql.SelectActualVisit("10507");
        string newSql = M2DoctorMonthlySql.SelectActualVisit("10508");
        Assert.DoesNotContain("92431", oldSql);
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(oldSql, "'F','M'").Count);
        Assert.Contains("92431", newSql);
        Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(newSql, "'F','M'").Count);
        foreach (string value in new[] { "1051001", "RRRR", "SSSS", "ZZZZ", "chOp0Type<>'20'",
                     "chOp0QuoteFlg<>'N'", "0297*", "C36979", "1000000", "chOp0DC='0'" })
            Assert.Contains(value, newSql);
        Assert.DoesNotContain("'DDDD'", newSql);
        Assert.DoesNotContain("DocMonthSumTbl", newSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RepositoryRejectsInvalidInputsBeforeOpeningConnection()
    {
        var repository = new M2DoctorMonthlyReportRepository(new ThrowingConnectionStringProvider());
        await Assert.ThrowsAsync<ArgumentException>(() => repository.QueryStatisticsAsync(
            "202608", M2VisitScope.All, M2TimeSlot.All));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => repository.QueryActualVisitDayAsync(
            "11508", 32, M2VisitScope.All, M2TimeSlot.All));
    }

    private sealed class ThrowingConnectionStringProvider : Infrastructure.IConnectionStringProvider
    {
        public string GetConnectionString() => throw new InvalidOperationException("must not open");
    }
}
