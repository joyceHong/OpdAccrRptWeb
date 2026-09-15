using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Tests;

public sealed class C13HighRiskEmergencyRepositoryTests
{
    [Fact]
    public void GetColumns_ReturnsLegacyTenColumnOrder()
    {
        var repository = new C13HighRiskEmergencyRepository(
            new StubConnectionStringProvider(), new C13LegacyPhoneMasker());

        var columns = repository.GetColumns();

        Assert.Equal(10, columns.Count);
        Assert.Equal(
            ["就診日期", "病歷號", "姓名", "就診身份", "部份負擔", "生日", "身分證字號", "地址", "電話", "急診床號"],
            columns.Select(column => column.Label));
    }

    [Fact]
    public void RangeProjection_PreservesLegacyJoinAndPredicates()
    {
        string sql = C13HighRiskEmergencyRepository.RangeProjection;

        Assert.Contains("SELECT DISTINCT", sql, StringComparison.Ordinal);
        Assert.Contains("JOIN OpdMRBasicTbl", sql, StringComparison.Ordinal);
        Assert.Contains("JOIN GenFin1Tbl", sql, StringComparison.Ordinal);
        Assert.Contains("JOIN OpdPPayTbl", sql, StringComparison.Ordinal);
        Assert.Contains("JOIN GenDebtTbl", sql, StringComparison.Ordinal);
        Assert.Contains("LEFT JOIN OpdEmgSoapDispTbl", sql, StringComparison.Ordinal);
        Assert.Contains("d.intOp1No = r.intOp0No", sql, StringComparison.Ordinal);
        Assert.Contains("s.intRegNo = r.intOp0No", sql, StringComparison.Ordinal);
        Assert.Contains("r.chOp0Room = '0000'", sql, StringComparison.Ordinal);
        Assert.Contains("r.chOp0PMrNo NOT IN ('C36979', '1000000')", sql, StringComparison.Ordinal);
        Assert.Contains("r.chOp0DC <> '1'", sql, StringComparison.Ordinal);
        Assert.Contains("r.chOp0Fin1 IN ('01', '35')", sql, StringComparison.Ordinal);
        Assert.Contains("r.chOp0PPay = '004'", sql, StringComparison.Ordinal);
        Assert.Contains("r.chOp0PName LIKE '%無名氏%'", sql, StringComparison.Ordinal);
        Assert.Contains("RTRIM(d.chBackFlg) IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("d.rlDebtAmt > 0", sql, StringComparison.Ordinal);
        Assert.Contains("r.chOp0Date BETWEEN :StartDate AND :EndDate", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("UNION ALL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SUM(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("2026-09-15", "2026-09-15", "1150915", "1150915")]
    [InlineData("2026-09-14", "2026-09-15", "1150914", "1150915")]
    [InlineData("2026-08-31", "2026-09-02", "1150831", "1150902")]
    [InlineData("2025-09-15", "2026-09-15", "1140915", "1150915")]
    public void BuildDefinition_UsesOneEquivalentInclusiveRange(
        string startDate,
        string endDate,
        string expectedRocStartDate,
        string expectedRocEndDate)
    {
        var definition = C13HighRiskEmergencyRepository.BuildDefinition(new()
        {
            StartDate = startDate,
            EndDate = endDate
        });

        Assert.Equal(expectedRocStartDate, definition.StartDate);
        Assert.Equal(expectedRocEndDate, definition.EndDate);
        Assert.Equal(C13HighRiskEmergencyRepository.RangeProjection, definition.ProjectionSql);
    }

    [Fact]
    public void BuildDefinition_ConvertsInclusiveCrossMonthDatesToOneRange()
    {
        var condition = new SearchReportCondition
        {
            StartDate = "2026-08-31",
            EndDate = "2026-09-02"
        };

        var definition = C13HighRiskEmergencyRepository.BuildDefinition(condition);

        Assert.Equal("1150831", definition.StartDate);
        Assert.Equal("1150902", definition.EndDate);
        Assert.Equal(1, CountOccurrences(definition.ProjectionSql, "SELECT DISTINCT"));
        Assert.DoesNotContain("UNION ALL", definition.ProjectionSql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("2026-08-31", definition.ProjectionSql, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDefinition_OneYearStillUsesExactlyTwoBoundDates()
    {
        var definition = C13HighRiskEmergencyRepository.BuildDefinition(new()
        {
            StartDate = "2025-09-15",
            EndDate = "2026-09-15"
        });

        Assert.Equal("1140915", definition.StartDate);
        Assert.Equal("1150915", definition.EndDate);
        Assert.Equal(1, CountOccurrences(definition.ProjectionSql, "SELECT DISTINCT"));
        Assert.DoesNotContain("UNION ALL", definition.ProjectionSql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddRangeParameters_BindsExactlyTwoFixedLengthRocDates()
    {
        var definition = C13HighRiskEmergencyRepository.BuildDefinition(new()
        {
            StartDate = "2025-09-15",
            EndDate = "2026-09-15"
        });
        using var command = new OracleCommand();

        C13HighRiskEmergencyRepository.AddRangeParameters(command, definition);

        Assert.Equal(2, command.Parameters.Count);
        AssertParameter(command.Parameters["StartDate"], "1140915");
        AssertParameter(command.Parameters["EndDate"], "1150915");
    }

    [Fact]
    public void BuildDefinition_RejectsInvalidOrReversedDates()
    {
        Assert.Throws<ArgumentException>(() => C13HighRiskEmergencyRepository.BuildDefinition(new()
        {
            StartDate = "2026-09-02",
            EndDate = "2026-09-01"
        }));
    }

    private static int CountOccurrences(string value, string token) =>
        value.Split(token, StringSplitOptions.None).Length - 1;

    private static void AssertParameter(OracleParameter parameter, string expectedValue)
    {
        Assert.Equal(OracleDbType.Char, parameter.OracleDbType);
        Assert.Equal(7, parameter.Size);
        Assert.Equal(expectedValue, parameter.Value);
    }

    private sealed class StubConnectionStringProvider : Infrastructure.IConnectionStringProvider
    {
        public string GetConnectionString() => "unused";
    }
}
