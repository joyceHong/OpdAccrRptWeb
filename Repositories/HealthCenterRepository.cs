using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Diagnostics;
using static OpdAccrRptWeb.Help.ModelDescriptionsHelper;

namespace OpdAccrRptWeb.Repositories
{
    public class HealthCenterRepository : IHealthCenterRepository
    {

        private readonly IConnectionStringProvider _connectionStringProvider;
        private readonly ILogger<HealthCenterRepository> _logger;

        public HealthCenterRepository(
            IConnectionStringProvider connectionStringProvider,
            ILogger<HealthCenterRepository> logger)
        {
            _connectionStringProvider = connectionStringProvider;
            _logger = logger;
        }

        #region C171  健康中心明細

        /// <summary>
        /// 取得健康中心查詢欄位資訊
        /// </summary>
        /// <returns></returns>
        public List<PropertyMetadata> GetHelthCenterDetailColumns()
        {
            return ModelDescriptionsHelper.GetPropertyDescriptions<HealthCenterDetailViewModel>();
        }

        /// <summary>
        ///  C171 健康管理中心明細資料
        /// </summary>
        /// <param name="searchCondition"></param>
        /// <returns></returns>
        public int GetHealthCenterDataCount(SearchReportCondition searchCondition)
        {
            return ExecuteC171Count(searchCondition, () =>
            {
                using IDbConnection connection = CreateConnection();
                return connection.ExecuteScalar<int>(C171CountSql, CreateC171Parameters(searchCondition));
            });
        }

        public List<T> GetHealthCenterDataPage<T>(SearchReportCondition searchCondition)
        {
            return ExecuteC171Page(searchCondition, () =>
            {
                using IDbConnection connection = CreateConnection();
                return connection.Query<T>(C171PageSql, CreateC171Parameters(searchCondition)).ToList();
            });
        }

        internal int ExecuteC171Count(SearchReportCondition searchCondition, Func<int> query)
        {
            var stopwatch = Stopwatch.StartNew();
            var totalCount = query();
            stopwatch.Stop();
            _logger.LogInformation(
                "{ReportCode} count SQL completed in {CountSqlElapsedMs} ms for {StartDate} through {EndDate}; TotalCount={TotalCount}",
                searchCondition.ReportCode,
                stopwatch.ElapsedMilliseconds,
                searchCondition.StartDate,
                searchCondition.EndDate,
                totalCount);
            return totalCount;
        }

        internal List<T> ExecuteC171Page<T>(SearchReportCondition searchCondition, Func<List<T>> query)
        {
            var stopwatch = Stopwatch.StartNew();
            var data = query();
            stopwatch.Stop();
            _logger.LogInformation(
                "{ReportCode} page SQL completed in {PageSqlElapsedMs} ms for {StartDate} through {EndDate}; PageNumber={PageNumber}, PageSize={PageSize}, ReturnedRows={ReturnedRows}",
                searchCondition.ReportCode,
                stopwatch.ElapsedMilliseconds,
                searchCondition.StartDate,
                searchCondition.EndDate,
                searchCondition.PageNumber,
                searchCondition.PageSize,
                data.Count);
            return data;
        }

        internal const string C171BaseSql = @"SELECT
                                            RTRIM(o.chop4pfin2)      AS PostingCode,
                                            RTRIM(t.chdcttypename)   AS PostingName,
                                            RTRIM(s.chnewsecno)      AS CenterCode,
                                            RTRIM(o.chop4drid)       AS OrderingDoctorId,
                                            RTRIM(o.chop4exedrid)    AS PerformingDoctorId,
                                            b.chop1date              AS VisitDate,
                                            RTRIM(b.chop1room)       AS ClinicRoom,
                                            RTRIM(b.chop1mrno)       AS CHMRNO,
                                            RTRIM(b.chop1pname)      AS PatientName,
                                            o.rlop4ordtot + o.rlop4ordtot2 AS Qty,
                                           ROUND(  (o.rlop4sub1 + o.rlop4sub2 + o.rlop4sub3 +
                                             o.rlop4sub4 + o.rlop4sub5 + o.rlop4sub6)
                                               / (o.rlop4ordtot + o.rlop4ordtot2),4) AS UnitPrice,
                                            o.rlop4sub1 + o.rlop4sub2 + o.rlop4sub3 +
                                            o.rlop4sub4 + o.rlop4sub5 + o.rlop4sub6 AS TotalAmount,
                                            RTRIM(o.chop4ordno)      AS BillingCode,
                                            RTRIM(o.chop4ordname)    AS BillingName,
                                            RTRIM(o.chop4cdate)      AS OrderTime,
                                            1                        AS SourceRank,
                                            ROWIDTOCHAR(o.ROWID)      AS SourceRowId
                                        FROM opdordtbl o
                                        JOIN opdbasictbl b
                                          ON o.chop1date = b.chop1date
                                         AND o.chop1time = b.chop1time
                                         AND o.chop1room = b.chop1room
                                         AND o.intop1no  = b.intop1no
                                        LEFT JOIN gendcttypetbl t
                                          ON o.chop4pfin2 = t.chdcttype
                                        LEFT JOIN gensectiontbl s
                                          ON b.chop1sec = s.chsecno
                                        WHERE b.chop1date BETWEEN :strSDate AND :strEDate
                                         AND (b.chop1room IN ('6F4', '6021') OR b.chop1sec LIKE '0294%')
                                         AND b.chop1mrno NOT IN ('C36979', '1000000')
                                         AND o.chop4stat <> 'DC'
                                         AND o.chop4ordno NOT IN ('ACC-69', 'ACC-64')
                                         AND o.rlop4ordtot + o.rlop4ordtot2 <> 0

                                        UNION ALL

                                        SELECT
                                            RTRIM(d.chop3pfin2),
                                            RTRIM(t.chdcttypename),
                                            RTRIM(s.chnewsecno),
                                            RTRIM(d.chop3drid),
                                            '',
                                            b.chop1date,
                                            RTRIM(b.chop1room),
                                            RTRIM(b.chop1mrno),
                                            RTRIM(b.chop1pname),
                                            d.rlop3drgtot AS qty,
                                           ROUND( (d.rlop3sub1 + d.rlop3sub2 + d.rlop3sub3 +
                                             d.rlop3sub4 + d.rlop3sub5 + d.rlop3sub6)
                                               / d.rlop3drgtot,4) AS unitprice,
                                            d.rlop3sub1 + d.rlop3sub2 + d.rlop3sub3 +
                                            d.rlop3sub4 + d.rlop3sub5 + d.rlop3sub6 AS amt,
                                            RTRIM(d.chop3drgno),
                                            RTRIM(d.chop3drgname),
                                            RTRIM(d.chop3cdate),
                                            2,
                                            ROWIDTOCHAR(d.ROWID)
                                        FROM opddrgtbl d
                                        JOIN opdbasictbl b
                                          ON d.chop1date = b.chop1date
                                         AND d.chop1time = b.chop1time
                                         AND d.chop1room = b.chop1room
                                         AND d.intop1no  = b.intop1no
                                        LEFT JOIN gendcttypetbl t
                                          ON d.chop3pfin2 = t.chdcttype
                                        LEFT JOIN gensectiontbl s
                                          ON b.chop1sec = s.chsecno
                                        WHERE b.chop1date BETWEEN :strSDate AND :strEDate
                                         AND (b.chop1room IN ('6F4', '6021') OR b.chop1sec LIKE '0294%')
                                         AND b.chop1mrno NOT IN ('C36979', '1000000')
                                         AND d.chop3stat <> 'DC'
                                         AND d.rlop3drgtot <> 0";

        internal static readonly string C171CountSql = $@"SELECT COUNT(*) FROM ({C171BaseSql}) C171Rows";

        internal static readonly string C171PageSql = $@"SELECT
                                                PostingCode,
                                                PostingName,
                                                CenterCode,
                                                OrderingDoctorId,
                                                PerformingDoctorId,
                                                VisitDate,
                                                ClinicRoom,
                                                CHMRNO,
                                                PatientName,
                                                Qty,
                                                UnitPrice,
                                                TotalAmount,
                                                BillingCode,
                                                BillingName,
                                                OrderTime
                                            FROM ({C171BaseSql}) C171Rows
                                            ORDER BY SourceRowId
                                            OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";

        //internal static readonly string C171PageSql = $@"SELECT
        //                                        PostingCode,
        //                                        PostingName,
        //                                        CenterCode,
        //                                        OrderingDoctorId,
        //                                        PerformingDoctorId,
        //                                        VisitDate,
        //                                        ClinicRoom,
        //                                        CHMRNO,
        //                                        PatientName,
        //                                        Qty,
        //                                        UnitPrice,
        //                                        TotalAmount,
        //                                        BillingCode,
        //                                        BillingName,
        //                                        OrderTime
        //                                    FROM ({C171BaseSql}) C171Rows
        //                                    ORDER BY CenterCode, OrderingDoctorId, PerformingDoctorId,
        //                                             VisitDate, ClinicRoom, CHMRNO, BillingCode, BillingName,
        //                                             SourceRank, SourceRowId
        //                                    OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";

        internal static object CreateC171Parameters(SearchReportCondition searchCondition)
        {
            var pageNumber = searchCondition.PageNumber ?? 1;
            var pageSize = searchCondition.PageSize ?? 10;
            return new
            {
                strSDate = searchCondition.StartDate,
                strEDate = searchCondition.EndDate,
                rowOffset = ((long)pageNumber - 1) * pageSize,
                pageSize
            };
        }
        #endregion

        #region C172  健康管理中心金額統計
        /// <summary>
        /// C172  健康管理中心金額統計
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchCondition"></param>
        /// <returns></returns>
        public List<T> GetHealthCenterCountData<T>(SearchReportCondition searchCondition)
        {
            try
            {
                using (IDbConnection connection = CreateConnection())
                {
                    string selectSql = $@"SELECT
                                            RTRIM(s.chnewsecno) AS CenterCode, -- 責任中心代碼
                                            b.chop1date AS VisitDate, -- 就診日,
                                            RTRIM(o.chop4ordno) AS BillingCode, --批價代碼,
                                            RTRIM(o.chop4ordname) AS BillingName, -- 批價碼名稱,
                                            SUM(
                                                o.rlop4sub1
                                                + o.rlop4sub2
                                                + o.rlop4sub3
                                                + o.rlop4sub4
                                                + o.rlop4sub5
                                                + o.rlop4sub6
                                            ) AS TotalAmount --金額
                                        FROM opdordtbl o
                                        JOIN opdbasictbl b
                                            ON  o.chop1date = b.chop1date
                                            AND o.chop1time = b.chop1time
                                            AND o.chop1room = b.chop1room
                                            AND o.intop1no = b.intop1no
                                        JOIN gensectiontbl s
                                            ON b.chop1sec = s.chsecno
                                        WHERE b.chop1date BETWEEN :strSDate AND :strEDate
                                          AND (
                                                b.chop1room IN ('6F4', '6021')
                                                OR b.chop1sec LIKE '0294%'
                                              )
                                          AND b.chop1mrno NOT IN ('C36979', '1000000')
                                          AND o.chop4stat <> 'DC'
                                          AND o.chop4ordno NOT IN ('ACC-69', 'ACC-64')
                                        GROUP BY
                                            s.chnewsecno,
                                            b.chop1date,
                                            o.chop4ordno,
                                            o.chop4ordname
                                        HAVING SUM(
                                            o.rlop4sub1
                                            + o.rlop4sub2
                                            + o.rlop4sub3
                                            + o.rlop4sub4
                                            + o.rlop4sub5
                                            + o.rlop4sub6
                                        ) <> 0

                                        UNION ALL

                                        SELECT
                                            RTRIM(s.chnewsecno) AS CenterCode, -- 責任中心代碼,
                                            b.chop1date AS  VisitDate,
                                            RTRIM(d.chop3drgno) AS BillingCode, --批價代碼,
                                            RTRIM(d.chop3drgname) AS BillingName, -- 批價碼名稱,
                                            SUM(
                                                d.rlop3sub1
                                                + d.rlop3sub2
                                                + d.rlop3sub3
                                                + d.rlop3sub4
                                                + d.rlop3sub5
                                                + d.rlop3sub6
                                            ) AS TotalAmount --AS 金額
                                        FROM opddrgtbl d
                                        JOIN opdbasictbl b
                                            ON  d.chop1date = b.chop1date
                                            AND d.chop1time = b.chop1time
                                            AND d.chop1room = b.chop1room
                                            AND d.intop1no = b.intop1no
                                        JOIN gensectiontbl s
                                            ON b.chop1sec = s.chsecno
                                        WHERE b.chop1date BETWEEN :strSDate AND :strEDate
                                          AND (
                                                b.chop1room IN ('6F4', '6021')
                                                OR b.chop1sec LIKE '0294%'
                                              )
                                          AND b.chop1mrno NOT IN ('C36979', '1000000')
                                          AND d.chop3stat <> 'DC'
                                        GROUP BY
                                            s.chnewsecno,
                                            b.chop1date,
                                            d.chop3drgno,
                                            d.chop3drgname
                                        HAVING SUM(
                                            d.rlop3sub1
                                            + d.rlop3sub2
                                            + d.rlop3sub3
                                            + d.rlop3sub4
                                            + d.rlop3sub5
                                            + d.rlop3sub6
                                        ) <> 0
                                        ORDER BY
                                            CenterCode,
                                            VisitDate,
                                            BillingCode,
                                            BillingName";

                    return connection.Query<T>(selectSql, new { strSDate = searchCondition.StartDate, strEDate = searchCondition.EndDate }).ToList();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        /// <summary>
        /// C172 健康管理中心金額統計的欄位資訊
        /// </summary>
        /// <returns></returns>
        public List<PropertyMetadata> GetHelthCenterCountColumns()
        {
            return ModelDescriptionsHelper.GetPropertyDescriptions<HealthCenterCountViewModel>();
        }
        #endregion

        #region C173 健檢人次的統計


        /// <summary>
        /// 健檢人次的欄位資訊
        /// </summary>
        /// <returns></returns>
        public List<PropertyMetadata> GetHealthCheckupVisitsColumns()
        {
            return ModelDescriptionsHelper.GetPropertyDescriptions<HealthCheckupVisits>();
        }

        /// <summary>
        ///  健檢人次的資料
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="searchCondition"></param>
        /// <returns></returns>
        public List<T> GetHealthCheckupVisitsData<T>(SearchReportCondition searchCondition)
        {
            try
            {
                using (IDbConnection connection = CreateConnection())
                {
                    string selectSql = $@"SELECT
                                            SUBSTR(chop1date, 1, 5) AS chop1date, --就診年月
                                            RTRIM(chop1sec) AS chop1sec, --科別
                                            COUNT(*) AS Visits --人次
                                        FROM (
                                            SELECT DISTINCT
                                                b.chop1date,
                                                b.chop1time,
                                                b.chop1room,
                                                b.intop1no,
                                                s.chnewsecno AS chop1sec
                                            FROM opdbasictbl b
                                            JOIN opdordtbl o
                                                ON  b.chop1date = o.chop1date
                                                AND b.chop1time = o.chop1time
                                                AND b.chop1room = o.chop1room
                                                AND b.intop1no = o.intop1no
                                            JOIN gensectiontbl s
                                                ON b.chop1sec = s.chsecno
                                            WHERE b.chop1date BETWEEN :strSDate AND :strEDate
                                              AND b.chop1room NOT IN ('AAAA', 'RRRR', 'SSSS', 'ZZZZ')
                                              AND b.chop1mrno NOT IN ('C36979', '1000000')
                                              AND b.chop1sec LIKE '0294%'
                                              AND RTRIM(o.chop4dcdate) IS NULL
                                              AND o.chop4stat <> 'DC'
                                        )
                                        GROUP BY
                                            SUBSTR(chop1date, 1, 5),
                                            chop1sec";

                    return connection.Query<T>(selectSql, new { strSDate = searchCondition.StartDate, strEDate = searchCondition.EndDate }).ToList();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        #endregion



        #region C174 健康管理中心合約單位記帳表

        public List<PropertyMetadata> GetHealthCenterContractBillingReportColumns()
        {
            return ModelDescriptionsHelper.GetPropertyDescriptions<HealthCenterContractBillingReport>();
        }

        public int GetHealthCenterContractBillingReportCount(SearchReportCondition searchCondition)
        {
            using IDbConnection connection = CreateConnection();
            return connection.ExecuteScalar<int>(C174CountSql, CreateC174Parameters(searchCondition));
        }

        public List<T> GetHealthCenterContractBillingReportPage<T>(SearchReportCondition searchCondition)
        {
            using IDbConnection connection = CreateConnection();
            return connection.Query<T>(C174PageSql, CreateC174Parameters(searchCondition)).ToList();
        }

        internal const string C174BaseSql = @"SELECT
                        RTRIM(chop1pfin2)   AS BillingCode,
                        RTRIM(chop1pfin2nm) AS BillingName,
                        chop1date           AS VisitDate,
                        RTRIM(chop1mrno)    AS Chop1mrno,
                        RTRIM(chop1pname)   AS PatientName,
                        RTRIM(chop1psecnm)  AS DepartmentName,
                        RTRIM(chop1dridnm)  AS DoctorName,
                        RTRIM(chop1dct)     AS AccountSubjectCode,
                        RTRIM(chop1dctnm)   AS AccountSubjectName,
                        rlop1sub3           AS DiscountAmount,
                        rlop1sub2           AS BillingAmount,
                        rlop1subamt         AS TotalAmount,
                        RTRIM(chcuser)      AS BillingUser,
                        ROWIDTOCHAR(ROWID)  AS SourceRowId
                    FROM opdRecRpt_PFin2SumDM1
                    WHERE chdateflag BETWEEN :strSDate || '000000'
                                         AND :strEDate || '999999'";

        internal static readonly string C174CountSql = $@"SELECT COUNT(*) FROM ({C174BaseSql}) C174Rows";

        internal static readonly string C174PageSql = $@"SELECT
                        BillingCode,
                        BillingName,
                        VisitDate,
                        Chop1mrno,
                        PatientName,
                        DepartmentName,
                        DoctorName,
                        AccountSubjectCode,
                        AccountSubjectName,
                        DiscountAmount,
                        BillingAmount,
                        TotalAmount,
                        BillingUser
                    FROM ({C174BaseSql}) C174Rows
                    ORDER BY BillingCode, BillingName, VisitDate, Chop1mrno,
                             DepartmentName, DoctorName, AccountSubjectCode,
                             AccountSubjectName, BillingUser, SourceRowId
                    OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";

        internal static object CreateC174Parameters(SearchReportCondition searchCondition)
        {
            var pageNumber = searchCondition.PageNumber ?? 1;
            var pageSize = searchCondition.PageSize ?? 10;
            return new
            {
                strSDate = searchCondition.StartDate,
                strEDate = searchCondition.EndDate,
                rowOffset = ((long)pageNumber - 1) * pageSize,
                pageSize
            };
        }

        public List<HealthCenterContractBillingReport> GetHealthCenterContractBillingReportBatch(
            SearchReportCondition searchCondition,
            int offset,
            int batchSize)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(batchSize, 0);
            using IDbConnection connection = CreateConnection();
            return connection.Query<HealthCenterContractBillingReport>(
                C174PageSql,
                new
                {
                    strSDate = searchCondition.StartDate,
                    strEDate = searchCondition.EndDate,
                    rowOffset = offset,
                    pageSize = batchSize
                }).ToList();
        }

        #endregion

        private IDbConnection CreateConnection() =>
            new OracleConnection(_connectionStringProvider.GetConnectionString());

    }
}
