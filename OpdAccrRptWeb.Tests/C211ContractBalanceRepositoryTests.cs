using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Tests;

public sealed class C211ContractBalanceRepositoryTests
{
    [Theory]
    [InlineData("O", false, "OpdContractMrNoTbl", "IDX_OPDCONTRACTMRNOTBL_CHSTAT")]
    [InlineData("O", true, "OpdContractMrNoTbl", "IDX_OPDCONTRACTMRNOTBL_CHSTAT")]
    [InlineData("I", false, "IpdContractMrNoTbl", "IDX_IPDCONTRACTMRNOTBL_CHSTAT")]
    [InlineData("I", true, "IpdContractMrNoTbl", "IDX_IPDCONTRACTMRNOTBL_CHSTAT")]
    public void ReportSql_PreservesLegacyContract(string source, bool hasContract, string table, string hint)
    {
        var sql = C211Sql.Report(source, hasContract);

        Assert.Contains($"FROM {table} c", sql);
        Assert.Contains(hint, sql);
        Assert.Contains("chStat = '0' AND chIDate <= :end_date", sql);
        Assert.Contains("GROUP BY chFin2, chMrNo, chDate, chTime, chRoom, intNo", sql);
        Assert.Contains("HAVING SUM(intSelfAmt) <> 0 OR SUM(intClaimAmt) <> 0", sql);
        Assert.Contains("ORDER BY chFin2, chDate, chTime, chRoom, intNo, chMrNo", sql);
        Assert.Equal(hasContract, sql.Contains("chFin2 = :contract_code", StringComparison.Ordinal));
        Assert.DoesNotContain("start", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("chIDate, chType", sql);
    }

    [Fact]
    public void ReportSql_RejectsNonWhitelistedSource() =>
        Assert.Throws<ArgumentException>(() => C211Sql.Report("O; DROP TABLE X", false));

    [Fact]
    public void RocDate_UsesSevenAsciiDigits() =>
        Assert.Equal("1150831", C211ContractBalanceRepository.ToRocDate(new DateOnly(2026, 8, 31)));

    [Fact]
    public void ContractChoices_AreTrimmedDeduplicatedAndContainFixedCodes()
    {
        var values = C211ContractBalanceRepository.NormalizeChoices(
        [
            new(" TT ", "master"), new("AA", "甲"), new("TT", "duplicate"),
            new("UU", "其他"), new("VV", "維康記帳"), new("WW", "老人健檢"),
            new("XX", "老人照護鑑定"), new("YY", "殘障鑑定"), new("ZZ", "聯盟代檢")
        ]);

        Assert.Equal(values.OrderBy(value => value.Code, StringComparer.Ordinal), values);
        Assert.Single(values, value => value.Code == "TT");
        Assert.All(new[] { "TT", "UU", "VV", "WW", "XX", "YY", "ZZ" },
            code => Assert.Contains(values, value => value.Code == code));
    }

    [Fact]
    public void C211Sql_IsSelectOnly()
    {
        var sql = string.Join('\n', C211Sql.ContractChoices, C211Sql.OutpatientAll,
            C211Sql.OutpatientContract, C211Sql.InpatientAll, C211Sql.InpatientContract);
        foreach (var keyword in new[] { "INSERT", "UPDATE", "DELETE", "MERGE", "CREATE", "ALTER", "DROP" })
            Assert.DoesNotContain(keyword, sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetRowsAsync_PreCanceledTokenStopsBeforeOpeningOracleConnection()
    {
        var repository = new C211ContractBalanceRepository(new ThrowingConnectionStringProvider());
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => repository.GetRowsAsync(
            "O", new DateOnly(2026, 8, 31), null, source.Token));
    }

    private sealed class ThrowingConnectionStringProvider : Infrastructure.IConnectionStringProvider
    {
        public string GetConnectionString() => throw new InvalidOperationException("must not resolve");
    }
}
