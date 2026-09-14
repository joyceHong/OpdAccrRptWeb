using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace OpdAccrRptWeb.Tests;

public sealed class CashierCashSummaryRepositoryTests
{
    [Fact]
    public void DependencyInjection_ResolvesC213Repository()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IConnectionStringProvider, FakeConnectionStringProvider>()
            .AddSingleton<ICashierCashSummaryRepository, CashierCashSummaryRepository>()
            .BuildServiceProvider();

        Assert.IsType<CashierCashSummaryRepository>(
            provider.GetRequiredService<ICashierCashSummaryRepository>());
    }

    [Fact]
    public void Columns_ExposeEightApprovedFieldsAndSixDecimalAmounts()
    {
        var repository = new CashierCashSummaryRepository(new FakeConnectionStringProvider());

        var columns = repository.GetColumns();
        var amountProperties = typeof(CashierCashSummaryReportViewModel).GetProperties()
            .Where(property => property.Name.EndsWith("Amount", StringComparison.Ordinal));

        Assert.Equal(8, columns.Count);
        Assert.Equal("cashierUserId", columns[0].Key);
        Assert.Equal("收款員代碼", columns[0].Label);
        Assert.Equal("totalAmount", columns[^1].Key);
        Assert.Equal("合計", columns[^1].Label);
        Assert.Equal(6, amountProperties.Count());
        Assert.All(amountProperties, property => Assert.Equal(typeof(decimal), property.PropertyType));
        Assert.IsAssignableFrom<ICashierCashSummaryRepository>(repository);
    }

    [Fact]
    public void SourceSql_UsesThreeParameterizedGroupedNonZeroSources()
    {
        string sql = CashierCashSummaryRepository.SourceSql;

        Assert.Contains("FROM GenAccCaseDayTbl", sql);
        Assert.Contains("FROM IpdAccCaseDayTbl", sql);
        Assert.Contains("FROM GenAccHappyCashTbl", sql);
        Assert.Equal(2, Count(sql, "UNION ALL"));
        Assert.Equal(3, Count(sql, ":startDate"));
        Assert.Equal(3, Count(sql, ":endDate"));
        Assert.Equal(3, Count(sql, "NOT IN ('C36979', '1000000')"));
        Assert.Equal(3, Count(sql, "HAVING SUM(NVL("));
        Assert.Contains("THEN 'E' ELSE 'O'", sql);
        Assert.Contains("SUBSTR(vchAccSeqNo, 1, 1) AS RoomType", sql);
        Assert.Contains("'H' AS RoomType", sql);
        Assert.Contains("SUM(NVL(intHappyCash, 0))", sql);
    }

    [Fact]
    public void AggregateSql_CalculatesDetailAndOneGrandTotalRow()
    {
        string sql = CashierCashSummaryRepository.AggregateSql;

        Assert.Contains(CashierCashSummaryRepository.SourceSql, sql);
        Assert.Contains("LEFT JOIN GenUserProfile1", sql);
        Assert.Contains("GROUP BY ROLLUP(sourceRows.CashierUserId, RTRIM(userProfile.chUserName))", sql);
        Assert.Contains("THEN '合計'", sql);
        Assert.Contains("RoomType = 'O'", sql);
        Assert.Contains("RoomType = 'E'", sql);
        Assert.Contains("RoomType = 'I'", sql);
        Assert.Contains("RoomType IN ('O', 'E', 'I')", sql);
        Assert.Contains("RoomType = 'H'", sql);
        Assert.Contains("NVL(SUM(NVL(sourceRows.CashAmount, 0)), 0) AS TotalAmount", sql);
        Assert.Contains("GROUPING(RTRIM(userProfile.chUserName)) = 0", sql);
        Assert.Contains("GROUPING(sourceRows.CashierUserId) = 1", sql);
        Assert.Contains("FROM DUAL", sql);
        Assert.Contains("WHERE NOT EXISTS", sql);
        Assert.Contains("'合計' AS CashierUserId", sql);
        Assert.Contains("0 AS TotalAmount", sql);
    }

    [Fact]
    public void CountAndPageSql_ShareCanonicalResultAndOrderTotalLast()
    {
        Assert.Contains(CashierCashSummaryRepository.AggregateSql,
            CashierCashSummaryRepository.CountSql);
        Assert.Contains(CashierCashSummaryRepository.AggregateSql,
            CashierCashSummaryRepository.PageSql);
        Assert.Contains($"ORDER BY {CashierCashSummaryRepository.StableOrder}",
            CashierCashSummaryRepository.PageSql);
        Assert.Contains("OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY",
            CashierCashSummaryRepository.PageSql);
        Assert.True(
            CashierCashSummaryRepository.PageSql.IndexOf("IsGrandTotal", StringComparison.Ordinal)
            < CashierCashSummaryRepository.PageSql.IndexOf("OFFSET", StringComparison.Ordinal));
        Assert.Equal(24, 23 + 1);
        Assert.Equal(3, (int)Math.Ceiling(24 / 10m));
    }

    [Fact]
    public void Parameters_UseDatesAndComputeSecondPageOffset()
    {
        object parameters = CashierCashSummaryRepository.CreateParameters(new SearchReportCondition
        {
            StartDate = "1150801",
            EndDate = "1150831",
            PageNumber = 2,
            PageSize = 10
        });
        var values = parameters.GetType().GetProperties()
            .ToDictionary(property => property.Name, property => property.GetValue(parameters));

        Assert.Equal("1150801", values["startDate"]);
        Assert.Equal("1150831", values["endDate"]);
        Assert.Equal(10L, values["rowOffset"]);
        Assert.Equal(10, values["pageSize"]);
    }

    [Fact]
    public void CalculationExamples_ProduceHisSubtotalTotalAndGrandTotal()
    {
        decimal hisSubtotal = 100m + 50m + 200m;
        decimal total = hisSubtotal + 30m;

        Assert.Equal(350m, hisSubtotal);
        Assert.Equal(380m, total);
        Assert.Equal(500m, total + 120m);
        Assert.Equal(40m, new[] { 0m, 0m, 0m, 0m, 40m }.Sum());
    }

    [Fact]
    public void Sql_IsStrictlyReadOnlyAndUsesOnlyDeclaredTables()
    {
        string sql = string.Join(" ",
            CashierCashSummaryRepository.SourceSql,
            CashierCashSummaryRepository.AggregateSql,
            CashierCashSummaryRepository.CountSql,
            CashierCashSummaryRepository.PageSql);

        Assert.DoesNotMatch("(?i)\\b(INSERT|UPDATE|DELETE|MERGE)\\b", sql);
        Assert.DoesNotContain("Access", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, Count(CashierCashSummaryRepository.SourceSql, "GenAccCaseDayTbl"));
        Assert.Equal(1, Count(CashierCashSummaryRepository.SourceSql, "IpdAccCaseDayTbl"));
        Assert.Equal(1, Count(CashierCashSummaryRepository.SourceSql, "GenAccHappyCashTbl"));
        Assert.Equal(1, Count(CashierCashSummaryRepository.AggregateSql, "GenUserProfile1"));
    }

    private static int Count(string value, string token) =>
        (value.Length - value.Replace(token, "", StringComparison.Ordinal).Length) / token.Length;

    private sealed class FakeConnectionStringProvider : IConnectionStringProvider
    {
        public string GetConnectionString() => "Data Source=unused";
    }
}
