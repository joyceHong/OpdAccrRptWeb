using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Tests;

public sealed class C5ReportRepositoryTests
{
    [Fact]
    public void FixedSql_ContainsTenDistinctStatementsInCSharp()
    {
        C5QueryId[] ids = Enum.GetValues<C5QueryId>();
        Assert.Equal(10, ids.Length);
        Assert.All(ids, id => Assert.StartsWith("SELECT /*+ rules */", C5Sql.Get(id)));
        Assert.Equal(10, ids.Select(C5Sql.Get).Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Sql_PreservesCriticalLegacyPredicates()
    {
        Assert.Contains("A.chOp4OrdNo = :charge_code", C5Sql.OpdOrderAggregate);
        Assert.Contains("A.chOp4ExtNo AS DrgNo", C5Sql.OpdOrderAggregate);
        Assert.Contains("A.chStation = :section_code", C5Sql.IpdOrderAggregate);
        Assert.Contains("A.chOp3PSec = :section_code", C5Sql.IpdDrugAggregate);
        Assert.Contains("SUM(A.rlOp4Sub3) AS Sub3", C5Sql.OpdOrder0430Aggregate);
        Assert.Contains("A.chOp1Room LIKE 'OP_%'", C5Sql.OpdOrderAggregate);
        Assert.Contains("OR RTRIM(A.chOp4Proj) IS NULL", C5Sql.OpdOrderAggregate);
    }

    [Fact]
    public void AddParameters_UsesNamedNullSafeBindings()
    {
        C5ValidatedRequest request = new C5ReportRequest("2026-09-18", "2026-09-18").Validate();
        using var command = new OracleCommand { BindByName = true };
        C5ReportRepository.AddParameters(command, request, "1150918", C5QueryId.OpdDrugAggregate);

        Assert.True(command.BindByName);
        Assert.Equal(new[] { "run_date", "encounter_type", "room_no", "section_code",
            "charge_code", "insurance_identity_code" },
            command.Parameters.Cast<OracleParameter>().Select(x => x.ParameterName));
        Assert.Equal(DBNull.Value, command.Parameters["room_no"].Value);
    }

    [Fact]
    public void RepositoryTree_HasNoC5SqlResource()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        Assert.Empty(Directory.EnumerateFiles(root, "*C5*.sql", SearchOption.AllDirectories)
            .Where(path => !path.Contains("c#_OpdAccRpt", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void DailySessionContract_PreservesSingleDaySqlAndTimeout()
    {
        Assert.Equal(60, C5ReportRepository.CommandTimeoutSeconds);
        Assert.NotNull(typeof(IC5ReportRepository).GetMethod(nameof(IC5ReportRepository.OpenSessionAsync)));
        Assert.NotNull(typeof(IC5ReportQuerySession).GetMethod(nameof(IC5ReportQuerySession.QueryDayAsync)));
        Assert.All(Enum.GetValues<C5QueryId>(), id =>
        {
            string sql = C5Sql.Get(id);
            Assert.Contains(":run_date", sql, StringComparison.Ordinal);
            Assert.DoesNotContain(":start_date", sql, StringComparison.Ordinal);
            Assert.DoesNotContain(":end_date", sql, StringComparison.Ordinal);
        });
    }
}
