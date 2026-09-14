using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Tests;

public sealed class C212BoneBankBalanceRepositoryTests
{
    [Fact]
    public void ReportSql_PreservesLegacyGroupingFilteringAndOrdering()
    {
        string sql = C212Sql.Report;

        Assert.Contains("FROM GenCONTRACT42MRNOTBL", sql);
        Assert.Contains("WHERE chIDate < :month_first_day", sql);
        Assert.Contains("WHERE chIDate BETWEEN :month_first_day AND :end_date", sql);
        Assert.Contains("GROUP BY chIDate, chMrNo, chPName", sql);
        Assert.Equal(2, Count(sql, "HAVING SUM(intAmt) <> 0"));
        Assert.Contains("ORDER BY sort_bucket, chIDate, chMrNo, chPName", sql);
        Assert.DoesNotContain("start", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(';', sql);
    }

    [Fact]
    public void ReportSql_IsSelectOnly()
    {
        foreach (string keyword in new[] { "INSERT", "UPDATE", "DELETE", "MERGE", "CREATE", "ALTER", "DROP" })
            Assert.DoesNotContain(keyword, C212Sql.Report, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    [InlineData("  A123  ", "A123")]
    public void NormalizeText_ReproducesLegacyNullAndTrimBehavior(object? input, string expected) =>
        Assert.Equal(expected, C212BoneBankBalanceRepository.NormalizeText(input));

    [Fact]
    public async Task GetRowsAsync_PreCanceledTokenStopsBeforeResolvingConnection()
    {
        var repository = new C212BoneBankBalanceRepository(new ThrowingConnectionStringProvider());
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.GetRowsAsync("1150911", "1150901", source.Token));
    }

    [Theory]
    [InlineData("115091")]
    [InlineData("１１５０９１１")]
    [InlineData("11509A1")]
    public async Task GetRowsAsync_RejectsInvalidBindBeforeResolvingConnection(string invalidDate)
    {
        var repository = new C212BoneBankBalanceRepository(new ThrowingConnectionStringProvider());

        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.GetRowsAsync(invalidDate, "1150901"));
    }

    private static int Count(string value, string pattern) =>
        value.Split(pattern, StringSplitOptions.None).Length - 1;

    private sealed class ThrowingConnectionStringProvider : Infrastructure.IConnectionStringProvider
    {
        public string GetConnectionString() => throw new InvalidOperationException("must not resolve");
    }
}
