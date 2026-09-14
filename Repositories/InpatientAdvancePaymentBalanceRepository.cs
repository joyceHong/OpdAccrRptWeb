using System.Data;
using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class InpatientAdvancePaymentBalanceRepository : IInpatientAdvancePaymentBalanceRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;

    public InpatientAdvancePaymentBalanceRepository(IConnectionStringProvider connectionStringProvider) =>
        _connectionStringProvider = connectionStringProvider;

    internal const string SourceSql = @"SELECT
            SUBSTR(chOp4IDate, 1, 7) AS EffectiveYearMonth,
            RTRIM(chOp1MrNo) AS MedicalRecordNumber,
            RTRIM(chOp1PName) AS PatientName,
            RTRIM(chOp4PFin1) AS FinancialCategory,
            rlOp4Sub1 AS AdvancePaymentBalance,
            RTRIM(chSeqNo) AS SequenceNumber
        FROM IpdAdvPayTbl
        WHERE chOp4IDate <= :endDateBoundary
          AND (chOp4DCDate > :endDateBoundary
               OR RTRIM(chOp4DCDate) IS NULL
               OR chOp4DCDate = '0')
          AND chOp1MrNo NOT IN ('C36979', '1000000')
          AND chOp4DC IN ('0', '1', '2')
          AND rlOp4Sub1 <> 0";

    internal static string CountSql => $"SELECT COUNT(*) FROM ({SourceSql}) C25Rows";

    internal const string StableOrder =
        "EffectiveYearMonth, MedicalRecordNumber, PatientName, FinancialCategory, AdvancePaymentBalance, SequenceNumber";

    internal static string PageSql => $@"SELECT
            EffectiveYearMonth, MedicalRecordNumber, PatientName,
            FinancialCategory, AdvancePaymentBalance, SequenceNumber
        FROM ({SourceSql}) C25Rows
        ORDER BY {StableOrder}
        OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<InpatientAdvancePaymentBalanceReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.ExecuteScalar<int>(CountSql, CreateParameters(searchCondition));
    }

    public List<InpatientAdvancePaymentBalanceReportViewModel> GetPage(SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.Query<InpatientAdvancePaymentBalanceReportViewModel>(
            PageSql, CreateParameters(searchCondition)).ToList();
    }

    internal static object CreateParameters(SearchReportCondition searchCondition)
    {
        var endDate = searchCondition.EndDate
            ?? throw new ArgumentException("C25 缺少截止日期。");
        var pageNumber = searchCondition.PageNumber ?? 1;
        var pageSize = searchCondition.PageSize ?? 10;
        return new
        {
            endDateBoundary = $"{endDate}9999",
            rowOffset = ((long)pageNumber - 1) * pageSize,
            pageSize
        };
    }

    private IDbConnection CreateConnection() =>
        new OracleConnection(_connectionStringProvider.GetConnectionString());
}
