using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using static OpdAccrRptWeb.Help.ModelDescriptionsHelper;

namespace OpdAccrRptWeb.Repositories
{
    public class HealthCenterRepository : IHealthCenterRepository
    {

        private string _connectionString;
        private IConnectionStringProvider _connectionStringProvider;
        public HealthCenterRepository(IConnectionStringProvider connectionStringProvider)
        {
            _connectionStringProvider = connectionStringProvider;
            _connectionString = _connectionStringProvider.GetDbTest3ConnectionString(); //取得測試資料庫連線字串
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
        public PagedReportResult<T> GetHealthCenterData<T>(SearchReportCondition searchCondition)
        {
            using IDbConnection connection = new OracleConnection(_connectionString);
            var parameters = CreateC171Parameters(searchCondition);
            var totalCount = connection.ExecuteScalar<int>(C171CountSql, parameters);
            var data = connection.Query<T>(C171PageSql, parameters).ToList();

            return new PagedReportResult<T>
            {
                Data = data,
                TotalCount = totalCount
            };
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
                                            (o.rlop4sub1 + o.rlop4sub2 + o.rlop4sub3 +
                                             o.rlop4sub4 + o.rlop4sub5 + o.rlop4sub6)
                                               / (o.rlop4ordtot + o.rlop4ordtot2) AS UnitPrice,
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
                                            (d.rlop3sub1 + d.rlop3sub2 + d.rlop3sub3 +
                                             d.rlop3sub4 + d.rlop3sub5 + d.rlop3sub6)
                                               / d.rlop3drgtot AS unitprice,
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
                                            ORDER BY CenterCode, OrderingDoctorId, PerformingDoctorId,
                                                     VisitDate, ClinicRoom, CHMRNO, BillingCode, BillingName,
                                                     SourceRank, SourceRowId
                                            OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";

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
                using (IDbConnection connection = new OracleConnection(_connectionString))
                {
                    string selectSql = $@"SELECT
                                            RTRIM(s.chnewsecno)    AS CenterCode,
                                            b.chop1date            AS VisitDate,
                                            RTRIM(o.chop4ordno)    AS BillingCode,
                                            RTRIM(o.chop4ordname)  AS BillingName,
                                            SUM(o.rlop4sub1 + o.rlop4sub2 + o.rlop4sub3 +
                                                o.rlop4sub4 + o.rlop4sub5 + o.rlop4sub6) AS TotalAmount
                                        FROM opdordtbl o, opdbasictbl b, gensectiontbl s
                                        WHERE o.chop1date = b.chop1date
                                          AND o.chop1time = b.chop1time
                                          AND o.chop1room = b.chop1room
                                          AND o.intop1no  = b.intop1no
                                          AND b.chop1sec  = s.chsecno
                                          AND b.chop1date BETWEEN :strSDate AND :strEDate
                                          AND (b.chop1room IN ('6F4', '6021') OR b.chop1sec LIKE '0294%')
                                          AND b.chop1mrno NOT IN ('C36979', '1000000')
                                          AND o.chop4stat <> 'DC'
                                          AND o.chop4ordno NOT IN ('ACC-69', 'ACC-64')
                                        GROUP BY s.chnewsecno, b.chop1date, o.chop4ordno, o.chop4ordname
                                        HAVING SUM(o.rlop4sub1 + o.rlop4sub2 + o.rlop4sub3 +
                                                   o.rlop4sub4 + o.rlop4sub5 + o.rlop4sub6) <> 0

                                        UNION ALL

                                        SELECT
                                            RTRIM(s.chnewsecno),
                                            b.chop1date,
                                            RTRIM(d.chop3drgno),
                                            RTRIM(d.chop3drgname),
                                            SUM(d.rlop3sub1 + d.rlop3sub2 + d.rlop3sub3 +
                                                d.rlop3sub4 + d.rlop3sub5 + d.rlop3sub6)
                                        FROM opddrgtbl d, opdbasictbl b, gensectiontbl s
                                        WHERE d.chop1date = b.chop1date
                                          AND d.chop1time = b.chop1time
                                          AND d.chop1room = b.chop1room
                                          AND d.intop1no  = b.intop1no
                                          AND b.chop1sec  = s.chsecno
                                          AND b.chop1date BETWEEN :strSDate AND :strEDate
                                          AND (b.chop1room IN ('6F4', '6021') OR b.chop1sec LIKE '0294%')
                                          AND b.chop1mrno NOT IN ('C36979', '1000000')
                                          AND d.chop3stat <> 'DC'
                                        GROUP BY s.chnewsecno, b.chop1date, d.chop3drgno, d.chop3drgname
                                        HAVING SUM(d.rlop3sub1 + d.rlop3sub2 + d.rlop3sub3 +
                                                   d.rlop3sub4 + d.rlop3sub5 + d.rlop3sub6) <> 0
                                        ORDER BY CenterCode, VisitDate, BillingCode, BillingName";

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
                using (IDbConnection connection = new OracleConnection(_connectionString))
                {
                    string selectSql = $@"SELECT
                                        SUBSTR(chop1date, 1, 5) AS Chop1date,
                                        RTRIM(chop1sec)         AS Chop1sec,
                                        COUNT(*)                AS Visits
                                    FROM (
                                        SELECT DISTINCT
                                            b.chop1date,
                                            b.chop1time,
                                            b.chop1room,
                                            b.intop1no,
                                            s.chnewsecno AS chop1sec
                                        FROM opdbasictbl b,
                                             opdordtbl o,
                                             gensectiontbl s
                                        WHERE b.chop1date = o.chop1date
                                          AND b.chop1time = o.chop1time
                                          AND b.chop1room = o.chop1room
                                          AND b.intop1no  = o.intop1no
                                          AND b.chop1sec  = s.chsecno
                                          AND b.chop1date BETWEEN :strSDate AND :strEDate
                                          AND b.chop1room NOT IN ('AAAA', 'RRRR', 'SSSS', 'ZZZZ')
                                          AND b.chop1mrno NOT IN ('C36979', '1000000')
                                          AND b.chop1sec LIKE '0294%'
                                          AND RTRIM(o.chop4dcdate) IS NULL
                                          AND o.chop4stat <> 'DC'
                                    )
                                    GROUP BY SUBSTR(chop1date, 1, 5), chop1sec";

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

        public List<T> GetHealthCenterContractBillingReport<T>(SearchReportCondition searchCondition)
        {
            try
            {
                using (IDbConnection connection = new OracleConnection(_connectionString))
                {
                     string selectSql = $@"SELECT
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
                        RTRIM(chcuser)      AS BillingUser
                    FROM opdRecRpt_PFin2SumDM1
                    WHERE chdateflag BETWEEN :strSDate || '000000'
                                         AND :strEDate || '999999'";

                    return connection.Query<T>(selectSql, new { strSDate = searchCondition.StartDate, strEDate = searchCondition.EndDate }).ToList();
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        #endregion

    }
}
