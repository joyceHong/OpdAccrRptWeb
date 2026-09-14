using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace OpdAccrRptWeb.Repositories;

/// <summary>
/// C19 安全針具使用情形查檢表。
/// </summary>
public sealed class SafeNeedleRepository : ISafeNeedleRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;

    public SafeNeedleRepository(IConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    internal const string OutpatientEmergencySql = @"WITH EligibleOrders AS (
            SELECT o.chop1date,
                   o.chop1time,
                   o.chop1room,
                   o.intop1no,
                   CASE
                       WHEN o.chop4ordno IN
                           ('SICPU24','SICPU22','SICPU20','SICPU19','SICPU18','SICTEF20')
                       THEN 'X'
                       ELSE 'Y'
                   END AS Category,
                   SUBSTR(o.chop4idate, 1, 7) AS OrderDate,
                   o.chop4ordno AS OrderCode
            FROM OpdOrdTbl o
            WHERE o.chop4idate BETWEEN :orderDateStart AND :orderDateEnd
              AND (o.chop4dcdate NOT BETWEEN :orderDateStart AND :orderDateEnd
                   OR RTRIM(o.chop4dcdate) IS NULL)
              AND o.chop4ordno IN
                  ('SICPU24','SICPU22','SICPU20','SICPU19','SICPU18','SICTEF20',
                   'SDS3','SDS0.5','SSN23S','SDS1','SDS3B')
              AND (o.chop4stat <> 'DC' OR RTRIM(o.chop4stat) IS NULL)
        )
        SELECT DISTINCT
               o.Category,
               o.OrderDate,
               o.OrderCode,
               RTRIM(b.chop1ebid) AS BedNumber,
               RTRIM(b.chop1mrno) AS MedicalRecordNumber,
               RTRIM(b.chop1pname) AS PatientName
        FROM EligibleOrders o
        JOIN OpdBasicTbl b
          ON o.chop1date = b.chop1date
         AND o.chop1time = b.chop1time
         AND o.chop1room = b.chop1room
         AND o.intop1no = b.intop1no
        WHERE (:stationPrefix IS NULL OR b.chop1ebid LIKE :stationPrefix)";

    internal const string InpatientSql = @"WITH EligibleOrders AS (
            SELECT o.chop1date,
                   o.chop1time,
                   o.chop1room,
                   o.intop1no,
                   CASE
                       WHEN o.chop4ordno IN
                           ('SICPU24','SICPU22','SICPU20','SICPU19','SICPU18','SICTEF20')
                       THEN 'X'
                       ELSE 'Y'
                   END AS Category,
                   SUBSTR(o.chop4idate, 1, 7) AS OrderDate,
                   o.chop4ordno AS OrderCode
            FROM IpdOrdTbl o
            WHERE o.chop4idate BETWEEN :orderDateStart AND :orderDateEnd
              AND (o.chop4dcdate NOT BETWEEN :orderDateStart AND :orderDateEnd
                   OR RTRIM(o.chop4dcdate) IS NULL)
              AND o.chop4ordno IN
                  ('SICPU24','SICPU22','SICPU20','SICPU19','SICPU18','SICTEF20',
                   'SDS3','SDS0.5','SSN23S','SDS1','SDS3B')
              AND (o.chop4stat <> 'DC' OR RTRIM(o.chop4stat) IS NULL)
        )
        SELECT DISTINCT
               o.Category,
               o.OrderDate,
               o.OrderCode,
               RTRIM(b.chop1ebid) AS BedNumber,
               RTRIM(b.chop1mrno) AS MedicalRecordNumber,
               RTRIM(b.chop1pname) AS PatientName
        FROM EligibleOrders o
        JOIN IpdBasicTbl b
          ON o.chop1date = b.chop1date
         AND o.chop1time = b.chop1time
         AND o.chop1room = b.chop1room
         AND o.intop1no = b.intop1no
        WHERE (:stationPrefix IS NULL OR b.chop1ebid LIKE :stationPrefix)";

    private const string StableOrder =
        "BedNumber, MedicalRecordNumber, Category, OrderCode, OrderDate";

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<SafeNeedleReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.ExecuteScalar<int>(
            GetCountSql(searchCondition.EncounterSource),
            CreateParameters(searchCondition));
    }

    public List<SafeNeedleReportViewModel> GetPage(SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.Query<SafeNeedleReportViewModel>(
            GetPageSql(searchCondition.EncounterSource),
            CreateParameters(searchCondition)).ToList();
    }

    internal static string GetBaseSql(string? encounterSource) => encounterSource switch
    {
        EncounterSources.Emergency => OutpatientEmergencySql,
        EncounterSources.Inpatient => InpatientSql,
        _ => throw new ArgumentException("不支援的 C19 就醫來源。", nameof(encounterSource))
    };

    internal static string GetCountSql(string? encounterSource) =>
        $"SELECT COUNT(*) FROM ({GetBaseSql(encounterSource)}) C19Rows";

    internal static string GetPageSql(string? encounterSource) =>
        $@"SELECT *
        FROM ({GetBaseSql(encounterSource)}) C19Rows
        ORDER BY {StableOrder}
        OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";

    internal static object CreateParameters(SearchReportCondition searchCondition)
    {
        GetBaseSql(searchCondition.EncounterSource);
        var pageNumber = searchCondition.PageNumber ?? 1;
        var pageSize = searchCondition.PageSize ?? 10;
        string reportDate = searchCondition.StartDate
            ?? throw new ArgumentException("C19 缺少查詢日期。", nameof(searchCondition));

        return new
        {
            orderDateStart = $"{reportDate}0000",
            orderDateEnd = $"{reportDate}9999",
            stationPrefix = string.IsNullOrWhiteSpace(searchCondition.StationOrBedPrefix)
                ? null
                : $"{searchCondition.StationOrBedPrefix.Trim()}%",
            rowOffset = ((long)pageNumber - 1) * pageSize,
            pageSize
        };
    }

    private IDbConnection CreateConnection() =>
        new OracleConnection(_connectionStringProvider.GetConnectionString());
}
