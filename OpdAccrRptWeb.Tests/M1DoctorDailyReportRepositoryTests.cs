using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Tests;

public sealed class M1DoctorDailyReportRepositoryTests
{
    [Fact]
    public void Select_UsesLegacyCutoffAndContainsOnlyApprovedSources()
    {
        string before = M1DoctorDailySql.Select("1000831");
        string after = M1DoctorDailySql.Select("1000901");

        Assert.Contains("chOp1HtQty) + TO_NUMBER(A.chOp1HcQty)", before);
        Assert.Contains("chOp1SQty) + TO_NUMBER(A.chOp1HtQty)", after);
        foreach (string sql in new[] { before, after })
        {
            Assert.Contains("A.chDate = :report_date", sql);
            Assert.Contains("OpdRegStatsTbl", sql);
            Assert.Contains("GenSectionTbl", sql);
            Assert.Contains("GenDoctorTbl", sql);
            Assert.DoesNotContain("DocDaySumTbl", sql, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task Query_RejectsInvalidRocDateBeforeOpeningConnection()
    {
        var repository = new M1DoctorDailyReportRepository(new ThrowingConnectionStringProvider());
        await Assert.ThrowsAsync<ArgumentException>(() => repository.QueryAsync("20260924"));
    }

    [Fact]
    public void NumberOrZero_ReturnsZeroForDatabaseNull()
    {
        Assert.Equal(0, M1DoctorDailyReportRepository.NumberOrZero(DBNull.Value));
        Assert.Equal(0, M1DoctorDailyReportRepository.NumberOrZero(null));
        Assert.Equal(12, M1DoctorDailyReportRepository.NumberOrZero(12m));
    }

    private sealed class ThrowingConnectionStringProvider : Infrastructure.IConnectionStringProvider
    {
        public string GetConnectionString() => throw new InvalidOperationException("must not open");
    }
}
