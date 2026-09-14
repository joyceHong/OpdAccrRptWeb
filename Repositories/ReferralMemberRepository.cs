using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;
using System.Data;

namespace OpdAccrRptWeb.Repositories;

/// <summary>
/// C18 醫療群會員急診住院查詢
/// </summary>
public sealed class ReferralMemberRepository : IReferralMemberRepository
{
    private readonly IConnectionStringProvider _connectionStringProvider;

    public ReferralMemberRepository(IConnectionStringProvider connectionStringProvider)
    {
        _connectionStringProvider = connectionStringProvider;
    }

    /// <summary>
    /// 急診就醫來源的基本 SQL 查詢語句。
    /// </summary>
    internal const string EmergencyBaseSql = @"SELECT
            RTRIM(f.chhospno) AS ClinicCode,
            RTRIM(h.chname) AS ClinicName,
            RTRIM(b.chop1pid) AS PatientIdentifier,
            RTRIM(f.chcase) AS CaseCategory,
            RTRIM(b.chop1pname) AS PatientName,
            RTRIM(b.chop1mrno) AS MedicalRecordNumber,
            RTRIM(s.chsecname) AS EncounterDepartment,
            RTRIM(b.chop1ebid) AS BedNumber,
            RTRIM(b.chop1drname) AS AttendingPhysician,
            b.chop1date AS AdmissionDate,
            SUBSTR(b.chop1eoutdate, 1, 7) AS DischargeDate,
            RTRIM(b.chop1icd1) AS DiagnosisCode1,
            RTRIM(b.chop1icd2) AS DiagnosisCode2,
            RTRIM(b.chop1icd3) AS DiagnosisCode3,
            RTRIM(i1.chicd9cname) AS DiagnosisName1,
            RTRIM(i2.chicd9cname) AS DiagnosisName2,
            RTRIM(i3.chicd9cname) AS DiagnosisName3,
            RTRIM(t.chagree) AS NetworkConsent,
            RTRIM(t.chwhole) AS CompleteResponse,
            CASE
                WHEN RTRIM(b.chop1tranin) IS NOT NULL
                 AND b.chop1tranout = 'N'
                 AND b.chop1sec <> '0340' THEN '轉入'
                WHEN RTRIM(b.chop1tranin) IS NOT NULL
                 AND b.chop1tranout = 'Y'
                 AND b.chop1sec <> '0340' THEN '轉出'
                ELSE ''
            END AS ReferralAnnotation,
            b.chop1date AS SortEncounterDate,
            b.chop1time AS SortEncounterTime,
            b.chop1room AS SortClinicRoom,
            b.intop1no AS SortEncounterSequence,
            ROWIDTOCHAR(f.ROWID) AS MemberSourceRowId,
            ROWIDTOCHAR(t.ROWID) AS ReferralSourceRowId,
            ROWIDTOCHAR(b.ROWID) AS EncounterSourceRowId
        FROM OpdBasicTbl b
        JOIN OpdRegPtnTbl r
          ON b.chop1date = r.chop0date
         AND b.chop1time = r.chop0time
         AND b.chop1room = r.chop0room
         AND b.intop1no = r.intop0no
        JOIN GenReferralMemberTbl f
          ON b.chop1pid = f.chid
         AND f.chyear = :memberYear
        JOIN GenSectionTbl s
          ON b.chop1sec = s.chsecno
        LEFT JOIN OpdTFHospitalTbl h
          ON f.chhospno = h.chhospno
        LEFT JOIN GenICD9Tbl i1
          ON b.chop1icd1 = i1.chicd9no
        LEFT JOIN GenICD9Tbl i2
          ON b.chop1icd2 = i2.chicd9no
        LEFT JOIN GenICD9Tbl i3
          ON b.chop1icd3 = i3.chicd9no
        LEFT JOIN OpdTFTbl t
          ON b.chop1date = t.chop1date
         AND b.chop1time = t.chop1time
         AND b.chop1room = t.chop1room
         AND b.intop1no = CASE
                WHEN REGEXP_LIKE(TRIM(t.intop1no), '^[0-9]+$')
                THEN TO_NUMBER(TRIM(t.intop1no))
             END
        WHERE b.chop1date BETWEEN :strSDate AND :strEDate
          AND b.chop1room = '0000'
          AND r.chop0dc = '0'";

    /// <summary>
    /// 住院就醫來源的 C18 查詢 SQL，包含必要的欄位、JOIN 條件和篩選條件。
    /// </summary>
    internal const string InpatientBaseSql = @"SELECT
            RTRIM(f.chhospno) AS ClinicCode,
            RTRIM(h.chname) AS ClinicName,
            RTRIM(b.chop1pid) AS PatientIdentifier,
            RTRIM(f.chcase) AS CaseCategory,
            RTRIM(b.chop1pname) AS PatientName,
            RTRIM(b.chop1mrno) AS MedicalRecordNumber,
            RTRIM(s.chsecname) AS EncounterDepartment,
            RTRIM(b.chop1ebid) AS BedNumber,
            RTRIM(b.chop1drname) AS AttendingPhysician,
            b.chop1date AS AdmissionDate,
            SUBSTR(b.chop1eoutdate, 1, 7) AS DischargeDate,
            RTRIM(b.chop1icd1) AS DiagnosisCode1,
            RTRIM(b.chop1icd2) AS DiagnosisCode2,
            RTRIM(b.chop1icd3) AS DiagnosisCode3,
            RTRIM(i1.chicd9cname) AS DiagnosisName1,
            RTRIM(i2.chicd9cname) AS DiagnosisName2,
            RTRIM(i3.chicd9cname) AS DiagnosisName3,
            RTRIM(t.chagree) AS NetworkConsent,
            RTRIM(t.chwhole) AS CompleteResponse,
            CASE
                WHEN RTRIM(b.chop1tranin) IS NOT NULL
                 AND b.chop1tranout = 'N'
                 AND b.chop1sec <> '0340' THEN '轉入'
                WHEN RTRIM(b.chop1tranin) IS NOT NULL
                 AND b.chop1tranout = 'Y'
                 AND b.chop1sec <> '0340' THEN '轉出'
                ELSE ''
            END AS ReferralAnnotation,
            b.chop1date AS SortEncounterDate,
            b.chop1time AS SortEncounterTime,
            b.chop1room AS SortClinicRoom,
            b.intop1no AS SortEncounterSequence,
            ROWIDTOCHAR(f.ROWID) AS MemberSourceRowId,
            ROWIDTOCHAR(t.ROWID) AS ReferralSourceRowId,
            ROWIDTOCHAR(b.ROWID) AS EncounterSourceRowId
        FROM IpdBasicTbl b
        JOIN GenReferralMemberTbl f
          ON b.chop1pid = f.chid
         AND f.chyear = :memberYear
        JOIN GenSectionTbl s
          ON b.chop1sec = s.chsecno
        LEFT JOIN OpdTFHospitalTbl h
          ON f.chhospno = h.chhospno
        LEFT JOIN GenICD9Tbl i1
          ON b.chop1icd1 = i1.chicd9no
        LEFT JOIN GenICD9Tbl i2
          ON b.chop1icd2 = i2.chicd9no
        LEFT JOIN GenICD9Tbl i3
          ON b.chop1icd3 = i3.chicd9no
        LEFT JOIN OpdTFTbl t
          ON b.chop1date = t.chop1date
         AND b.chop1time = t.chop1time
         AND b.chop1room = t.chop1room
         AND b.intop1no = CASE
                WHEN REGEXP_LIKE(TRIM(t.intop1no), '^[0-9]+$')
                THEN TO_NUMBER(TRIM(t.intop1no))
             END
        WHERE b.chop1date BETWEEN :strSDate AND :strEDate
          AND (TRIM(b.chop1clmflg) <> 'D' OR b.chop1clmflg IS NULL)";

    private const string PublicProjection = @"ClinicCode,
            ClinicName,
            PatientIdentifier,
            CaseCategory,
            PatientName,
            MedicalRecordNumber,
            EncounterDepartment,
            BedNumber,
            AttendingPhysician,
            AdmissionDate,
            DischargeDate,
            DiagnosisCode1,
            DiagnosisCode2,
            DiagnosisCode3,
            DiagnosisName1,
            DiagnosisName2,
            DiagnosisName3,
            NetworkConsent,
            CompleteResponse,
            ReferralAnnotation";

    private const string StableOrder = @"SortEncounterDate,
            SortEncounterTime,
            SortClinicRoom,
            SortEncounterSequence,
            MemberSourceRowId,
            ReferralSourceRowId,
            EncounterSourceRowId";

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<ReferralMemberReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.ExecuteScalar<int>(GetCountSql(searchCondition.EncounterSource), CreateParameters(searchCondition));
    }

    public List<ReferralMemberReportViewModel> GetPage(SearchReportCondition searchCondition)
    {
        using IDbConnection connection = CreateConnection();
        return connection.Query<ReferralMemberReportViewModel>(
            GetPageSql(searchCondition.EncounterSource),
            CreateParameters(searchCondition)).ToList();
    }

    internal static string GetBaseSql(string? encounterSource) => encounterSource switch
    {
        EncounterSources.Emergency => EmergencyBaseSql,
        EncounterSources.Inpatient => InpatientBaseSql,
        _ => throw new ArgumentException("不支援的 C18 就醫來源。", nameof(encounterSource))
    };

    internal static string GetCountSql(string? encounterSource) =>
        $"SELECT COUNT(*) FROM ({GetBaseSql(encounterSource)}) C18Rows";

    internal static string GetPageSql(string? encounterSource) =>
        $@"SELECT
            {PublicProjection}
        FROM ({GetBaseSql(encounterSource)}) C18Rows
        ORDER BY
            {StableOrder}
        OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY";

    internal static object CreateParameters(SearchReportCondition searchCondition)
    {
        GetBaseSql(searchCondition.EncounterSource);
        var pageNumber = searchCondition.PageNumber ?? 1;
        var pageSize = searchCondition.PageSize ?? 10;
        string startDate = searchCondition.StartDate
            ?? throw new ArgumentException("C18 缺少起始日期。", nameof(searchCondition));

        if (startDate.Length < 3)
        {
            throw new ArgumentException("C18 起始日期不是有效的民國日期。", nameof(searchCondition));
        }

        return new
        {
            strSDate = startDate,
            strEDate = searchCondition.EndDate,
            memberYear = startDate[..3],
            rowOffset = ((long)pageNumber - 1) * pageSize,
            pageSize
        };
    }

    private IDbConnection CreateConnection() =>
        new OracleConnection(_connectionStringProvider.GetConnectionString());
}
