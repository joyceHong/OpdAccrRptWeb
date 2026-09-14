using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class CashierCashRepositoryTests
{
    [Fact]
    public void Columns_ExposeSeventeenApprovedFieldsAndNonNullableAmounts()
    {
        var columns = new CashierCashRepository(new FakeConnectionStringProvider()).GetColumns();
        Assert.Equal(17, columns.Count);
        Assert.Equal("cashierDate", columns[0].Key);
        Assert.Equal("patientBankDebitAmount", columns[^1].Key);

        var amountProperties = typeof(CashierCashReportViewModel).GetProperties()
            .Where(property => property.Name.EndsWith("Amount", StringComparison.Ordinal));
        Assert.All(amountProperties, property => Assert.Equal(typeof(decimal), property.PropertyType));
    }

    [Fact]
    public void SourceSql_UsesThreeParameterizedSourcesAndNullNormalization()
    {
        string sql = CashierCashRepository.SourceSql;
        Assert.Contains("FROM GenAccCaseDayTbl", sql);
        Assert.Contains("FROM IpdAccCaseDayTbl", sql);
        Assert.Contains("FROM IpdContractAccTbl", sql);
        Assert.Equal(2, Count(sql, "UNION ALL"));
        Assert.Contains("LEFT JOIN GenUserProfile1", sql);
        Assert.Contains(":startDate", sql);
        Assert.Contains(":endDate", sql);
        Assert.Contains(":cashierUserId", sql);
        Assert.Contains("NOT IN ('C36979', '1000000')", sql);
        Assert.Contains("NVL(ac.intAccCash10, 0)", sql);
        Assert.DoesNotContain("CashPrintTbl", sql);
    }

    [Fact]
    public void AggregateAndPageSql_AggregateBeforeStablePagination()
    {
        Assert.Contains("GROUP BY CashierDate, CashierUserId, CounterName, SourceType, Cash9Note", CashierCashRepository.AggregateSql);
        Assert.Contains("OriginalCounter = '繳欠'", CashierCashRepository.AggregateSql);
        Assert.Contains(CashierCashRepository.AggregateSql, CashierCashRepository.CountSql);

        string cashierSql = CashierCashRepository.GetPageSql(CashierCashSortTypes.Cashier);
        string encounterSql = CashierCashRepository.GetPageSql(CashierCashSortTypes.Encounter);
        Assert.Contains($"ORDER BY {CashierCashRepository.CashierOrder}", cashierSql);
        Assert.Contains($"ORDER BY {CashierCashRepository.EncounterOrder}", encounterSql);
        Assert.Contains("OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY", cashierSql);
        Assert.True(cashierSql.IndexOf("GROUP BY", StringComparison.Ordinal) < cashierSql.IndexOf("OFFSET", StringComparison.Ordinal));
    }

    [Fact]
    public void CreateParameters_TrimsCashierAndComputesOffset()
    {
        object parameters = CashierCashRepository.CreateParameters(new SearchReportCondition
        {
            StartDate = "1150801", EndDate = "1150803", CashierUserId = " A123 ",
            PageNumber = 2, PageSize = 30
        });
        var values = parameters.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(parameters));
        Assert.Equal("1150801", values["startDate"]);
        Assert.Equal("A123", values["cashierUserId"]);
        Assert.Equal(30L, values["rowOffset"]);
    }

    [Fact]
    public void PaymentExample_SumsOneHundredAndTwoHundredFifty()
    {
        Assert.Equal(350m, new[] { 100m, 250m }.Sum());
        Assert.Contains("SUM(CASE WHEN OriginalCounter = '繳欠' THEN 0 ELSE Cash1 END)", CashierCashRepository.AggregateSql);
    }

    private static int Count(string value, string token) =>
        (value.Length - value.Replace(token, "", StringComparison.Ordinal).Length) / token.Length;

    private sealed class FakeConnectionStringProvider : IConnectionStringProvider
    {
        public string GetConnectionString() => "Data Source=unused";
    }
}
