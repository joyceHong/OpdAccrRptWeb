using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Tests;

public sealed class C8ReportRepositoryTests
{
    [Fact] public void DetailSql_PreservesCanonicalLegacyShape()
    {
        string sql=C8Sql.PatchBillDetail;
        Assert.Contains("UNION ALL",sql); Assert.Equal(2,Count(sql,"BETWEEN :start_date || '0000' AND :end_date || '9999'"));
        Assert.Equal(2,Count(sql,"A.chOp1Date = B.chOp1Date(+)")); Assert.Equal(2,Count(sql,"A.chOp1Time = B.chOp1Time(+)"));
        Assert.Equal(2,Count(sql,"A.chOp1Room = B.chOp1Room(+)")); Assert.Equal(2,Count(sql,"A.intOp1No = B.intOp1No(+)"));
        Assert.Contains("A.chOp4Sys = '9'",sql); Assert.Contains("A.chOp3Sys = '9'",sql);
        Assert.Contains("A.chOp4Stat <> 'DC'",sql); Assert.Contains("A.chOp3Stat <> 'DC'",sql);
        Assert.Equal(2,Count(sql,"NOT IN ('I', 'S')")); Assert.DoesNotContain("DISTINCT",sql); Assert.DoesNotContain("ORDER BY",sql);
        Assert.DoesNotContain("SPay",sql); Assert.DoesNotContain("GROUP BY",sql);
    }
    [Fact] public void SectionSql_UsesNamedBind()=>Assert.Contains("S.chSecNo = :legacy_section_code",C8Sql.SectionLookup);
    private static int Count(string value,string part)=>(value.Length-value.Replace(part,string.Empty,StringComparison.Ordinal).Length)/part.Length;
}
