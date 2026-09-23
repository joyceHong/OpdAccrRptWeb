using OpdAccrRptWeb.Repositories;
namespace OpdAccrRptWeb.Tests;
public sealed class C7ReportRepositoryTests
{
    [Fact] public void Sql_IsFixedAndPreservesLegacyPredicates(){foreach(string sql in new[]{C7Sql.DrugDetail,C7Sql.OrderDetail}){Assert.Contains("BETWEEN :run_date || :start_time AND :run_date || :end_time",sql);Assert.Contains("chOp1Date=:run_date",sql);Assert.Contains(":input_user_id IS NULL OR",sql);Assert.Contains("NOT IN ('C36979','1000000')",sql);Assert.Contains("NOT IN ('I','S') OR RTRIM",sql);Assert.Contains("JOIN GenUserProfile1",sql);Assert.DoesNotContain("DISTINCT",sql);Assert.DoesNotContain("Stat <> 'DC'",sql);}}
    [Fact] public void UserLookup_UsesStrictEndDate(){Assert.Contains("chEndDate > :end_date",C7Sql.UserLookup);Assert.DoesNotContain("ORDER BY",C7Sql.UserLookup);}
}
