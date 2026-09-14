using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace OpdAccrRptWeb.Tests;

public sealed class OutpatientReceivableBalanceRepositoryTests
{
    [Fact]
    public void DependencyInjection_ResolvesC214Repository()
    {
        using ServiceProvider provider = new ServiceCollection()
            .AddSingleton<IConnectionStringProvider, FakeConnectionStringProvider>()
            .AddSingleton<IOutpatientReceivableBalanceRepository, OutpatientReceivableBalanceRepository>()
            .BuildServiceProvider();

        Assert.IsType<OutpatientReceivableBalanceRepository>(
            provider.GetRequiredService<IOutpatientReceivableBalanceRepository>());
        string programPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "Program.cs"));
        Assert.Contains(
            "AddSingleton<IOutpatientReceivableBalanceRepository, OutpatientReceivableBalanceRepository>()",
            File.ReadAllText(programPath));
    }

    [Fact]
    public void Columns_ExposeThreeC214FieldsAndDecimalAmount()
    {
        var repository = new OutpatientReceivableBalanceRepository(
            new FakeConnectionStringProvider());

        var columns = repository.GetColumns();

        Assert.Collection(columns,
            column => Assert.Equal(("medicalRecordNumber", "病歷號"), (column.Key, column.Label)),
            column => Assert.Equal(("visitDate", "就診日期"), (column.Key, column.Label)),
            column => Assert.Equal(("receivableAmount", "應收金額"), (column.Key, column.Label)));
        Assert.Equal(typeof(decimal),
            typeof(OutpatientReceivableBalanceReportViewModel)
                .GetProperty(nameof(OutpatientReceivableBalanceReportViewModel.ReceivableAmount))!
                .PropertyType);
        Assert.IsAssignableFrom<IOutpatientReceivableBalanceRepository>(repository);
        Assert.True(ReceivableBalanceTypes.IsSupported(ReceivableBalanceTypes.SelfPay));
        Assert.True(ReceivableBalanceTypes.IsSupported(ReceivableBalanceTypes.Insurance));
        Assert.False(ReceivableBalanceTypes.IsSupported("Unknown"));
    }

    [Fact]
    public void SourceSql_UsesFixedParameterizedDatesActiveRowsAndThreshold()
    {
        string sql = OutpatientReceivableBalanceRepository.GetSourceSql(
            ReceivableBalanceTypes.SelfPay);

        Assert.Contains("FROM OpdRecRpt_PDebtDM", sql);
        Assert.Contains("chOp1Date BETWEEN :startDate AND :endDate", sql);
        Assert.Contains("chOp1Date2 >= :startDate", sql);
        Assert.Contains("chDC = '0' OR RTRIM(chDC) IS NULL", sql);
        Assert.Contains("GROUP BY chOp1MrNo, chOp1Date2", sql);
        Assert.Contains("HAVING ABS(SUM(NVL(rlOp1Sub6, 0))) > 10", sql);
        Assert.Equal("1040101", OutpatientReceivableBalanceRepository.FixedStartDate);
    }

    [Fact]
    public void SourceSql_SelectsOnlyControlledIdentitiesForEachBalanceType()
    {
        string selfPaySql = OutpatientReceivableBalanceRepository.GetSourceSql(
            ReceivableBalanceTypes.SelfPay);
        string insuranceSql = OutpatientReceivableBalanceRepository.GetSourceSql(
            ReceivableBalanceTypes.Insurance);

        Assert.Contains("chOp4PFin1 IN ('01', '35')", selfPaySql);
        Assert.DoesNotContain("chOp4PFin1 IN ('30')", selfPaySql);
        Assert.Contains("chOp4PFin1 IN ('30')", insuranceSql);
        Assert.DoesNotContain("chOp4PFin1 IN ('01', '35')", insuranceSql);
        Assert.Throws<ArgumentException>(() =>
            OutpatientReceivableBalanceRepository.GetSourceSql("Unknown"));
    }

    [Theory]
    [InlineData(10, false)]
    [InlineData(-10, false)]
    [InlineData(10.01, true)]
    [InlineData(-10.01, true)]
    public void AbsoluteThreshold_MatchesSpecification(decimal amount, bool included)
    {
        Assert.Equal(included, Math.Abs(amount) > 10m);
    }

    [Fact]
    public void CountAndPageSql_ShareCanonicalResultAndPageAfterStableOrder()
    {
        string source = OutpatientReceivableBalanceRepository.GetSourceSql(
            ReceivableBalanceTypes.SelfPay);
        string count = OutpatientReceivableBalanceRepository.GetCountSql(
            ReceivableBalanceTypes.SelfPay);
        string page = OutpatientReceivableBalanceRepository.GetPageSql(
            ReceivableBalanceTypes.SelfPay);

        Assert.Contains(source, count);
        Assert.Contains(source, page);
        Assert.Contains("MedicalRecordNumber, VisitDate, ReceivableAmount", page);
        Assert.Contains($"ORDER BY {OutpatientReceivableBalanceRepository.StableOrder}", page);
        Assert.Contains("OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY", page);
        Assert.True(page.IndexOf("ORDER BY", StringComparison.Ordinal)
            < page.IndexOf("OFFSET", StringComparison.Ordinal));
    }

    [Fact]
    public void Parameters_PageTwoSizeTen_UseFixedStartAndRocCutoff()
    {
        object parameters = OutpatientReceivableBalanceRepository.CreateParameters(
            new SearchReportCondition
            {
                EndDate = "1150831",
                PageNumber = 2,
                PageSize = 10
            });
        var values = parameters.GetType().GetProperties()
            .ToDictionary(property => property.Name, property => property.GetValue(parameters));

        Assert.Equal("1040101", values["startDate"]);
        Assert.Equal("1150831", values["endDate"]);
        Assert.Equal(10L, values["rowOffset"]);
        Assert.Equal(10, values["pageSize"]);
    }

    [Fact]
    public void Sql_IsReadOnlyAndUsesOnlyDeclaredTable()
    {
        string sql = string.Join(" ",
            OutpatientReceivableBalanceRepository.GetCountSql(ReceivableBalanceTypes.SelfPay),
            OutpatientReceivableBalanceRepository.GetPageSql(ReceivableBalanceTypes.Insurance));

        Assert.DoesNotMatch("(?i)\\b(INSERT|UPDATE|DELETE|MERGE)\\b", sql);
        Assert.DoesNotContain("C24", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(2, Count(sql, "OpdRecRpt_PDebtDM"));
    }

    private static int Count(string value, string token) =>
        (value.Length - value.Replace(token, "", StringComparison.Ordinal).Length) / token.Length;

    private sealed class FakeConnectionStringProvider : IConnectionStringProvider
    {
        public string GetConnectionString() => "Data Source=unused";
    }
}
