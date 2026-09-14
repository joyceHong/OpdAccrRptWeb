using System.Data;
using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C21AccountingSummaryRepository(IConnectionStringProvider connectionStringProvider)
    : IC21AccountingSummaryRepository
{
    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<C21AccountingSummaryReportViewModel>();
    internal const string BillingItemsSql = """
        SELECT SUBSTR(chDctItem, 1, 2) AS Code, MAX(chDctItemName) AS Name
        FROM GenDctItemTbl
        WHERE RTRIM(chDctItem) IS NOT NULL
        GROUP BY SUBSTR(chDctItem, 1, 2)
        ORDER BY SUBSTR(chDctItem, 1, 2)
        """;

    internal const string OutpatientSourceSql = """
        SELECT chOp1RoomType AS RoomType,
               SUBSTR(chOp1Dct, 1, 2) AS BillingCode,
               CASE WHEN chOp1Fin1 = '35' AND chOp1Date >= '1000901' THEN '01'
                    ELSE chOp1Fin1 END AS IdentityCode,
               SUM(NVL(intAMT, 0)) AS Amount
        FROM OpdTranColeTbl
        WHERE chOp1Date BETWEEN :startDate AND :endDate
          AND chOp1RoomType IN (0, 1)
          AND (:billingCode IS NULL OR SUBSTR(chOp1Dct, 1, 2) = :billingCode)
          AND (chOp1Fin1 IN ('01', '30') OR chOp1Fin1 = '35')
        GROUP BY chOp1RoomType, SUBSTR(chOp1Dct, 1, 2),
                 CASE WHEN chOp1Fin1 = '35' AND chOp1Date >= '1000901' THEN '01'
                      ELSE chOp1Fin1 END
        """;

    internal const string InpatientSourceSql = """
        SELECT chOp1RoomType AS RoomType,
               SUBSTR(chOp1Dct, 1, 2) AS BillingCode,
               DECODE(chOp1Fin1, '35', '30', chOp1Fin1) AS IdentityCode,
               SUM(NVL(intAMT, 0)) AS Amount
        FROM IpdTranColeTbl
        WHERE chOp1Date BETWEEN :startDate AND :endDate
          AND chOp1RoomType IN (2, 3, 4, 5)
          AND (:billingCode IS NULL OR SUBSTR(chOp1Dct, 1, 2) = :billingCode)
          AND chOp1Fin1 IN ('01', '30', '35')
        GROUP BY chOp1RoomType, SUBSTR(chOp1Dct, 1, 2),
                 DECODE(chOp1Fin1, '35', '30', chOp1Fin1)
        """;

    internal const string Room23ExistenceSql = """
        SELECT COUNT(*) FROM IpdTranColeTbl
        WHERE chOp1Date = :accountingDate AND chOp1RoomType IN ('2', '3')
        """;

    public IReadOnlyList<C21BillingItem> GetBillingItems()
    {
        using IDbConnection connection = CreateConnection();
        return connection.Query<C21BillingItem>(BillingItemsSql).ToList();
    }

    public IReadOnlyList<C21SourceAmount> GetSourceAmounts(SearchReportCondition condition)
    {
        var sql = condition.EncounterSource == C21EncounterSources.Outpatient
            ? OutpatientSourceSql
            : InpatientSourceSql;
        using IDbConnection connection = CreateConnection();
        return connection.Query<C21SourceAmount>(sql, CreateParameters(condition)).ToList();
    }

    public bool HasInpatientRoom23Data(string rocDate)
    {
        using IDbConnection connection = CreateConnection();
        return connection.ExecuteScalar<int>(Room23ExistenceSql, new { accountingDate = rocDate }) > 0;
    }

    public void RebuildSingleInpatientDay(string rocDate)
    {
        using var connection = (OracleConnection)CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        ExecuteAtomically(
            C21RebuildSql.Commands,
            command => connection.Execute(command, new { SDate = rocDate }, transaction),
            () => connection.ExecuteScalar<int>(C21RebuildSql.IntegritySql,
                new { SDate = rocDate }, transaction),
            transaction.Commit,
            transaction.Rollback);
    }

    internal static object CreateParameters(SearchReportCondition condition)
    {
        var start = DateOnly.ParseExact(condition.StartDate!, "yyyy-MM-dd");
        var end = DateOnly.ParseExact(condition.EndDate!, "yyyy-MM-dd");
        return new
        {
            startDate = ToRocDate(start),
            endDate = ToRocDate(end),
            billingCode = string.IsNullOrWhiteSpace(condition.BillingCode)
                ? null
                : condition.BillingCode.Trim()[..2]
        };
    }

    internal static string ToRocDate(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue).ToRocDateString();

    internal static void ExecuteAtomically(
        IReadOnlyList<string> commands,
        Action<string> execute,
        Func<int> countInvalidRows,
        Action commit,
        Action rollback)
    {
        try
        {
            foreach (var command in commands)
            {
                execute(command);
            }
            if (countInvalidRows() != 0)
            {
                throw new InvalidOperationException("C21 重建後完整性檢查失敗。");
            }
            commit();
        }
        catch
        {
            rollback();
            throw;
        }
    }

    private IDbConnection CreateConnection() =>
        new OracleConnection(connectionStringProvider.GetConnectionString());
}
