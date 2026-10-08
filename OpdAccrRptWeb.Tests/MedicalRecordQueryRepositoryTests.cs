using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Tests;

public sealed class MedicalRecordQueryRepositoryTests
{
    [Fact]
    public void BuildWhere_UsesFixedColumnsAndParametersForLegacyPredicates()
    {
        using var command = new OracleCommand();
        string where = MedicalRecordQueryPredicates.BuildWhere(new(
            "AB123", "", "王", "台北", "", "", "", ">123"), command);

        Assert.Contains("B.chMrNo LIKE :Filter0 || '%'", where, StringComparison.Ordinal);
        Assert.Contains("B.chName LIKE :Filter1 || '%'", where, StringComparison.Ordinal);
        Assert.Contains("B.chAdd1 LIKE :Filter2 || '%'", where, StringComparison.Ordinal);
        Assert.Contains("SUBSTR(B.chNewBirthday, 1, 3) > :Filter3", where, StringComparison.Ordinal);
        Assert.DoesNotContain("AB123", where, StringComparison.Ordinal);
        Assert.Equal(4, command.Parameters.Count);
    }

    [Fact]
    public void BuildWhere_PreservesBlankAndNonBlankPredicatesWithoutUserSql()
    {
        using var command = new OracleCommand();
        string where = MedicalRecordQueryPredicates.BuildWhere(new(
            "!", "#", "", "", "", "", "", ""), command);

        Assert.Contains("B.chMrNo > '.'", where, StringComparison.Ordinal);
        Assert.Contains("(NOT B.chID > '' OR B.chID IS NULL)", where, StringComparison.Ordinal);
        Assert.Equal(0, command.Parameters.Count);
    }

    [Fact]
    public void BuildWhere_RejectsEmptyFiltersToProtectAgainstFullTableQuery()
    {
        using var command = new OracleCommand();

        Assert.Throws<ArgumentException>(() =>
            MedicalRecordQueryPredicates.BuildWhere(new("", "", "", "", "", "", "", ""), command));
    }

    [Fact]
    public void Sql_ContainsPagedProjectionAndDebtBackFlagRule()
    {
        Assert.Contains("ROW_NUMBER() OVER", MedicalRecordQuerySql.PageBase, StringComparison.Ordinal);
        Assert.Contains("B.chSWKNo AS chPatientCondition", MedicalRecordQuerySql.PageBase, StringComparison.Ordinal);
        Assert.Contains("B.ROWID", MedicalRecordQuerySql.PageBase, StringComparison.Ordinal);
        Assert.Contains("RTRIM(D.chBackFlg) IS NULL", MedicalRecordQuerySql.DebtTotal, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("C36979")]
    [InlineData("1234567890")]
    public void ExactLookupParameter_UsesUnpaddedVarchar2Value(string medicalRecordNo)
    {
        OracleParameter parameter =
            MedicalRecordQueryRepository.CreateExactMedicalRecordParameter(medicalRecordNo);

        Assert.Equal(OracleDbType.Varchar2, parameter.OracleDbType);
        Assert.Equal(10, parameter.Size);
        Assert.Equal(medicalRecordNo, parameter.Value);
    }

    [Fact]
    public void ExactLookupSql_UsesTheSharedMedicalRecordParameterInBothPaths()
    {
        Assert.Contains("B.chMrNo = :MedicalRecordNo", MedicalRecordQuerySql.Detail,
            StringComparison.Ordinal);
        Assert.Contains("A.chMrNo = :MedicalRecordNo", MedicalRecordQuerySql.MergedMedicalRecordNumbers,
            StringComparison.Ordinal);
    }
}
