using Dapper;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C10ReceivableDetailRepositoryTests
{
    [Fact]
    public void OutpatientDebt_PreservesLegacyPredicatesWithoutHavingClause()
    {
        string sql = C10Sql.OutpatientDebt;

        Assert.Contains("GenDebtTbl", sql, StringComparison.Ordinal);
        Assert.Contains("B.chBillDate BETWEEN :StartDate AND :EndDate", sql, StringComparison.Ordinal);
        Assert.Contains("B.chBackFlg IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("RTRIM(B.chMrNo) = :MedicalRecordNumber", sql, StringComparison.Ordinal);
        Assert.Contains(":RoomScope", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("HAVING", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InpatientDebt_UsesInpatientFieldsAndHasNoRoomScope()
    {
        string sql = C10Sql.InpatientDebt;

        Assert.Contains("IpdDebtTbl", sql, StringComparison.Ordinal);
        Assert.Contains("B.vchBillDate BETWEEN :StartDate AND :EndDate", sql, StringComparison.Ordinal);
        Assert.Contains("B.vchBackFlg IS NULL", sql, StringComparison.Ordinal);
        Assert.Contains("C.chMrNo = :MedicalRecordNumber", sql, StringComparison.Ordinal);
        Assert.DoesNotContain(":RoomScope", sql, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("OpdEr", "OpdOrdTbl", "OpdDrgTbl")]
    [InlineData("Inpatient", "IpdOrdTbl", "IpdDrgTbl")]
    public void DetailSql_BindsEveryVisitAndKeepsUnionAll(
        string source,
        string orderTable,
        string drugTable)
    {
        string sql = C10Sql.BuildDetail(source, 2);

        Assert.Contains(orderTable, sql, StringComparison.Ordinal);
        Assert.Contains(drugTable, sql, StringComparison.Ordinal);
        Assert.Contains(":VisitDate0", sql, StringComparison.Ordinal);
        Assert.Contains(":VisitNumber1", sql, StringComparison.Ordinal);
        Assert.Contains("UNION ALL", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("DebtTbl", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("COALESCE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NVL", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void InpatientDetail_PreservesAdditionalFilters()
    {
        string sql = C10Sql.BuildDetail(C10Sources.Inpatient, 1);

        Assert.Contains("chOp4IDate", sql, StringComparison.Ordinal);
        Assert.Contains("chOp3IDate", sql, StringComparison.Ordinal);
        Assert.Contains("chOp3Rep3Flg", sql, StringComparison.Ordinal);
        Assert.Contains("'09', '10', '11', '12'", sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Parameters_NormalizeIsoDatesAndKeepMedicalRecordBound()
    {
        var condition = new SearchReportCondition
        {
            StartDate = "2026-09-01",
            EndDate = "2026-09-02",
            Source = C10Sources.OpdEr,
            RoomScope = C10RoomScopes.Emergency,
            MedicalRecordNo = "AB12"
        };

        DynamicParameters parameters = C10ReceivableDetailRepository.CreateDebtParameters(condition);

        Assert.Equal("1150901", parameters.Get<string>("StartDate"));
        Assert.Equal("1150902", parameters.Get<string>("EndDate"));
        Assert.Equal("AB12", parameters.Get<string>("MedicalRecordNumber"));
        Assert.Equal(1, parameters.Get<int>("RoomScope"));
    }

    [Fact]
    public void MedicalRecordInjectionText_IsOnlyAParameterValue()
    {
        const string malicious = "X' OR '1'='1";
        var condition = new SearchReportCondition
        {
            StartDate = "2026-09-01",
            EndDate = "2026-09-02",
            Source = C10Sources.OpdEr,
            RoomScope = C10RoomScopes.All,
            MedicalRecordNo = malicious
        };

        DynamicParameters parameters = C10ReceivableDetailRepository.CreateDebtParameters(condition);

        Assert.Equal(malicious, parameters.Get<string>("MedicalRecordNumber"));
        Assert.DoesNotContain(malicious, C10Sql.OutpatientDebt, StringComparison.Ordinal);
        Assert.Contains(":MedicalRecordNumber", C10Sql.OutpatientDebt, StringComparison.Ordinal);
    }
}
