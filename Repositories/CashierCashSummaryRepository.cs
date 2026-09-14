using System.Data;
using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class CashierCashSummaryRepository : ICashierCashSummaryRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;

    public CashierCashSummaryRepository(IConnectionStringProvider connectionStringProvider) =>
        _connectionStringProvider = connectionStringProvider;

    internal const string SourceSql = @"SELECT
            CASE WHEN SUBSTR(chAccSeqNo, 1, 1) = 'E' THEN 'E' ELSE 'O' END AS RoomType,
            chAccDate AS CashierDate,
            RTRIM(chAccUserID) AS CashierUserId,
            SUM(NVL(intAccCash1, 0)) AS CashAmount
        FROM GenAccCaseDayTbl
        WHERE chAccDate BETWEEN :startDate AND :endDate
          AND chAccMrNo NOT IN ('C36979', '1000000')
        GROUP BY CASE WHEN SUBSTR(chAccSeqNo, 1, 1) = 'E' THEN 'E' ELSE 'O' END,
            chAccDate, chAccUserID
        HAVING SUM(NVL(intAccCash1, 0)) <> 0
        UNION ALL
        SELECT
            SUBSTR(vchAccSeqNo, 1, 1) AS RoomType,
            vchAccDate AS CashierDate,
            RTRIM(vchAccUserID) AS CashierUserId,
            SUM(NVL(intAccCash1, 0)) AS CashAmount
        FROM IpdAccCaseDayTbl
        WHERE vchAccDate BETWEEN :startDate AND :endDate
          AND vchAccMrNo NOT IN ('C36979', '1000000')
        GROUP BY SUBSTR(vchAccSeqNo, 1, 1), vchAccDate, vchAccUserID
        HAVING SUM(NVL(intAccCash1, 0)) <> 0
        UNION ALL
        SELECT
            'H' AS RoomType,
            vchAccDate AS CashierDate,
            RTRIM(vchUserID) AS CashierUserId,
            SUM(NVL(intHappyCash, 0)) AS CashAmount
        FROM GenAccHappyCashTbl
        WHERE vchAccDate BETWEEN :startDate AND :endDate
          AND vchMrNo NOT IN ('C36979', '1000000')
        GROUP BY vchAccDate, vchUserID
        HAVING SUM(NVL(intHappyCash, 0)) <> 0";

    internal static string AggregateSql => $@"SELECT
            CASE WHEN GROUPING(sourceRows.CashierUserId) = 1
                THEN '合計' ELSE sourceRows.CashierUserId END AS CashierUserId,
            CASE WHEN GROUPING(sourceRows.CashierUserId) = 1
                THEN NULL ELSE RTRIM(userProfile.chUserName) END AS CashierUserName,
            NVL(SUM(CASE WHEN sourceRows.RoomType = 'O' THEN sourceRows.CashAmount ELSE 0 END), 0) AS OutpatientAmount,
            NVL(SUM(CASE WHEN sourceRows.RoomType = 'E' THEN sourceRows.CashAmount ELSE 0 END), 0) AS EmergencyAmount,
            NVL(SUM(CASE WHEN sourceRows.RoomType = 'I' THEN sourceRows.CashAmount ELSE 0 END), 0) AS InpatientAmount,
            NVL(SUM(CASE WHEN sourceRows.RoomType IN ('O', 'E', 'I') THEN sourceRows.CashAmount ELSE 0 END), 0) AS HisSubtotalAmount,
            NVL(SUM(CASE WHEN sourceRows.RoomType = 'H' THEN sourceRows.CashAmount ELSE 0 END), 0) AS CashPlatformAmount,
            NVL(SUM(NVL(sourceRows.CashAmount, 0)), 0) AS TotalAmount,
            GROUPING(sourceRows.CashierUserId) AS IsGrandTotal
        FROM ({SourceSql}) sourceRows
        LEFT JOIN GenUserProfile1 userProfile
          ON RPAD(sourceRows.CashierUserId, 10, ' ') = userProfile.chUserID
        GROUP BY ROLLUP(sourceRows.CashierUserId, RTRIM(userProfile.chUserName))
        HAVING (GROUPING(sourceRows.CashierUserId) = 0
                AND GROUPING(RTRIM(userProfile.chUserName)) = 0)
            OR GROUPING(sourceRows.CashierUserId) = 1
        UNION ALL
        SELECT
            '合計' AS CashierUserId,
            NULL AS CashierUserName,
            0 AS OutpatientAmount,
            0 AS EmergencyAmount,
            0 AS InpatientAmount,
            0 AS HisSubtotalAmount,
            0 AS CashPlatformAmount,
            0 AS TotalAmount,
            1 AS IsGrandTotal
        FROM DUAL
        WHERE NOT EXISTS (SELECT 1 FROM ({SourceSql}) emptySourceRows)";

    internal static string CountSql =>
        $"SELECT COUNT(*) FROM ({AggregateSql}) C213Rows";

    internal const string StableOrder =
        "IsGrandTotal, CashierUserId, CashierUserName";

    internal static string PageSql => $@"SELECT
            CashierUserId, CashierUserName, OutpatientAmount, EmergencyAmount,
            InpatientAmount, HisSubtotalAmount, CashPlatformAmount, TotalAmount
        FROM ({AggregateSql}) C213Rows
        ORDER BY {StableOrder}
        OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<CashierCashSummaryReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.ExecuteScalar<int>(CountSql, CreateParameters(searchCondition));
    }

    public List<CashierCashSummaryReportViewModel> GetPage(SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.Query<CashierCashSummaryReportViewModel>(
            PageSql, CreateParameters(searchCondition)).ToList();
    }

    internal static object CreateParameters(SearchReportCondition searchCondition)
    {
        var pageNumber = searchCondition.PageNumber ?? 1;
        var pageSize = searchCondition.PageSize ?? 10;
        return new
        {
            startDate = searchCondition.StartDate
                ?? throw new ArgumentException("C213 缺少起始日期。"),
            endDate = searchCondition.EndDate
                ?? throw new ArgumentException("C213 缺少截止日期。"),
            rowOffset = ((long)pageNumber - 1) * pageSize,
            pageSize
        };
    }

    private IDbConnection CreateConnection() =>
        new OracleConnection(_connectionStringProvider.GetConnectionString());
}
