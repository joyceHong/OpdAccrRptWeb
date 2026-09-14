using System.Data;
using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

/// <summary>
/// C1 門急診手術核帳表。ROWID 僅在來源資料未異動期間作分頁同值排序鍵。
/// </summary>
public sealed class SurgicalAccountingRepository : ISurgicalAccountingRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;

    public SurgicalAccountingRepository(IConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    internal const string BaseSql = @"SELECT
            CASE
                WHEN RTRIM(b.chOp1Room) = '0000' AND RTRIM(o.chStation) = '0296' THEN 'e急診(4F8)'
                WHEN RTRIM(b.chOp1Room) = '0000' THEN 'e急診(3F1)'
                WHEN RTRIM(o.chStation) = '0296' THEN 'o門診(4F8)'
                WHEN RTRIM(o.chStation) IS NOT NULL THEN 'o門診(3F1)'
                WHEN RTRIM(b.chOp1Room) = '4F7' THEN 'o門診(4F7)'
                WHEN RTRIM(b.chOp1Room) = '4F85' THEN 'o門診(4F8)'
                ELSE 'o門診(3F1)'
            END AS EncounterType,
            RTRIM(b.chOp1Date) AS EncounterDate,
            RTRIM(b.chOp1Time) AS EncounterTime,
            RTRIM(b.chOp1MrNo) AS MedicalRecordNumber,
            RTRIM(b.chOp1PName) AS PatientName,
            RTRIM(o.chOp4PFin1) AS PatientIdentity,
            RTRIM(o.chOp4SPay) AS PaymentMethod,
            RTRIM(d.chDocName) AS DoctorName,
            CASE
                WHEN RTRIM(b.chOp1Room) = '0000' AND resolvedSection.OldSection = '0201' THEN '11910'
                WHEN RTRIM(b.chOp1Room) = '0000' AND resolvedSection.OldSection = '0281' THEN '11920'
                WHEN RTRIM(b.chOp1Room) = '0000' AND resolvedSection.OldSection IN ('0220', '0221') THEN '11930'
                WHEN RTRIM(b.chOp1Room) = '0000' AND resolvedSection.OldSection = '0230' THEN '11309'
                ELSE RTRIM(sectionMap.chNewSecNo)
            END AS DepartmentCode,
            resolvedOrder.OrderCode
                || CASE
                    WHEN resolvedOrder.OrderCode IN
                        ('64202B','64164B','64201B','64162B','86007C','86008C','86011C','86012C','86013C')
                         AND RTRIM(o.chOp4Spect) IS NOT NULL
                    THEN '(' || RTRIM(o.chOp4Spect) || ')'
                    ELSE ''
                END
                || CASE
                    WHEN RTRIM(o.chOp4Sys) = '7' THEN '(麻醉)'
                    WHEN RTRIM(o.chOp4Sys) = '9' THEN '(補批)'
                    WHEN RTRIM(o.chStation) = '0330' THEN '(麻醉)'
                    ELSE ''
                END AS SurgicalOrderCode,
            NVL(o.rlOp4OrdTot, 0) AS Quantity,
            amounts.Amount,
            RTRIM(o.chOp4Dct) AS ChargeItem,
            RTRIM(o.chOp4ORFlg) AS SurgicalClass,
            CASE
                WHEN RTRIM(b.chOp1Room) = '0000' AND RTRIM(r.chSecNo) = '0201' THEN '11910'
                WHEN RTRIM(b.chOp1Room) = '0000' AND RTRIM(r.chSecNo) = '0281' THEN '11920'
                WHEN RTRIM(b.chOp1Room) = '0000' AND RTRIM(r.chSecNo) IN ('0220', '0221') THEN '11930'
                WHEN RTRIM(b.chOp1Room) = '0000' AND RTRIM(r.chSecNo) = '0230' THEN '11309'
                ELSE RTRIM(ratioSectionMap.chNewSecNo)
            END AS RatioDepartmentCode,
            RTRIM(r.chDrNo) AS RatioDoctorNumber,
            NVL(r.intRatioQty, 0) AS RatioQuantity,
            ROUND(amounts.Amount * NVL(r.intRatioQty, 0) / 100, 2) AS RatioAmount,
            o.ROWID AS OrderRowId
        FROM OpdOrdTbl o
        JOIN OpdBasicTbl b
          ON o.chOp1Date = b.chOp1Date
         AND o.chOp1Time = b.chOp1Time
         AND o.chOp1Room = b.chOp1Room
         AND o.intOp1No = b.intOp1No
        LEFT JOIN GenDoctorTbl d
          ON o.chOp4ExeDrID = d.chDocNo
        LEFT JOIN IPDPriceDrRatioTbl r
          ON o.intPriceDrRatioSeq = r.intPriceDrRatioSeq
        LEFT JOIN GenSectionTbl orderingSection
          ON o.chOp4PSec = orderingSection.chSecNo
        CROSS APPLY (
            SELECT CASE
                WHEN RTRIM(o.chOp4ExeSec) IS NOT NULL THEN RTRIM(o.chOp4ExeSec)
                WHEN o.chOp4DrID = o.chOp4ExeDrID THEN RTRIM(orderingSection.chCatSecNo)
                WHEN RTRIM(o.chOp4DrID) IS NULL AND b.chOp1DrID = o.chOp4ExeDrID
                    THEN RTRIM(orderingSection.chCatSecNo)
                ELSE RTRIM(d.chSecNo)
            END AS OldSection
            FROM dual
        ) resolvedSection
        LEFT JOIN GenSectionTbl sectionMap
          ON resolvedSection.OldSection = sectionMap.chSecNo
        LEFT JOIN GenSectionTbl ratioSectionMap
          ON RTRIM(r.chSecNo) = ratioSectionMap.chSecNo
        CROSS APPLY (
            SELECT COALESCE(RTRIM(o.chOp4ExtNo), RTRIM(o.chOp4OrdNo)) AS OrderCode
            FROM dual
        ) resolvedOrder
        CROSS APPLY (
            SELECT CASE
                WHEN o.rlOp4Sub1 > 0 THEN o.rlOp4Sub1
                WHEN o.rlOp4Sub2 > 0 THEN o.rlOp4Sub2
                ELSE 0
            END AS Amount
            FROM dual
        ) amounts
        WHERE b.chOp1Date BETWEEN :startDate AND :endDate
          AND (
                b.chOp1Room IN ('3F1', '4F7', '4F85')
                OR b.chOp1Room LIKE 'OP_%'
                OR (b.chOp1Room = '0000' AND o.chOp4Sys IN ('5', '7'))
                OR ((o.chStation IN ('0532', '0296', '0330') OR o.chStation LIKE '1OR%')
                    AND o.chStation <> '1OR4F')
              )
          AND o.chOp4Stat <> 'DC'
          AND o.chOp4HinCls NOT IN ('34', '38', '39', '52')
          AND b.chOp1MrNo NOT IN ('C36979', '1000000')
          AND (o.chOp4Proj NOT IN ('I', 'S') OR RTRIM(o.chOp4Proj) IS NULL)";

    internal const string StableOrder =
        "EncounterDate, EncounterTime, MedicalRecordNumber, OrderRowId";

    internal static string CountSql => $"SELECT COUNT(*) FROM ({BaseSql}) C1Rows";

    internal static string PageSql => $@"SELECT
            EncounterType, EncounterDate, EncounterTime, MedicalRecordNumber,
            PatientName, PatientIdentity, PaymentMethod, DoctorName, DepartmentCode,
            SurgicalOrderCode, Quantity, Amount, ChargeItem, SurgicalClass,
            RatioDepartmentCode, RatioDoctorNumber, RatioQuantity, RatioAmount
        FROM ({BaseSql}) C1Rows
        ORDER BY {StableOrder}
        OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<SurgicalAccountingReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.ExecuteScalar<int>(CountSql, CreateParameters(searchCondition));
    }

    public List<SurgicalAccountingReportViewModel> GetPage(SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.Query<SurgicalAccountingReportViewModel>(
            PageSql,
            CreateParameters(searchCondition)).ToList();
    }

    internal static object CreateParameters(SearchReportCondition searchCondition)
    {
        string startDate = searchCondition.StartDate
            ?? throw new ArgumentException("C1 缺少起始日期。", nameof(searchCondition));
        string endDate = searchCondition.EndDate
            ?? throw new ArgumentException("C1 缺少截止日期。", nameof(searchCondition));
        var pageNumber = searchCondition.PageNumber ?? 1;
        var pageSize = searchCondition.PageSize ?? 10;

        return new
        {
            startDate,
            endDate,
            rowOffset = ((long)pageNumber - 1) * pageSize,
            pageSize
        };
    }

    private IDbConnection CreateConnection() =>
        new OracleConnection(_connectionStringProvider.GetConnectionString());
}
