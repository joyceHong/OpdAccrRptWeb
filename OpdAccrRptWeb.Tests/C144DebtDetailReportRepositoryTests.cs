using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Tests;

public sealed class C144DebtDetailReportRepositoryTests
{
    [Fact]
    public void InpatientSql_PreservesLegacyFiltersClassificationAndPaging()
    {
        string sql = C144Sql.Page(C144Sql.InpatientProjection);
        Assert.Contains("FROM IpdDebtTbl X", sql);
        Assert.Contains("JOIN IpdOrdTbl B", sql);
        Assert.Contains("JOIN IpdDrgTbl B", sql);
        Assert.Contains("X.vchMrNo NOT IN ('C36979', '1000000')", sql);
        Assert.Contains("X.vchBackFlg NOT IN ('D', 'R', 'T')", sql);
        Assert.Contains("RTRIM(B.chOp4IDate) IS NOT NULL", sql);
        Assert.Contains("RTRIM(B.chOp3IDate) IS NOT NULL", sql);
        Assert.Contains("B.chOp4PFin1 IN ('30','35')", sql);
        Assert.Contains("UNION ALL", sql);
        Assert.Contains("ROUND(SUM(sub16),0)", sql);
        Assert.Contains("OFFSET :RowOffset ROWS FETCH NEXT :PageSize ROWS ONLY", sql);
    }

    [Fact]
    public void OutpatientSql_PreservesLegacySourceDifferencesAndDrugRules()
    {
        string sql = C144Sql.All(C144Sql.OutpatientEmergencyProjection);
        Assert.Contains("FROM GenDebtTbl X", sql);
        Assert.Contains("JOIN OpdOrdTbl B", sql);
        Assert.Contains("JOIN OpdDrgTbl B", sql);
        Assert.Contains("DECODE(RTRIM(X.chOp1Room), '0000', 'E', 'R')", sql);
        Assert.Contains("B.chOp4PFin1 IN ('01','35')", sql);
        Assert.Contains("B.chOp4PFin1 = '30'", sql);
        Assert.Contains("LIKE '49-U%' AND LENGTH(RTRIM(B.chOp4OrdNo))=5", sql);
        Assert.DoesNotContain("chOp4IDate", sql);
    }

    [Fact]
    public void CreateCommand_UsesNamedSevenCharacterDateBinds()
    {
        using var connection = new OracleConnection();
        var query = new C144Query("1150901", "1150916", C144Sources.OpdEr, 1, 10);
        using OracleCommand command = C144DebtDetailReportRepository.CreateCommand(
            C144Sql.OutpatientEmergencyProjection, connection, query);
        Assert.True(command.BindByName);
        Assert.Equal(600, command.CommandTimeout);
        Assert.Equal(OracleDbType.Char, command.Parameters["StartDate"].OracleDbType);
        Assert.Equal(7, command.Parameters["StartDate"].Size);
        Assert.Equal("1150901", command.Parameters["StartDate"].Value);
        Assert.Equal("1150916", command.Parameters["EndDate"].Value);
    }
}
