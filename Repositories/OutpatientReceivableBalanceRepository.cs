using System.Data;
using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class OutpatientReceivableBalanceRepository : IOutpatientReceivableBalanceRepository
{
    internal const string FixedStartDate = "1040101";
    internal const string SelfPayIdentityPredicate = "chOp4PFin1 IN ('01', '35')";
    internal const string InsuranceIdentityPredicate = "chOp4PFin1 IN ('30')";
    internal const string StableOrder = "MedicalRecordNumber, VisitDate";

    private readonly IConnectionStringProvider _connectionStringProvider;

    public OutpatientReceivableBalanceRepository(IConnectionStringProvider connectionStringProvider) =>
        _connectionStringProvider = connectionStringProvider;

    internal static string GetSourceSql(string? balanceType)
    {
        string identityPredicate = balanceType switch
        {
            ReceivableBalanceTypes.SelfPay => SelfPayIdentityPredicate,
            ReceivableBalanceTypes.Insurance => InsuranceIdentityPredicate,
            _ => throw new ArgumentException("C214 應收餘額類型不正確。", nameof(balanceType))
        };

        return $@"SELECT
                RTRIM(chOp1MrNo) AS MedicalRecordNumber,
                chOp1Date2 AS VisitDate,
                SUM(NVL(rlOp1Sub6, 0)) AS ReceivableAmount
            FROM OpdRecRpt_PDebtDM
            WHERE chOp1Date BETWEEN :startDate AND :endDate
              AND chOp1Date2 >= :startDate
              AND (chDC = '0' OR RTRIM(chDC) IS NULL)
              AND {identityPredicate}
            GROUP BY chOp1MrNo, chOp1Date2
            HAVING ABS(SUM(NVL(rlOp1Sub6, 0))) > 10";
    }

    internal static string GetCountSql(string? balanceType) =>
        $"SELECT COUNT(*) FROM ({GetSourceSql(balanceType)}) C214Rows";

    internal static string GetPageSql(string? balanceType) => $@"SELECT
            MedicalRecordNumber, VisitDate, ReceivableAmount
        FROM ({GetSourceSql(balanceType)}) C214Rows
        ORDER BY {StableOrder}
        OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<OutpatientReceivableBalanceReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.ExecuteScalar<int>(
            GetCountSql(searchCondition.ReceivableBalanceType),
            CreateParameters(searchCondition));
    }

    public List<OutpatientReceivableBalanceReportViewModel> GetPage(
        SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.Query<OutpatientReceivableBalanceReportViewModel>(
            GetPageSql(searchCondition.ReceivableBalanceType),
            CreateParameters(searchCondition)).ToList();
    }

    internal static object CreateParameters(SearchReportCondition searchCondition)
    {
        var endDate = searchCondition.EndDate
            ?? throw new ArgumentException("C214 缺少截止日期。");
        var pageNumber = searchCondition.PageNumber ?? 1;
        var pageSize = searchCondition.PageSize ?? 10;
        return new
        {
            startDate = FixedStartDate,
            endDate,
            rowOffset = ((long)pageNumber - 1) * pageSize,
            pageSize
        };
    }

    private IDbConnection CreateConnection() =>
        new OracleConnection(_connectionStringProvider.GetConnectionString());
}
