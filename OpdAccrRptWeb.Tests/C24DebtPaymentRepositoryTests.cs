using System.Data;
using Oracle.ManagedDataAccess.Client;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C24DebtPaymentRepositoryTests
{
    [Fact]
    public void AccountingStatements_AreDistinctFixedAndNeverUseRoomType()
    {
        var all = new[] { C24Sql.A01, C24Sql.A02, C24Sql.A03, C24Sql.A04, C24Sql.A05, C24Sql.A06 };
        Assert.Equal(6, all.Distinct().Count());
        Assert.All(all, sql =>
        {
            Assert.Contains(":day_begin", sql);
            Assert.Contains(":day_end", sql);
            Assert.DoesNotContain("chOp1RoomType", sql, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Replace(", sql, StringComparison.OrdinalIgnoreCase);
        });
        Assert.Equal(new[] { "A01", "A02", "A03" }, C24Sql.Accounting(C24Sources.OpdEr).Select(x => x.Code));
        Assert.Equal(new[] { "A04", "A05", "A06" }, C24Sql.Accounting(C24Sources.Inpatient).Select(x => x.Code));
    }

    [Fact]
    public void FixedStatements_UseTheExactSpecTablesAndColumnFamilies()
    {
        Assert.Contains("FROM OpdDrgTbl", C24Sql.A01);
        Assert.Contains("FROM OpdOrdTbl", C24Sql.A02);
        Assert.Contains("FROM OpdOrdTbl", C24Sql.A03);
        Assert.Contains("FROM IpdDrgTbl", C24Sql.A04);
        Assert.Contains("FROM IpdOrdTbl", C24Sql.A05);
        Assert.Contains("FROM IpdOrdTbl", C24Sql.A06);
        Assert.Contains("chOp3IDate", C24Sql.A01);
        Assert.Contains("chOp4OrdNo = 'ACC-69'", C24Sql.A03);
        Assert.Contains("FROM IpdBasicTbl", C24Sql.E01);
        Assert.Contains("FROM OpdRegPtnTbl", C24Sql.E02);
        Assert.Contains("FROM OpdBasicTbl", C24Sql.E03);
        Assert.Contains("FROM GenDctItemTbl", C24Sql.E04);
        Assert.Contains("FROM GenDebtTbl", C24Sql.B01);
        Assert.Contains("FROM IpdDebtTbl", C24Sql.B02);
        Assert.Contains("a.vchBillDate", C24Sql.B02);
    }

    [Fact]
    public void EnrichmentAndBillingStatements_HaveFixedKeysAndBinds()
    {
        Assert.Contains("RTRIM(b.chOp1MrNo) = m.chMrNo", C24Sql.E01);
        Assert.Contains(":visit_date", C24Sql.E02);
        Assert.Contains("chOp1Room IN ('EEEE', 'HHHH')", C24Sql.E03);
        Assert.Contains(":dct_code", C24Sql.E04);
        Assert.Contains("BETWEEN :start_date AND :end_date", C24Sql.B01);
        Assert.Contains("BETWEEN :start_date AND :end_date", C24Sql.B02);
        Assert.Contains(":mr_no", C24Sql.B01);
        Assert.Contains("GenDebtTbl", C24Sql.B01);
        Assert.Contains("IpdDebtTbl", C24Sql.B02);
        Assert.Contains("rlDebtAMT <> 0", C24Sql.B01);
        Assert.Contains("vchBackFlg", C24Sql.B02);
    }

    [Fact]
    public void Parameter_IsExplicitlyTypedSizedAndInputOnly()
    {
        OracleParameter parameter = C24DebtPaymentRepository.Parameter(
            "medicalRecordNo", OracleDbType.Varchar2, "A'--", 10);
        Assert.Equal(OracleDbType.Varchar2, parameter.OracleDbType);
        Assert.Equal(10, parameter.Size);
        Assert.Equal(ParameterDirection.Input, parameter.Direction);
        Assert.Equal("A'--", parameter.Value);
    }

    [Theory]
    [InlineData(C24Sources.OpdEr, "OpdRecRpt_PDebtDM", "OpdRecRpt_PDebtDM_S", true)]
    [InlineData(C24Sources.Inpatient, "IpdRecRpt_PDebtDM", "IpdRecRpt_PDebtDM_S", false)]
    public void LegacyPublication_UsesFixedSourceSpecificAtomicSql(
        string source, string detailTable, string summaryTable, bool hasDischargeFlag)
    {
        var sql = C24DebtPaymentRepository.LegacySql(source);

        Assert.Contains("LOCK TABLE " + detailTable, sql.Lock);
        Assert.Contains("DELETE FROM " + detailTable, sql.DeleteDetails);
        Assert.Contains("DELETE FROM " + summaryTable, sql.DeleteSummaries);
        Assert.Contains("INSERT INTO " + detailTable, sql.InsertDetail);
        Assert.Contains("INSERT INTO " + summaryTable, sql.InsertSummary);
        Assert.Equal(hasDischargeFlag, sql.InsertDetail.Contains("chDC", StringComparison.Ordinal));
        Assert.Contains(":accounting_date", sql.DeleteDetails);
        Assert.Contains(":accounting_date", sql.DeleteSummaries);
    }

    [Theory]
    [InlineData(C24Sources.OpdEr, "OpdRecRpt_PDebtDM", "OpdRecRpt_PDebtDM_S", "chDC")]
    [InlineData(C24Sources.Inpatient, "IpdRecRpt_PDebtDM", "IpdRecRpt_PDebtDM_S", "CAST(NULL")]
    public void LegacyRead_UsesVb6SourceSpecificTablesAndDateRange(
        string source, string detailTable, string summaryTable, string dischargeExpression)
    {
        var sql = C24DebtPaymentRepository.LegacyReadSql(source);

        Assert.Contains("FROM " + summaryTable, sql.Exists);
        Assert.Contains(":accounting_date", sql.Exists);
        Assert.Contains("FROM " + detailTable, sql.Details);
        Assert.Contains("chOp1Date BETWEEN :start_date AND :end_date", sql.Details);
        Assert.Contains(dischargeExpression, sql.Details);
        Assert.Contains("FROM " + summaryTable, sql.Summaries);
        Assert.Contains("chAccDate BETWEEN :start_date AND :end_date", sql.Summaries);
    }
}
