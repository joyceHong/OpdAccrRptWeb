using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Tests;

public sealed class C16ReportRepositoryTests
{
    [Fact]
    public void FixedSql_PreservesModeSpecificSemantics()
    {
        Assert.Contains("s.chOp1Date BETWEEN :StartDate AND :EndDate", C16ReportRepository.OutpatientVisitDateSql);
        Assert.Contains("o.chOp4Stat<>'DC'", C16ReportRepository.OutpatientVisitDateSql);
        Assert.DoesNotContain("o.chOp4Stat<>'DC'", C16ReportRepository.OutpatientAccountingDateSql);
        Assert.Contains("THEN -o.rlOp4Sub5", C16ReportRepository.OutpatientAccountingDateSql);
        Assert.Contains("s.chOp1Time='0'", C16ReportRepository.InpatientAccountingDateSql);
        Assert.Contains("ORDER BY SUBSTR(b.doctAllowDate,1,7)", C16ReportRepository.InpatientAccountingDateSql);
        Assert.Contains("SUBSTR(o.chOp4Dct,1,2)='49'", C16ReportRepository.InpatientAccountingDateSql);
    }

    [Theory]
    [MemberData(nameof(AllReportSql))]
    public void FixedSql_SeparatesConcatenatedClauses(string sql)
    {
        Assert.DoesNotContain("SecWHERE", sql, StringComparison.Ordinal);
        Assert.DoesNotContain(")GROUP BY", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("NoHAVING", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("HAVINGSUM", sql, StringComparison.Ordinal);
    }

    public static TheoryData<string> AllReportSql => new()
    {
        C16ReportRepository.OutpatientVisitDateSql,
        C16ReportRepository.OutpatientAccountingDateSql,
        C16ReportRepository.InpatientAccountingDateSql
    };

    [Fact]
    public void Parameters_AreTypedAndSized()
    {
        using var command = new OracleCommand();
        var request = new C16PreviewRequest("2026-09-01", "2026-09-16", C16Source.OutpatientEmergency,
            C16ReportType.Child, C16DateBasis.AccountingDate);
        C16ReportRepository.AddParameters(command, request, new("1150901", "1150916"), true);
        Assert.Equal(OracleDbType.Char, command.Parameters["StartDate"].OracleDbType);
        Assert.Equal(7, command.Parameters["StartDate"].Size);
        Assert.Equal(OracleDbType.Int32, command.Parameters["ReportType"].OracleDbType);
        Assert.Equal(1, command.Parameters["ReportType"].Value);

        using var accounting = new OracleCommand();
        C16ReportRepository.AddParameters(accounting, request, new("1150901", "1150916"), false);
        Assert.Equal(11, accounting.Parameters["EndDateTime"].Size);
        Assert.False(accounting.Parameters.Contains("StartDate"));
    }

    [Fact]
    public void DiagnosisSql_PreservesStatusAndOrdering()
    {
        Assert.Contains("s.chStat<>'DC' OR RTRIM(s.chStat) IS NULL", C16ReportRepository.FirstDiagnosisSql);
        Assert.Contains("ORDER BY s.chDiagState,s.chPrintDate,s.intSortNo", C16ReportRepository.FirstDiagnosisSql);
        Assert.Contains("FETCH FIRST 1 ROW ONLY", C16ReportRepository.FirstDiagnosisSql);
    }
}
