using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Tests;

public sealed class C141NoticeHistorySqlTests
{
    [Fact]
    public void Query_PreservesLegacyNoticeAndPrepaymentJoin()
    {
        string sql = C141NoticeHistorySql.Query;

        Assert.Contains("SELECT DISTINCT", sql, StringComparison.Ordinal);
        Assert.Contains("LEFT JOIN IpdAccCaseDayTbl", sql, StringComparison.Ordinal);
        Assert.Contains("TO_NUMBER(SUBSTR(a.chopdunino,15,4))=c.intop1no", sql, StringComparison.Ordinal);
        Assert.Contains("c.vchaccbid='預繳'", sql, StringComparison.Ordinal);
        Assert.Contains("a.ipdttype='18'", sql, StringComparison.Ordinal);
        Assert.Contains("a.chmrno NOT LIKE 'C36979%'", sql, StringComparison.Ordinal);
        Assert.DoesNotContain("ipdtnote LIKE 'Line%'", sql, StringComparison.Ordinal);
    }
}
