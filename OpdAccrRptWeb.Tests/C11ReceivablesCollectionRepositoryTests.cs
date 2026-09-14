using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Tests;

public sealed class C11ReceivablesCollectionRepositoryTests
{
    [Fact]
    public void Sql_PreservesRequiredLegacyPredicatesAndOrdering()
    {
        Assert.Contains("GenDebtTbl", C11Sql.OutpatientOutstandingToEnd);
        Assert.Contains("d.chOp1Date <= :EndDate", C11Sql.OutpatientOutstandingToEnd);
        Assert.Contains("RTRIM(d.chBackFlg) IS NULL", C11Sql.OutpatientPeriodOutstanding);
        Assert.Contains("ORDER BY", C11Sql.OutpatientPeriodOutstanding);
        Assert.Contains("b.intOp1No=d.intOp1No", C11Sql.InpatientOutstandingToEnd);
        Assert.Contains("vchDebtAccType <> '3'", C11Sql.InpatientPeriodOutstanding);
        Assert.Contains("BETWEEN :StartDateTime AND :EndDateTime", C11Sql.InpatientPatientCount);
        Assert.DoesNotContain("GROUP BY SUBSTR", C11Sql.InpatientPatientCount);
        Assert.Contains("COUNT(DISTINCT d.chMrNo)", C11Sql.OutpatientPatientCount);
        Assert.Contains(":RoomTypeName", C11Sql.OutpatientPatientCount);
    }
}
