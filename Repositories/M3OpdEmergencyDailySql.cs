namespace OpdAccrRptWeb.Repositories;

public static class M3OpdEmergencyDailySql
{
    private const string SelectFields = """
SELECT
    CASE
        WHEN LENGTH(RTRIM(chDeptId)) >= 5
         AND SUBSTR(chDeptId, LENGTH(RTRIM(chDeptId)), 1) IN ('A','B','C','D','E','F','M','*')
        THEN SUBSTR(chDeptId, 1, LENGTH(RTRIM(chDeptId)) - 1)
        ELSE RTRIM(chDeptId)
    END AS chDeptId,
    SUM(TO_NUMBER(chOp1SQty)) AS s1,
    SUM(TO_NUMBER(chOp1HtQty)) AS s2,
    SUM(TO_NUMBER(chOp1HcQty)) AS s3,
    SUM(TO_NUMBER(chOp2SQty)) AS s4,
    SUM(TO_NUMBER(chOp2HtQty)) AS s5,
    SUM(TO_NUMBER(chOp2HcQty)) AS s6,
    SUM(TO_NUMBER(chEm1SQty)) AS s7,
    SUM(TO_NUMBER(chEm1HtQty)) AS s8,
    SUM(TO_NUMBER(chEm1HcQty)) AS s9,
    SUM(TO_NUMBER(chEm2SQty)) AS s10,
    SUM(TO_NUMBER(chEm2HtQty)) AS s11,
    SUM(TO_NUMBER(chEm2HcQty)) AS s12
FROM OpdRegStatsTbl
""";

    private const string GroupBy = """
GROUP BY CASE
    WHEN LENGTH(RTRIM(chDeptId)) >= 5
     AND SUBSTR(chDeptId, LENGTH(RTRIM(chDeptId)), 1) IN ('A','B','C','D','E','F','M','*')
    THEN SUBSTR(chDeptId, 1, LENGTH(RTRIM(chDeptId)) - 1)
    ELSE RTRIM(chDeptId)
END
""";

    public static readonly string Daily = SelectFields + "\nWHERE chDate = :report_date\n" + GroupBy;
    public static readonly string Monthly = SelectFields +
        "\nWHERE chDate >= :month_start_date AND chDate <= :report_date\n" + GroupBy;
    public static readonly string Yearly = SelectFields +
        "\nWHERE chDate >= :year_start_date AND chDate <= :report_date\n" + GroupBy;

    public const string EmergencyDay = """
SELECT COUNT(*) AS qty
FROM OpdRegPtntbl A, OpdBasicTbl B
WHERE A.chOp0Date=B.chOp1Date AND A.chOp0Time=B.chOp1Time
  AND A.chOp0Room=B.chOp1Room AND A.intOp0No=B.intOp1No
  AND A.chOp0RoomType='E' AND A.chOp0DC='0' AND A.chOp0Date=:report_date
  AND B.chOp1EinDate BETWEEN :day_start_datetime AND :day_end_datetime
""";

    public const string EmergencyEvening = """
SELECT COUNT(*) AS qty
FROM OpdRegPtntbl A, OpdBasicTbl B
WHERE A.chOp0Date=B.chOp1Date AND A.chOp0Time=B.chOp1Time
  AND A.chOp0Room=B.chOp1Room AND A.intOp0No=B.intOp1No
  AND A.chOp0RoomType='E' AND A.chOp0DC='0' AND A.chOp0Date=:report_date
  AND B.chOp1EinDate BETWEEN :evening_start_datetime AND :evening_end_datetime
""";

    public const string EmergencyNight = """
SELECT COUNT(*) AS qty
FROM OpdRegPtntbl A, OpdBasicTbl B
WHERE A.chOp0Date=B.chOp1Date AND A.chOp0Time=B.chOp1Time
  AND A.chOp0Room=B.chOp1Room AND A.intOp0No=B.intOp1No
  AND A.chOp0RoomType='E' AND A.chOp0DC='0' AND A.chOp0Date=:report_date
  AND (B.chOp1EinDate BETWEEN :night_first_start_datetime AND :night_first_end_datetime
    OR B.chOp1EinDate BETWEEN :night_second_start_datetime AND :night_second_end_datetime)
""";

    public const string Kpis = """
SELECT SUM(TO_NUMBER(chOpTime1SQty)) AS s1,
       SUM(TO_NUMBER(chOpTime2Qty)) AS s2,
       SUM(TO_NUMBER(chOpTime3Qty)) AS s3,
       SUM(TO_NUMBER(chOpBookQty)) AS s7,
       SUM(TO_NUMBER(chOpNotShownQty)) AS s8
FROM OpdRegStatsTbl
WHERE chDate=:report_date
""";

    public const string DepartmentName = """
SELECT chSecName
FROM GenSectionTbl
WHERE chSecNo LIKE :old_department_id || '%'
ORDER BY chSecNo
FETCH FIRST 1 ROW ONLY
""";
}
