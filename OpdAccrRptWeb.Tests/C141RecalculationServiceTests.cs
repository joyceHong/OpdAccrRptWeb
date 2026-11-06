using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpdAccrRptWeb.Tests;

public sealed class C141RecalculationServiceTests
{
    [Fact]
    public void CalculateClaimFlaggedCases_UsesCurrentDayTotalsForEveryClaimFlaggedCase()
    {
        var first = new C141RecalculationCase(
            new("1150101", "1", "A12", 7m), "30", "000", "01P01");
        var second = new C141RecalculationCase(
            new("1150102", "2", "B34", 8m), "20", "100", "3J01");
        var repository = new FakeRepository(
            [first, second],
            new C141RecalculationTotals(100.4m, 5m, 80.6m));
        var service = new C141RecalculationService(repository, NullLogger<C141RecalculationService>.Instance);

        IReadOnlyList<C141CalculatedCase> results =
            service.CalculateClaimFlaggedCases("1151013", "1151013");

        Assert.Equal(2, results.Count);
        Assert.All(results, result => Assert.True(result.IsCurrentAccountingDay));
        Assert.Equal(2, repository.TotalQueries.Count);
        Assert.All(repository.TotalQueries, query =>
        {
            Assert.Equal("1151013", query.AccountingDateRoc);
            Assert.True(query.IsCurrentAccountingDay);
        });
        Assert.Equal(100.4m, results[0].Totals.AllTot1);
        Assert.Equal(5m, results[0].Totals.AllTot2);
        Assert.Equal(80.6m, results[0].Totals.AllTot3);
    }

    [Fact]
    public void CalculateClaimFlaggedCases_UsesHistoricalTotalsWhenAccountingDateDiffersFromOracleToday()
    {
        var item = new C141RecalculationCase(
            new("1150101", "1", "A12", 7m), "30", "000", "01P01");
        var repository = new FakeRepository([item], new C141RecalculationTotals(null, 2m, null));
        var service = new C141RecalculationService(repository, NullLogger<C141RecalculationService>.Instance);

        C141CalculatedCase result = Assert.Single(
            service.CalculateClaimFlaggedCases("1151012", "1151013"));

        Assert.False(result.IsCurrentAccountingDay);
        Assert.Equal((decimal?)null, result.Totals.AllTot1);
        Assert.Equal(2m, result.Totals.AllTot2);
        Assert.Equal("1151012", Assert.Single(repository.TotalQueries).AccountingDateRoc);
        Assert.False(repository.TotalQueries[0].IsCurrentAccountingDay);
    }

    [Fact]
    public void Recalculate_UpdatesOnlyWhenFirstOrSecondTotalIsPositive()
    {
        var item = new C141RecalculationCase(
            new("1150101", "1", "A12", 7m), "20", "000", "3J01");
        var repository = new FakeRepository([item], new C141RecalculationTotals(100.4m, 0m, 80.6m));
        var service = new C141RecalculationService(repository, NullLogger<C141RecalculationService>.Instance);

        C141RecalculationResult result = service.Recalculate("1151012", "1151013");

        Assert.Equal(new C141RecalculationResult(1, 1, 0, 0), result);
        Assert.Equal((100m, 81m), Assert.Single(repository.Updates));
    }

    [Fact]
    public void Recalculate_LeavesOldAmountsWhenOnlyThirdTotalIsPositive()
    {
        var item = new C141RecalculationCase(
            new("1150101", "1", "A12", 7m), "20", "000", "3J01");
        var repository = new FakeRepository([item], new C141RecalculationTotals(0m, 0m, 900m));
        var service = new C141RecalculationService(repository, NullLogger<C141RecalculationService>.Instance);

        C141RecalculationResult result = service.Recalculate("1151012", "1151013");

        Assert.Equal(new C141RecalculationResult(1, 0, 1, 0), result);
        Assert.Empty(repository.Updates);
    }

    [Fact]
    public void Recalculate_ContinuesAfterOneCaseFailsAndReportsFailure()
    {
        var failedCase = new C141RecalculationCase(
            new("1150101", "1", "A12", 7m), "20", "000", "3J01");
        var nextCase = new C141RecalculationCase(
            new("1150102", "2", "B34", 8m), "20", "000", "3J02");
        var repository = new FakeRepository(
            [failedCase, nextCase], new C141RecalculationTotals(100m, 0m, 80m))
        {
            FailingKey = failedCase.Key
        };
        var logger = new CapturingLogger<C141RecalculationService>();
        var service = new C141RecalculationService(repository, logger);

        C141RecalculationResult result = service.Recalculate("1151012", "1151013");

        Assert.Equal(new C141RecalculationResult(2, 1, 0, 1), result);
        Assert.Equal((100m, 80m), Assert.Single(repository.Updates));
        Assert.Equal(2, repository.TotalQueries.Count);
        CapturedLog entry = Assert.Single(logger.Entries);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Error, entry.Level);
        Assert.IsType<System.Data.DataException>(entry.Exception);
        Assert.DoesNotContain(failedCase.Key.AdmissionDate, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RecalculationSql_PreservesSeparateCurrentAndHistoricalOrderAndDrugRules()
    {
        string current = C141RecalculationSql.CurrentDayTotals;
        string historical = C141RecalculationSql.HistoricalDayTotals;

        Assert.Contains("FROM IPDBasicTbl", C141RecalculationSql.ClaimFlaggedCases, StringComparison.Ordinal);
        Assert.Contains("WHERE chOp1ClmFlg='1'", C141RecalculationSql.ClaimFlaggedCases, StringComparison.Ordinal);
        Assert.DoesNotContain(":mrNo", C141RecalculationSql.ClaimFlaggedCases, StringComparison.Ordinal);
        Assert.DoesNotContain(":bedNo", C141RecalculationSql.ClaimFlaggedCases, StringComparison.Ordinal);
        Assert.Contains("SUM(rlOp4sub1)", current, StringComparison.Ordinal);
        Assert.Contains("SUM(rlOp4sub2)", current, StringComparison.Ordinal);
        Assert.Contains("SUM(rlop4amt1)", current, StringComparison.Ordinal);
        Assert.Contains("SUBSTR(chOp4Dct,1,2) <> '49'", current, StringComparison.Ordinal);
        Assert.Contains("NVL(chOp4Idate,' ') > ' '", current, StringComparison.Ordinal);
        Assert.DoesNotContain("chOp3SPay", current, StringComparison.Ordinal);
        Assert.DoesNotContain(":accountDate2359", current, StringComparison.Ordinal);
        Assert.Contains("chOp4dcdate > :accountDate2359", historical, StringComparison.Ordinal);
        Assert.Contains("chOp3dcdate > :accountDate2359", historical, StringComparison.Ordinal);
        Assert.Contains("chOp4Idate <= :accountDate2359", historical, StringComparison.Ordinal);
        Assert.Contains("chOp3Idate <= :accountDate2359", historical, StringComparison.Ordinal);
        Assert.DoesNotContain("SUBSTR(chOp4Dct,1,2) <> '49'", historical, StringComparison.Ordinal);
        Assert.DoesNotContain("NVL(chOp4Idate,' ')", historical, StringComparison.Ordinal);
    }

    [Fact]
    public void RecalculationParameters_BindFixedKeysAndHistoricalCutoff()
    {
        using var command = new Oracle.ManagedDataAccess.Client.OracleCommand();
        var key = new C141AdmissionKey("1150101", "1", "A12", 7m);

        C141RecalculationRepository.AddAdmissionKeyParameters(command, key);
        C141OracleParameters.AddFixedChar(command, "accountDate2359", "11501012359", 11);

        Assert.Equal(Oracle.ManagedDataAccess.Client.OracleDbType.Char, command.Parameters["opDate"].OracleDbType);
        Assert.Equal(7, command.Parameters["opDate"].Size);
        Assert.Equal("1150101", command.Parameters["opDate"].Value);
        Assert.Equal(Oracle.ManagedDataAccess.Client.OracleDbType.Char, command.Parameters["opTime"].OracleDbType);
        Assert.Equal(1, command.Parameters["opTime"].Size);
        Assert.Equal("1", command.Parameters["opTime"].Value);
        Assert.Equal(Oracle.ManagedDataAccess.Client.OracleDbType.Char, command.Parameters["opRoom"].OracleDbType);
        Assert.Equal(6, command.Parameters["opRoom"].Size);
        Assert.Equal("A12   ", command.Parameters["opRoom"].Value);
        Assert.Equal(Oracle.ManagedDataAccess.Client.OracleDbType.Decimal, command.Parameters["opNo"].OracleDbType);
        Assert.Equal(Oracle.ManagedDataAccess.Client.OracleDbType.Char, command.Parameters["accountDate2359"].OracleDbType);
        Assert.Equal(11, command.Parameters["accountDate2359"].Size);
    }

    private sealed class FakeRepository(
        IReadOnlyList<C141RecalculationCase> cases,
        C141RecalculationTotals totals) : IC141RecalculationRepository
    {
        public List<(string AccountingDateRoc, bool IsCurrentAccountingDay)> TotalQueries { get; } = [];
        public List<(decimal Receivable, decimal Insured)> Updates { get; } = [];
        public C141AdmissionKey? FailingKey { get; init; }

        public IReadOnlyList<C141RecalculationCase> GetClaimFlaggedCases(CancellationToken cancellationToken) =>
            cases;

        public C141RecalculationTotals GetTotals(
            C141AdmissionKey key,
            string accountingDateRoc,
            bool isCurrentAccountingDay,
            CancellationToken cancellationToken)
        {
            TotalQueries.Add((accountingDateRoc, isCurrentAccountingDay));
            if (key == FailingKey)
                throw new System.Data.DataException("個案計算失敗。");
            return totals;
        }

        public void UpdateAmounts(C141AdmissionKey key, decimal receivableAmount,
            decimal insuredReceivableAmount, CancellationToken cancellationToken) =>
            Updates.Add((receivableAmount, insuredReceivableAmount));
    }
}
