using System.Data;
using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class InpatientReceivableBalanceRepository : IInpatientReceivableBalanceRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;

    public InpatientReceivableBalanceRepository(IConnectionStringProvider connectionStringProvider) =>
        _connectionStringProvider = connectionStringProvider;

    internal const string SourceSql = @"WITH AggregatedRows AS (
            SELECT
                RTRIM(chMrNo) AS MedicalRecordNumber,
                RTRIM(chDate) AS AdmissionDate,
                RTRIM(chTime) AS AdmissionTime,
                RTRIM(chRoom) AS Room,
                intNo AS AdmissionNumber,
                SUM(NVL(intSelfAmt, 0)) AS SelfPayAmount,
                SUM(NVL(intClaimAmt, 0)) AS ClaimAmount,
                SUM(NVL(intPartAmt, 0)) AS CopaymentAmount
            FROM IpdTranColeMrNoTbl
            WHERE chIDate <= :endDate
              AND (chDC = '0' OR RTRIM(chDC) IS NULL)
            GROUP BY chMrNo, chDate, chTime, chRoom, intNo
        ), AdmissionTotals AS (
            SELECT
                AggregatedRows.*,
                SUM(SelfPayAmount) OVER (
                    PARTITION BY AdmissionDate, AdmissionTime, Room, AdmissionNumber) AS TotalSelfPayAmount,
                SUM(ClaimAmount) OVER (
                    PARTITION BY AdmissionDate, AdmissionTime, Room, AdmissionNumber) AS TotalClaimAmount,
                SUM(CopaymentAmount) OVER (
                    PARTITION BY AdmissionDate, AdmissionTime, Room, AdmissionNumber) AS TotalCopaymentAmount
            FROM AggregatedRows
        )
        SELECT
            MedicalRecordNumber, AdmissionDate, AdmissionTime, Room, AdmissionNumber,
            SelfPayAmount, ClaimAmount, CopaymentAmount
        FROM AdmissionTotals
        WHERE ABS(TotalSelfPayAmount + TotalClaimAmount) > 10
           OR ABS(TotalCopaymentAmount) > 10";

    internal static string CountSql => $"SELECT COUNT(*) FROM ({SourceSql}) C28Rows";

    internal const string StableOrder =
        "AdmissionDate, AdmissionTime, Room, AdmissionNumber, MedicalRecordNumber";

    internal static string PageSql => $@"SELECT
            MedicalRecordNumber, AdmissionDate, AdmissionTime, Room, AdmissionNumber,
            SelfPayAmount, ClaimAmount, CopaymentAmount
        FROM ({SourceSql}) C28Rows
        ORDER BY {StableOrder}
        OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<InpatientReceivableBalanceReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.ExecuteScalar<int>(CountSql, CreateParameters(searchCondition));
    }

    public List<InpatientReceivableBalanceReportViewModel> GetPage(SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.Query<InpatientReceivableBalanceReportViewModel>(
            PageSql, CreateParameters(searchCondition)).ToList();
    }

    internal static object CreateParameters(SearchReportCondition searchCondition)
    {
        var endDate = searchCondition.EndDate
            ?? throw new ArgumentException("C28 缺少截止日期。");
        var pageNumber = searchCondition.PageNumber ?? 1;
        var pageSize = searchCondition.PageSize ?? 10;
        return new
        {
            endDate,
            rowOffset = ((long)pageNumber - 1) * pageSize,
            pageSize
        };
    }

    private IDbConnection CreateConnection() =>
        new OracleConnection(_connectionStringProvider.GetConnectionString());
}
