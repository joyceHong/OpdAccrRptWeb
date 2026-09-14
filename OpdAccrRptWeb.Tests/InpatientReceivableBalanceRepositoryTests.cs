using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class InpatientReceivableBalanceRepositoryTests
{
    [Fact]
    public void Columns_ExposeEightC28FieldsInReportOrder()
    {
        var repository = new InpatientReceivableBalanceRepository(new FakeConnectionStringProvider());

        var columns = repository.GetColumns();

        Assert.Collection(columns,
            column => Assert.Equal(("medicalRecordNumber", "病歷號"), (column.Key, column.Label)),
            column => Assert.Equal(("admissionDate", "住院日期"), (column.Key, column.Label)),
            column => Assert.Equal(("admissionTime", "住院時間"), (column.Key, column.Label)),
            column => Assert.Equal(("room", "病房"), (column.Key, column.Label)),
            column => Assert.Equal(("admissionNumber", "住院序號"), (column.Key, column.Label)),
            column => Assert.Equal(("selfPayAmount", "自費金額"), (column.Key, column.Label)),
            column => Assert.Equal(("claimAmount", "申報金額"), (column.Key, column.Label)),
            column => Assert.Equal(("copaymentAmount", "部分負擔金額"), (column.Key, column.Label)));
    }

    [Fact]
    public void SourceSql_AggregatesActiveRowsByMedicalRecordAndAdmissionKey()
    {
        string sql = InpatientReceivableBalanceRepository.SourceSql;

        Assert.Contains("FROM IpdTranColeMrNoTbl", sql);
        Assert.Contains("chIDate <= :endDate", sql);
        Assert.Contains("chDC = '0' OR RTRIM(chDC) IS NULL", sql);
        Assert.Contains("SUM(NVL(intSelfAmt, 0))", sql);
        Assert.Contains("SUM(NVL(intClaimAmt, 0))", sql);
        Assert.Contains("SUM(NVL(intPartAmt, 0))", sql);
        Assert.Contains("GROUP BY chMrNo, chDate, chTime, chRoom, intNo", sql);
        Assert.DoesNotContain(":startDate", sql);
    }

    [Fact]
    public void CountAndPageSql_ShareAdmissionThresholdResult()
    {
        string sourceSql = InpatientReceivableBalanceRepository.SourceSql;

        Assert.Contains(sourceSql, InpatientReceivableBalanceRepository.CountSql);
        Assert.Contains(sourceSql, InpatientReceivableBalanceRepository.PageSql);
        Assert.Contains(
            "PARTITION BY AdmissionDate, AdmissionTime, Room, AdmissionNumber",
            sourceSql);
        Assert.Contains("ABS(TotalSelfPayAmount + TotalClaimAmount) > 10", sourceSql);
        Assert.Contains("ABS(TotalCopaymentAmount) > 10", sourceSql);
        Assert.DoesNotContain(">= 10", sourceSql);
    }

    [Fact]
    public void PageSql_AppliesStableOrderBeforeOraclePagination()
    {
        string sql = InpatientReceivableBalanceRepository.PageSql;

        Assert.Contains($"ORDER BY {InpatientReceivableBalanceRepository.StableOrder}", sql);
        Assert.EndsWith("MedicalRecordNumber", InpatientReceivableBalanceRepository.StableOrder);
        Assert.Contains("OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY", sql);
        Assert.True(sql.IndexOf("ORDER BY", StringComparison.Ordinal) <
                    sql.IndexOf("OFFSET", StringComparison.Ordinal));
    }

    [Fact]
    public void CreateParameters_PageTwoSizeThirty_UsesRocCutoffWithoutTimeSuffix()
    {
        object parameters = InpatientReceivableBalanceRepository.CreateParameters(new SearchReportCondition
        {
            EndDate = "1150831",
            PageNumber = 2,
            PageSize = 30
        });
        var values = ReadParameters(parameters);

        Assert.Equal("1150831", values["endDate"]);
        Assert.Equal(30L, values["rowOffset"]);
        Assert.Equal(30, values["pageSize"]);
    }

    [Fact]
    public void CreateParameters_MaximumPageNumber_DoesNotOverflowOffset()
    {
        object parameters = InpatientReceivableBalanceRepository.CreateParameters(new SearchReportCondition
        {
            EndDate = "1150831",
            PageNumber = int.MaxValue,
            PageSize = 50
        });

        Assert.Equal(((long)int.MaxValue - 1) * 50, ReadParameters(parameters)["rowOffset"]);
    }

    private static Dictionary<string, object?> ReadParameters(object parameters) =>
        parameters.GetType().GetProperties()
            .ToDictionary(property => property.Name, property => property.GetValue(parameters));

    private sealed class FakeConnectionStringProvider : IConnectionStringProvider
    {
        public string GetConnectionString() => "Data Source=unused";
    }
}
