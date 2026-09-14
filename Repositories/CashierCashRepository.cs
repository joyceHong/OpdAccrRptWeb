using System.Data;
using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class CashierCashRepository : ICashierCashRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;

    public CashierCashRepository(IConnectionStringProvider connectionStringProvider) =>
        _connectionStringProvider = connectionStringProvider;

    internal const string SourceSql = @"SELECT
            ac.chAccDate AS CashierDate, RTRIM(ac.chAccUserID) AS CashierUserId,
            RTRIM(upf.chUserName) AS CashierUserName, SUBSTR(ac.chAccSeqNo, 1, 1) AS SourceType,
            CASE WHEN SUBSTR(ac.chAccSeqNo, 1, 1) = 'E' THEN '急診' ELSE RTRIM(ac.chAccBid) END AS OriginalCounter,
            NVL(ac.intAccCash1, 0) AS Cash1, NVL(ac.intAccCash2, 0) AS Cash2,
            NVL(ac.intAccCash3, 0) AS Cash3, NVL(ac.intAccCash4, 0) AS Cash4,
            NVL(ac.intAccCash5, 0) AS Cash5, NVL(ac.intAccCash6, 0) AS Cash6,
            NVL(ac.intSocFree, 0) AS SocFree, NVL(ac.intAccCash7, 0) AS Cash7,
            NVL(ac.intAccCash8, 0) AS Cash8, NVL(ac.intAccCash9, 0) AS Cash9,
            RTRIM(ac.vchAccCash9Note) AS Cash9Note, NVL(ac.intAccCash10, 0) AS Cash10,
            NVL(ac.intOp2AMT50, 0) AS RepairCash, 'DAY' AS SourceKind
        FROM GenAccCaseDayTbl ac
        LEFT JOIN GenUserProfile1 upf ON ac.chAccUserID = upf.chUserID
        WHERE ac.chAccDate BETWEEN :startDate AND :endDate
          AND ac.chAccMrNo NOT IN ('C36979', '1000000')
          AND (:cashierUserId IS NULL OR RTRIM(ac.chAccUserID) = :cashierUserId)
        UNION ALL
        SELECT
            ac.vchAccDate, RTRIM(ac.vchAccUserID), RTRIM(upf.chUserName),
            SUBSTR(ac.vchAccSeqNo, 1, 1), RTRIM(ac.vchAccBid),
            NVL(ac.intAccCash1, 0), NVL(ac.intAccCash2, 0), NVL(ac.intAccCash3, 0),
            NVL(ac.intAccCash4, 0), NVL(ac.intAccCash5, 0), NVL(ac.intAccCash6, 0),
            NVL(ac.intSocFree, 0), NVL(ac.intAccCash7, 0), NVL(ac.intAccCash8, 0),
            NVL(ac.intAccCash9, 0), RTRIM(ac.vchAccCash9Note), NVL(ac.intAccCash10, 0),
            NVL(ac.intOp2AMT50, 0), 'DAY'
        FROM IpdAccCaseDayTbl ac
        LEFT JOIN GenUserProfile1 upf ON RPAD(ac.vchAccUserID, 10, ' ') = upf.chUserID
        WHERE ac.vchAccDate BETWEEN :startDate AND :endDate
          AND ac.vchAccMrNo NOT IN ('C36979', '1000000')
          AND (:cashierUserId IS NULL OR RTRIM(ac.vchAccUserID) = :cashierUserId)
        UNION ALL
        SELECT
            ac.vchAccDate, RTRIM(ac.vchAccUserID), RTRIM(upf.chUserName),
            SUBSTR(ac.vchAccSeqNo, 1, 1), RTRIM(ac.vchAccBid),
            NVL(ac.intAccCash1, 0), NVL(ac.intAccCash2, 0), NVL(ac.intAccCash3, 0),
            NVL(ac.intAccCash4, 0), 0, 0, NVL(ac.intSocFree, 0), 0, 0, 0, '', 0,
            NVL(ac.intOp2AMT50, 0), 'CONTRACT'
        FROM IpdContractAccTbl ac
        LEFT JOIN GenUserProfile1 upf ON RPAD(ac.vchAccUserID, 10, ' ') = upf.chUserID
        WHERE ac.vchAccDate BETWEEN :startDate AND :endDate
          AND ac.vchAccMrNo NOT IN ('C36979', '1000000')
          AND (:cashierUserId IS NULL OR RTRIM(ac.vchAccUserID) = :cashierUserId)";

    internal static string AggregateSql => $@"SELECT CashierDate, CashierUserId,
            MAX(CashierUserName) AS CashierUserName, CounterName,
            SUM(CASE WHEN OriginalCounter = '繳欠' THEN 0 ELSE Cash1 END) AS CashAmount,
            SUM(CASE WHEN OriginalCounter = '繳欠' THEN Cash1 ELSE RepairCash END) AS SupplementaryCashAmount,
            SUM(Cash2) AS CheckAmount, SUM(Cash3) AS SocialServiceAmount,
            SUM(Cash4) AS DebitCardAmount, SUM(Cash5) AS MedicalDisputeSubsidyAmount,
            SUM(Cash6) AS CreditCardAmount, SUM(SocFree) AS VoucherAmount,
            SUM(Cash7) AS StoredValueCardAmount, SUM(Cash8) AS RevitalizationVoucherAmount,
            SUM(Cash9) AS InsurancePaymentAmount, Cash9Note AS InsurancePaymentNote,
            SUM(Cash10) AS PatientBankDebitAmount, SourceType
        FROM (
            SELECT sourceRows.*,
                CASE
                    WHEN OriginalCounter = '繳欠' THEN CASE SourceType WHEN 'E' THEN '急診' WHEN 'I' THEN '住院' ELSE '門診' END
                    WHEN SourceType = 'I' THEN OriginalCounter
                    WHEN OriginalCounter = '合約' THEN '合約'
                    ELSE CASE SourceType WHEN 'E' THEN '急診' ELSE '門診' END
                END AS CounterName
            FROM ({SourceSql}) sourceRows
        ) normalizedRows
        GROUP BY CashierDate, CashierUserId, CounterName, SourceType, Cash9Note";

    internal static string CountSql => $"SELECT COUNT(*) FROM ({AggregateSql}) C22Rows";
    internal const string CashierOrder = "CashierUserId, CashierDate, CounterName, SourceType, InsurancePaymentNote";
    internal const string EncounterOrder = "CounterName, CashierDate, CashierUserId, SourceType, InsurancePaymentNote";

    internal static string GetPageSql(string? sortType) => $@"SELECT
            CashierDate, CashierUserId, CashierUserName, CounterName, CashAmount,
            SupplementaryCashAmount, CheckAmount, SocialServiceAmount, DebitCardAmount,
            MedicalDisputeSubsidyAmount, CreditCardAmount, VoucherAmount,
            StoredValueCardAmount, RevitalizationVoucherAmount, InsurancePaymentAmount,
            InsurancePaymentNote, PatientBankDebitAmount
        FROM ({AggregateSql}) C22Rows
        ORDER BY {(sortType == CashierCashSortTypes.Encounter ? EncounterOrder : CashierOrder)}
        OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<CashierCashReportViewModel>();

    public int GetCount(SearchReportCondition condition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.ExecuteScalar<int>(CountSql, CreateParameters(condition));
    }

    public List<CashierCashReportViewModel> GetPage(SearchReportCondition condition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.Query<CashierCashReportViewModel>(
            GetPageSql(condition.CashierCashSortType), CreateParameters(condition)).ToList();
    }

    internal static object CreateParameters(SearchReportCondition condition)
    {
        var pageNumber = condition.PageNumber ?? 1;
        var pageSize = condition.PageSize ?? 10;
        return new
        {
            startDate = condition.StartDate ?? throw new ArgumentException("C22 缺少起始日期。"),
            endDate = condition.EndDate ?? throw new ArgumentException("C22 缺少截止日期。"),
            cashierUserId = string.IsNullOrWhiteSpace(condition.CashierUserId) ? null : condition.CashierUserId.Trim(),
            rowOffset = ((long)pageNumber - 1) * pageSize,
            pageSize
        };
    }

    private IDbConnection CreateConnection() => new OracleConnection(_connectionStringProvider.GetConnectionString());
}
