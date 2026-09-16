using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Tests;

public sealed class C143AccountingBalanceDebtRepositoryTests
{
    [Fact]
    public void OutpatientSql_PreservesLegacyJoinMaterialityAndPaging()
    {
        string sql = C143Sql.Page(C143Sql.OutpatientProjection, C143Sql.OutpatientOrderBy);

        Assert.Contains("FULL OUTER JOIN", sql);
        Assert.Contains("IN ('01', '35')", sql);
        Assert.Contains("A.chOp4PFin1 = '30'", sql);
        Assert.Contains("ABS(rlDebtAmt2) > 10", sql);
        Assert.Contains(":ReportType = '2'", sql);
        Assert.Contains("CC.chOp1RoomTypeName = '急診' AND B.chOp1EOutDate IS NOT NULL", sql);
        Assert.Contains("OFFSET :RowOffset ROWS FETCH NEXT :PageSize ROWS ONLY", sql);
    }

    [Fact]
    public void InpatientSql_PreservesLegacyFourKeySAndS2Contracts()
    {
        string sql = C143Sql.Page(C143Sql.InpatientProjection, C143Sql.InpatientOrderBy);

        Assert.Contains("(A.chDate, A.chTime, A.chRoom, A.intNo) NOT IN", sql);
        Assert.Contains("LNNVL(C.vchAccLock = '1')", sql);
        Assert.Contains("LEFT OUTER JOIN IpdBasic2Tbl", sql);
        Assert.Contains("CC.s AS AccountingOutstanding", sql);
        Assert.Contains("CC.s2 AS AccountingMergedOutstanding", sql);
        Assert.Contains("ABS(NVL(CC.s2, 0) - NVL(XX.rlDebtAmt2, 0)) > 10", sql);
        Assert.Contains("NVL(CC.s, 0) - NVL(XX.rlDebtAmt2, 0) AS Difference", sql);
        Assert.Contains(":DischargeGroup = 2", sql);
    }

    [Theory]
    [InlineData("Difference", "1", 1)]
    [InlineData("All", "2", 2)]
    public void CreateCommand_UsesNamedTypedLegacyBinds(
        string reportType, string expectedLegacyType, int dischargeGroup)
    {
        using var connection = new OracleConnection();
        var query = new C143Query("1150901", "1150914", "Inpatient", reportType, 1, 10);

        using OracleCommand command = C143AccountingBalanceDebtRepository.CreateCommand(
            C143Sql.InpatientProjection, connection, query, dischargeGroup);

        Assert.True(command.BindByName);
        Assert.Equal(600, command.CommandTimeout);
        AssertParameter(command, "StartDate", OracleDbType.Char, "1150901", 7);
        AssertParameter(command, "EndDate", OracleDbType.Char, "1150914", 7);
        AssertParameter(command, "ReportType", OracleDbType.Char, expectedLegacyType, 1);
        AssertParameter(command, "DischargeGroup", OracleDbType.Int32, dischargeGroup, 0);
    }

    private static void AssertParameter(
        OracleCommand command, string name, OracleDbType type, object value, int size)
    {
        OracleParameter parameter = command.Parameters[name];
        Assert.Equal(type, parameter.OracleDbType);
        Assert.Equal(value, parameter.Value);
        if (size > 0) Assert.Equal(size, parameter.Size);
    }
}
