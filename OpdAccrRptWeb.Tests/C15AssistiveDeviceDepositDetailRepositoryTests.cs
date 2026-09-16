using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Tests;

public sealed class C15AssistiveDeviceDepositDetailRepositoryTests
{
    [Fact]
    public void QuerySql_PreservesLegacyJoinPredicatesNullSemanticsAndOrder()
    {
        string sql = C15AssistiveDeviceDepositDetailRepository.QuerySql;

        Assert.Contains("JOIN OpdOrdTbl o", sql);
        Assert.Contains("o.intOp1No = a.intOp1No", sql);
        Assert.Contains("a.chOp1MrNo NOT IN ('C36979', '1000000')", sql);
        Assert.Contains("a.chOp4DC IN ('0', '2')", sql);
        Assert.Contains("a.chOp4DCDate BETWEEN :StartDate AND :EndDate", sql);
        Assert.Contains("o.chOp4Stat <> 'DC'", sql);
        Assert.Contains("'696-007', '696-008'", sql);
        Assert.Contains("o.rlOp4Sub1 + o.rlOp4Sub2", sql);
        Assert.Contains("ORDER BY a.chOp1Date, a.chOp1Time, a.chOp1Room, a.intOp1No", sql);
        Assert.DoesNotContain("NVL", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SUM(", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ROW_NUMBER", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RPAD", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildPeriod_ConvertsSpecifiedGregorianRangeToRoc()
    {
        var period = C15AssistiveDeviceDepositDetailRepository.BuildPeriod(new SearchReportCondition
        {
            StartDate = "2026-09-01",
            EndDate = "2026-09-16"
        });

        Assert.Equal("1150901", period.StartDate);
        Assert.Equal("1150916", period.EndDate);
    }

    [Fact]
    public void AddParameters_UsesNamedLegacyTypesSizesAndBoundaries()
    {
        using var command = new OracleCommand { BindByName = true };
        var period = new C15AssistiveDeviceDepositDetailRepository.C15QueryPeriod("1150901", "1150916");

        C15AssistiveDeviceDepositDetailRepository.AddParameters(command, period);

        Assert.True(command.BindByName);
        AssertParameter(command.Parameters["StartDate"], OracleDbType.Varchar2, 7, "1150901");
        AssertParameter(command.Parameters["EndDate"], OracleDbType.Varchar2, 7, "1150916");
        AssertParameter(command.Parameters["AidStartDateTime"], OracleDbType.Varchar2, 11, "11509010000");
        AssertParameter(command.Parameters["AidEndDateTime"], OracleDbType.Varchar2, 11, "11509169999");
        AssertParameter(command.Parameters["OrderStartDateTime"], OracleDbType.Char, 11, "11509010000");
        AssertParameter(command.Parameters["OrderEndDateTime"], OracleDbType.Char, 11, "11509169999");
    }

    [Fact]
    public void BuildPeriod_RejectsInvalidOrReversedRange()
    {
        Assert.Throws<ArgumentException>(() =>
            C15AssistiveDeviceDepositDetailRepository.BuildPeriod(new SearchReportCondition
            {
                StartDate = "2026-09-16",
                EndDate = "2026-09-01"
            }));
    }

    private static void AssertParameter(
        OracleParameter parameter,
        OracleDbType type,
        int size,
        string value)
    {
        Assert.Equal(type, parameter.OracleDbType);
        Assert.Equal(size, parameter.Size);
        Assert.Equal(value, parameter.Value);
    }
}
