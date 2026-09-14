using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C21AccountingSummaryRepositoryTests
{
    [Fact]
    public void SourceAmount_AcceptsOracleCharRoomTypeForDapperMaterialization()
    {
        var type = typeof(C21SourceAmount);

        Assert.NotNull(type.GetConstructor(Type.EmptyTypes));
        Assert.Equal(typeof(string), type.GetProperty(nameof(C21SourceAmount.RoomType))!.PropertyType);
        Assert.True(type.GetProperty(nameof(C21SourceAmount.RoomType))!.CanWrite);
    }

    [Fact]
    public void CreateParameters_ConvertsGregorianDatesAndNormalizesBillingCode()
    {
        var parameters = C21AccountingSummaryRepository.CreateParameters(new SearchReportCondition
        {
            StartDate = "2026-09-08",
            EndDate = "2026-09-09",
            BillingCode = " 49 "
        });

        Assert.Equal("1150908", Property(parameters, "startDate"));
        Assert.Equal("1150909", Property(parameters, "endDate"));
        Assert.Equal("49", Property(parameters, "billingCode"));
    }

    [Fact]
    public void QuerySql_UsesWhitelistedTablesIdentityRulesAndBindParameters()
    {
        Assert.Contains("FROM OpdTranColeTbl", C21AccountingSummaryRepository.OutpatientSourceSql);
        Assert.Contains("1000901", C21AccountingSummaryRepository.OutpatientSourceSql);
        Assert.Contains("FROM IpdTranColeTbl", C21AccountingSummaryRepository.InpatientSourceSql);
        Assert.Contains("DECODE(chOp1Fin1, '35', '30'", C21AccountingSummaryRepository.InpatientSourceSql);
        Assert.Contains(":startDate", C21AccountingSummaryRepository.OutpatientSourceSql);
        Assert.Contains(":billingCode", C21AccountingSummaryRepository.InpatientSourceSql);
    }

    [Fact]
    public void RebuildSql_ContainsExactlyTwentyNamedBranchesAndOneIntegrityCheck()
    {
        var sql = string.Join("\n", C21RebuildSql.Commands);
        for (var branch = 1; branch <= 20; branch++)
        {
            Assert.Equal(1, Count(sql, $"B{branch:D2}"));
        }
        Assert.Contains("rlOp3DrgTot", C21RebuildSql.InsertRoom23Sql);
        Assert.Contains("SUM(self_amt)*-1", C21RebuildSql.InsertMrNoCSql);
        Assert.Contains(":SDate", C21RebuildSql.IntegritySql);
        Assert.DoesNotContain("COMMIT", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExecuteAtomically_ThirdCommandFailureRollsBackWithoutCommit()
    {
        var executed = new List<string>();
        var committed = false;
        var rolledBack = false;

        Assert.Throws<InvalidOperationException>(() => C21AccountingSummaryRepository.ExecuteAtomically(
            ["room23", "room45", "mrno-d", "mrno-c"],
            command =>
            {
                executed.Add(command);
                if (command == "mrno-d") throw new InvalidOperationException("failed");
            },
            () => 0,
            () => committed = true,
            () => rolledBack = true));

        Assert.Equal(["room23", "room45", "mrno-d"], executed);
        Assert.False(committed);
        Assert.True(rolledBack);
    }

    [Fact]
    public void ExecuteAtomically_IntegritySuccessCommitsExactlyOnce()
    {
        var commits = 0;
        var rollbacks = 0;
        C21AccountingSummaryRepository.ExecuteAtomically(
            ["room23", "room45", "mrno-d", "mrno-c"], _ => { }, () => 0,
            () => commits++, () => rollbacks++);

        Assert.Equal(1, commits);
        Assert.Equal(0, rollbacks);
    }

    private static object? Property(object value, string name) =>
        value.GetType().GetProperty(name)!.GetValue(value);

    private static int Count(string value, string token) =>
        (value.Length - value.Replace(token, string.Empty, StringComparison.Ordinal).Length) / token.Length;

    public static IEnumerable<object[]> BranchFixtures()
    {
        for (var branch = 1; branch <= 20; branch++)
        {
            yield return [branch];
        }
    }

    [Theory]
    [MemberData(nameof(BranchFixtures))]
    public void RebuildSql_EachBranchFixtureHasOneExplicitContractMarker(int branch)
    {
        var sql = string.Join("\n", C21RebuildSql.Commands);
        Assert.Equal(1, Count(sql, $"B{branch:D2}"));
        Assert.Contains(":SDate", sql);
        if (branch is 2 or 4 or 6 or 9 or 10 or 12 or 15 or 16 or 19 or 20)
        {
            var marker = $"B{branch:D2}";
            var start = sql.IndexOf(marker, StringComparison.Ordinal);
            var nextMarker = branch == 20 ? null : $"B{branch + 1:D2}";
            var next = nextMarker is null
                ? -1
                : sql.IndexOf(nextMarker, start + marker.Length, StringComparison.Ordinal);
            var branchSql = sql[start..(next < 0 ? sql.Length : next)];
            Assert.Contains("-1", branchSql);
        }
    }
}
