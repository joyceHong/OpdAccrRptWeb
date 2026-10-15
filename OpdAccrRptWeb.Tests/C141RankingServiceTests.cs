using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Tests;

public sealed class C141RankingServiceTests
{
    [Fact]
    public void Query_UsesStoredPayTotalAndAppliesStrictThresholdWithStableOrder()
    {
        var repository = new FakeRepository(
        [
            Source("1150102", "1", "B34", 8, 200m, 65m),
            Source("1150101", "1", "A12", 7, 200m, 65m),
            Source("1150103", "1", "C56", 9, 200m, 70m)
        ]);
        var service = new C141RankingService(repository);

        IReadOnlyList<C141RankingRow> result = service.Query(new(null, null, 130m));

        Assert.Equal(2, result.Count);
        Assert.Equal(135m, result[0].NetAmount);
        Assert.Equal(65m, result[0].PayTotal);
        Assert.Equal("1150101", result[0].Key.AdmissionDate);
        Assert.Equal("1150102", result[1].Key.AdmissionDate);
        Assert.Equal(1, repository.QueryCount);
        Assert.Equal(130m, repository.LastSearch?.MinimumNetAmount);
    }

    [Fact]
    public void Query_NullPayTotalBecomesZeroOnlyWhenCalculatingNet()
    {
        var service = new C141RankingService(new FakeRepository([Source("1150101", "1", "A12", 7, 200m, null)]));

        C141RankingRow result = Assert.Single(service.Query(new(null, null, null)));

        Assert.Null(result.PayTotal);
        Assert.Equal(200m, result.NetAmount);
    }

    [Fact]
    public void Query_AutomaticSelectionUsesSevenThirtyAndStrictFourDayRules()
    {
        var repository = new FakeRepository(
        [
            Source("1150101", "1", "A01", 1, 20_000m, 0m, "1151006"),
            Source("1150102", "1", "A02", 2, 20_000m, 0m, "1151007"),
            Source("1150103", "1", "A03", 3, 5_000m, 0m, "1150913"),
            Source("1150104", "1", "A04", 4, 5_000m, 0m, "1150914"),
            Source("1150105", "1", "A05", 5, 5_000m, 0m, "1150913", "1151009"),
            Source("1150106", "1", "A06", 6, 5_000m, 0m, "1150913", "1151008")
        ]);
        var service = new C141RankingService(repository);

        IReadOnlyList<C141RankingRow> result = service.Query(new(null, null, null)
        {
            OracleToday = new DateOnly(2026, 10, 13)
        });
        Dictionary<decimal, C141RankingRow> byNumber = result.ToDictionary(row => row.Key.AdmissionNumber);

        Assert.True(byNumber[1].IsAutomaticallySelected);
        Assert.Equal(7, byNumber[1].DaysSinceAdmission);
        Assert.False(byNumber[2].IsAutomaticallySelected);
        Assert.True(byNumber[3].IsAutomaticallySelected);
        Assert.Equal(30, byNumber[3].DaysSinceAdmission);
        Assert.False(byNumber[4].IsAutomaticallySelected);
        Assert.False(byNumber[5].IsAutomaticallySelected);
        Assert.True(byNumber[6].IsAutomaticallySelected);
    }

    [Fact]
    public void Query_RejectsOverlongMedicalRecordBeforeRepositoryAccess()
    {
        var repository = new FakeRepository([]);
        var service = new C141RankingService(repository);

        Assert.Throws<ArgumentException>(() => service.Query(new("12345678901", null, null)));
        Assert.Equal(0, repository.QueryCount);
    }

    [Fact]
    public void RankingSql_PreservesOracleNullPropagationAndOnlyReads()
    {
        string sql = C141RankingSql.Query;

        Assert.Contains("SUM(T.intOp2Acc1 + T.intOp2Acc2 + T.intOp2Acc3 + T.intOp2Acc4 + T.intitemI3)", sql, StringComparison.Ordinal);
        Assert.Contains("T.vchOp2Stat IN ('C','P')", sql, StringComparison.Ordinal);
        Assert.Contains("T.VChOp2Debt<>'T'", sql, StringComparison.Ordinal);
        Assert.Contains("LENGTH(TRIM(contact.vchpid))=10", sql, StringComparison.Ordinal);
        Assert.Contains("(:bedNoLength=6 AND B.chOp1EBid=:bedNo)", sql, StringComparison.Ordinal);
        Assert.Contains("B.chOp1EBid LIKE :bedNoPrefix", sql, StringComparison.Ordinal);
        Assert.Contains("(NVL(b2.rlAMT,0) - NVL(pay.PayTot,0)) AS NetAmount", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("NVL(T.intOp2Acc1", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("COALESCE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("UPDATE", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RankingSqlPagesByStrictNetThresholdAndFourStableKeys()
    {
        Assert.Contains("ranked.NetAmount > :minimumNetAmount", C141RankingSql.Page, StringComparison.Ordinal);
        Assert.Contains("ORDER BY ranked.NetAmount DESC", C141RankingSql.Page, StringComparison.Ordinal);
        Assert.Contains("ranked.AdmissionDate ASC", C141RankingSql.Page, StringComparison.Ordinal);
        Assert.Contains("ranked.AdmissionTime ASC", C141RankingSql.Page, StringComparison.Ordinal);
        Assert.Contains("ranked.AdmissionRoom ASC", C141RankingSql.Page, StringComparison.Ordinal);
        Assert.Contains("ranked.AdmissionNumber ASC", C141RankingSql.Page, StringComparison.Ordinal);
        Assert.Contains("OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY", C141RankingSql.Page, StringComparison.Ordinal);
    }

    [Fact]
    public void SearchParameters_UseFixedCharForExactFiltersAndVarcharPatternForPrefix()
    {
        using var command = new OracleCommand();

        C141RankingRepository.AddSearchParameters(command, new("C36979", "A12", null));

        Assert.Equal(OracleDbType.Char, command.Parameters["mrNo"].OracleDbType);
        Assert.Equal(10, command.Parameters["mrNo"].Size);
        Assert.Equal("C36979    ", command.Parameters["mrNo"].Value);
        Assert.Equal(OracleDbType.Char, command.Parameters["bedNo"].OracleDbType);
        Assert.Equal(8, command.Parameters["bedNo"].Size);
        Assert.Equal("A12     ", command.Parameters["bedNo"].Value);
        Assert.Equal(3, command.Parameters["bedNoLength"].Value);
        Assert.Equal(OracleDbType.Varchar2, command.Parameters["bedNoPrefix"].OracleDbType);
        Assert.Equal("A12%", command.Parameters["bedNoPrefix"].Value);
    }

    private static C141RankingSourceRow Source(
        string date,
        string time,
        string room,
        decimal number,
        decimal receivable,
        decimal? payTotal,
        string? inpatientAdmissionDate = null,
        string? noticeDate = null) =>
        new(new(date, time, room, number), "C36979", "測試", room, inpatientAdmissionDate, "30", null, null,
            "1", "1", null, receivable, null, null, noticeDate, payTotal, false);

    private sealed class FakeRepository(IReadOnlyList<C141RankingSourceRow> rows) : IC141RankingRepository
    {
        public int QueryCount { get; private set; }
        public C141RankingSearch? LastSearch { get; private set; }

        public DateOnly GetOracleToday(CancellationToken cancellationToken) => new(2026, 10, 13);

        public IReadOnlyList<C141RankingSourceRow> Query(C141RankingSearch search, CancellationToken cancellationToken)
        {
            QueryCount++;
            LastSearch = search;
            return rows;
        }

        public int GetCount(C141RankingSearch search, CancellationToken cancellationToken)
        {
            QueryCount++;
            LastSearch = search;
            return rows.Count(row => search.MinimumNetAmount is null
                || (row.ReceivableAmount ?? 0m) - (row.PayTotal ?? 0m) > search.MinimumNetAmount.Value);
        }

        public IReadOnlyList<C141RankingSourceRow> GetPage(
            C141RankingSearch search,
            int offset,
            int pageSize,
            CancellationToken cancellationToken)
        {
            QueryCount++;
            LastSearch = search;
            return rows
                .Where(row => search.MinimumNetAmount is null
                    || (row.ReceivableAmount ?? 0m) - (row.PayTotal ?? 0m) > search.MinimumNetAmount.Value)
                .OrderByDescending(row => (row.ReceivableAmount ?? 0m) - (row.PayTotal ?? 0m))
                .ThenBy(row => row.Key.AdmissionDate, StringComparer.Ordinal)
                .ThenBy(row => row.Key.AdmissionTime, StringComparer.Ordinal)
                .ThenBy(row => row.Key.AdmissionRoom, StringComparer.Ordinal)
                .ThenBy(row => row.Key.AdmissionNumber)
                .Skip(offset)
                .Take(pageSize)
                .ToArray();
        }
    }
}
