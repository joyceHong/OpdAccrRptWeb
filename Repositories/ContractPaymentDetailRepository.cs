using System.Data;
using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class ContractPaymentDetailRepository : IContractPaymentDetailRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;

    public ContractPaymentDetailRepository(IConnectionStringProvider connectionStringProvider) =>
        _connectionStringProvider = connectionStringProvider;

    internal const string OutpatientBaseSql = @"SELECT
            RTRIM(c.vchAccPFin2) AS ContractCode,
            RTRIM(t.chDctTypeName) AS ContractName,
            b.chOp1Date AS VisitDate,
            RTRIM(b.chOp1MrNo) AS MedicalRecordNumber,
            RTRIM(b.chOp1PName) AS PatientName,
            RTRIM(s.chSecName) AS DepartmentName,
            RTRIM(b.chOp1DrName) AS DoctorName,
            SUM(c.intAccCashAll) AS PaymentAmount,
            RTRIM(c.vchAccUserID) AS CashierUserId,
            b.chOp1Time AS EncounterTime,
            b.chOp1Room AS EncounterRoom,
            b.intOp1No AS EncounterNumber
        FROM IpdContractAccTbl c
        JOIN OpdBasicTbl b ON c.chOp1Date = b.chOp1Date
            AND c.chOp1Time = b.chOp1Time
            AND c.chOp1Room = b.chOp1Room
            AND c.intOp1No = b.intOp1No
        JOIN GenSectionTbl s ON b.chOp1Sec = s.chSecNo
        JOIN GenDctTypeTbl t ON RPAD(c.vchAccPFin2, 3, ' ') = t.chDctType
        WHERE c.vchAccDate BETWEEN :startDate AND :endDate
          AND c.chOp1Time <> '0'
          AND (:billingCode IS NULL OR RTRIM(c.vchAccPFin2) = :billingCode)
        GROUP BY c.vchAccPFin2, t.chDctTypeName, b.chOp1Date, b.chOp1MrNo,
            b.chOp1PName, s.chSecName, b.chOp1DrName, c.vchAccUserID,
            b.chOp1Time, b.chOp1Room, b.intOp1No
        HAVING SUM(c.intAccCashAll) <> 0";

    internal const string InpatientBaseSql = @"SELECT
            RTRIM(c.vchAccPFin2) AS ContractCode,
            RTRIM(t.chDctTypeName) AS ContractName,
            b.chOp1Date AS VisitDate,
            RTRIM(b.chOp1MrNo) AS MedicalRecordNumber,
            RTRIM(b.chOp1PName) AS PatientName,
            RTRIM(s.chSecName) AS DepartmentName,
            RTRIM(b.chOp1DrName) AS DoctorName,
            SUM(c.intAccCashAll) AS PaymentAmount,
            RTRIM(c.vchAccUserID) AS CashierUserId,
            b.chOp1Time AS EncounterTime,
            b.chOp1Room AS EncounterRoom,
            b.intOp1No AS EncounterNumber
        FROM IpdContractAccTbl c
        JOIN IpdBasicTbl b ON c.chOp1Date = b.chOp1Date
            AND c.chOp1Time = b.chOp1Time
            AND c.chOp1Room = b.chOp1Room
            AND c.intOp1No = b.intOp1No
        JOIN GenSectionTbl s ON b.chOp1Sec = s.chSecNo
        JOIN GenDctTypeTbl t ON RPAD(c.vchAccPFin2, 3, ' ') = t.chDctType
        WHERE c.vchAccDate BETWEEN :startDate AND :endDate
          AND c.chOp1Time = '0'
          AND (:billingCode IS NULL OR RTRIM(c.vchAccPFin2) = :billingCode)
        GROUP BY c.vchAccPFin2, t.chDctTypeName, b.chOp1Date, b.chOp1MrNo,
            b.chOp1PName, s.chSecName, b.chOp1DrName, c.vchAccUserID,
            b.chOp1Time, b.chOp1Room, b.intOp1No
        HAVING SUM(c.intAccCashAll) <> 0";

    internal const string StableOrder =
        "ContractCode, ContractName, VisitDate, MedicalRecordNumber, EncounterTime, " +
        "EncounterRoom, EncounterNumber, PatientName, DepartmentName, DoctorName, CashierUserId";

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<ContractPaymentDetailReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        string sql = GetCountSql(searchCondition.EncounterSource);
        using IDbConnection connection = CreateConnection();
        return connection.ExecuteScalar<int>(sql, CreateParameters(searchCondition));
    }

    public List<ContractPaymentDetailReportViewModel> GetPage(SearchReportCondition searchCondition)
    {
        string sql = GetPageSql(searchCondition.EncounterSource);
        using IDbConnection connection = CreateConnection();
        return connection.Query<ContractPaymentDetailReportViewModel>(
            sql, CreateParameters(searchCondition)).ToList();
    }

    internal static string GetBaseSql(string? encounterSource) => encounterSource switch
    {
        EncounterSources.Emergency => OutpatientBaseSql,
        EncounterSources.Inpatient => InpatientBaseSql,
        _ => throw new ArgumentException("C29 就醫來源僅接受門急診或住院。", nameof(encounterSource))
    };

    internal static string GetCountSql(string? encounterSource) =>
        $"SELECT COUNT(*) FROM ({GetBaseSql(encounterSource)}) C29Rows";

    internal static string GetPageSql(string? encounterSource) => $@"SELECT
            ContractCode, ContractName, VisitDate, MedicalRecordNumber, PatientName,
            DepartmentName, DoctorName, PaymentAmount, CashierUserId
        FROM ({GetBaseSql(encounterSource)}) C29Rows
        ORDER BY {StableOrder}
        OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";

    internal static object CreateParameters(SearchReportCondition searchCondition)
    {
        var pageNumber = searchCondition.PageNumber ?? 1;
        var pageSize = searchCondition.PageSize ?? 10;
        return new
        {
            startDate = searchCondition.StartDate ?? throw new ArgumentException("C29 缺少起始日期。"),
            endDate = searchCondition.EndDate ?? throw new ArgumentException("C29 缺少截止日期。"),
            billingCode = string.IsNullOrWhiteSpace(searchCondition.BillingCode)
                ? null
                : searchCondition.BillingCode.Trim(),
            rowOffset = ((long)pageNumber - 1) * pageSize,
            pageSize
        };
    }

    private IDbConnection CreateConnection() =>
        new OracleConnection(_connectionStringProvider.GetConnectionString());
}
