using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Tests;

public sealed class C9ReportRepositoryTests
{
    [Fact]
    public void Sql_PreservesCanonicalSelectionContract()
    {
        string sql = C9Sql.Spay6OrderDetail;
        foreach (string fragment in new[]
        {
            "FROM OpdOrdTbl A", "OpdBasicTbl B", "GenSectionTbl C",
            "A.chOp1Date = B.chOp1Date", "A.chOp1Time = B.chOp1Time",
            "A.chOp1Room = B.chOp1Room", "A.intOp1No = B.intOp1No",
            "B.chOp1Sec = C.chSecNo", "A.chOp4SPay = '6'",
            "A.chOp4Stat <> 'DC'", "B.chOp1Date = :run_date",
            "A.chOp4Proj NOT IN ('I', 'S')", "RTRIM(A.chOp4Proj) IS NULL"
        }) Assert.Contains(fragment, sql, StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain("DISTINCT", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ORDER BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("GROUP BY", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("OpdDrgTbl", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task QueryDay_RejectsNonRocBoundaryBeforeOpeningConnection()
    {
        var repository = new C9ReportRepository(new ThrowingConnectionStringProvider());
        await Assert.ThrowsAsync<ArgumentException>(() => repository.QueryDayAsync("20260101"));
    }

    private sealed class ThrowingConnectionStringProvider : Infrastructure.IConnectionStringProvider
    {
        public string GetConnectionString() => throw new InvalidOperationException("must not open");
    }
}
