using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class AssistiveDeviceDepositBalanceRepositoryTests
{
    [Fact]
    public void Columns_ExposeSixC27FieldsInReportOrder()
    {
        var repository = new AssistiveDeviceDepositBalanceRepository(new FakeConnectionStringProvider());

        var columns = repository.GetColumns();

        Assert.Collection(columns,
            column => Assert.Equal(("effectiveYearMonth", "入帳年月"), (column.Key, column.Label)),
            column => Assert.Equal(("medicalRecordNumber", "病歷號"), (column.Key, column.Label)),
            column => Assert.Equal(("patientName", "病患姓名"), (column.Key, column.Label)),
            column => Assert.Equal(("financialCategory", "身分別"), (column.Key, column.Label)),
            column => Assert.Equal(("assistiveDeviceDepositBalance", "輔具保證金餘額"), (column.Key, column.Label)),
            column => Assert.Equal(("sequenceNumber", "序號"), (column.Key, column.Label)));
        Assert.Equal(typeof(decimal),
            typeof(AssistiveDeviceDepositBalanceReportViewModel)
                .GetProperty(nameof(AssistiveDeviceDepositBalanceReportViewModel.AssistiveDeviceDepositBalance))!
                .PropertyType);
    }

    [Fact]
    public void CountAndPageSql_ShareC27CutoffBalanceSelection()
    {
        string sourceSql = AssistiveDeviceDepositBalanceRepository.SourceSql;

        Assert.Contains(sourceSql, AssistiveDeviceDepositBalanceRepository.CountSql);
        Assert.Contains(sourceSql, AssistiveDeviceDepositBalanceRepository.PageSql);
        Assert.Contains("FROM OpdAidPayTbl", sourceSql);
        Assert.Contains("chOp4IDate <= :endDateBoundary", sourceSql);
        Assert.Contains("chOp4DCDate > :endDateBoundary", sourceSql);
        Assert.Contains("RTRIM(chOp4DCDate) IS NULL", sourceSql);
        Assert.Contains("chOp4DCDate = '0'", sourceSql);
        Assert.Contains("chOp1MrNo NOT IN ('C36979', '1000000')", sourceSql);
        Assert.Contains("chOp4DC IN ('0', '2')", sourceSql);
        Assert.DoesNotContain("'1'", sourceSql);
        Assert.Contains("rlOp4Sub1 <> 0", sourceSql);
        Assert.DoesNotContain(":startDate", sourceSql);
    }

    [Fact]
    public void PageSql_AppliesStableOrderBeforeOraclePagination()
    {
        string sql = AssistiveDeviceDepositBalanceRepository.PageSql;

        Assert.Contains($"ORDER BY {AssistiveDeviceDepositBalanceRepository.StableOrder}", sql);
        Assert.EndsWith("SequenceNumber", AssistiveDeviceDepositBalanceRepository.StableOrder);
        Assert.Contains("OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY", sql);
        Assert.True(sql.IndexOf("ORDER BY", StringComparison.Ordinal) <
                    sql.IndexOf("OFFSET", StringComparison.Ordinal));
    }

    [Fact]
    public void CreateParameters_PageTwoSizeThirty_UsesSpecifiedCutoffExampleAndLongOffset()
    {
        object parameters = AssistiveDeviceDepositBalanceRepository.CreateParameters(new SearchReportCondition
        {
            EndDate = "1150831",
            PageNumber = 2,
            PageSize = 30
        });
        var values = ReadParameters(parameters);

        Assert.Equal("11508319999", values["endDateBoundary"]);
        Assert.Equal(30L, values["rowOffset"]);
        Assert.Equal(30, values["pageSize"]);
    }

    [Fact]
    public void CreateParameters_MaximumPageNumber_DoesNotOverflowOffset()
    {
        object parameters = AssistiveDeviceDepositBalanceRepository.CreateParameters(new SearchReportCondition
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
