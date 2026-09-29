namespace OpdAccrRptWeb.Repositories;

public static class M2DoctorMonthlySql
{
    public const string ActualVisitNewCutoffMonth = "10508";
    public const string RoomTypeUExclusionDate = "1051001";

    public const string Statistics = """
SELECT
    DECODE(SUBSTR(A.chDeptId,1,4),
      '0420','0450','0221','0220','0222','0220','0223','0220','0224','0220',
      '0225','0220','0226','0220','0227','0220','0228','0220',
      CASE WHEN LENGTH(A.chDeptId)>=5 AND SUBSTR(A.chDeptId,LENGTH(A.chDeptId),1)
        IN ('A','B','C','D','E','F','M','*')
        THEN SUBSTR(A.chDeptId,1,LENGTH(A.chDeptId)-1) ELSE A.chDeptId END) AS chSecNo,
    B.chSecName, A.chODrId, C.chDocName,
    SUBSTR(A.chDate,6,2) AS report_day,
    CASE
      WHEN :visit_scope='ALL' AND :time_slot='ALL' THEN TO_NUMBER(A.chOpTime1SQty)+TO_NUMBER(A.chOpTime2Qty)+TO_NUMBER(A.chOpTime3Qty)
      WHEN :visit_scope='ALL' AND :time_slot='MORNING' THEN TO_NUMBER(A.chOpTime1SQty)
      WHEN :visit_scope='ALL' AND :time_slot='AFTERNOON' THEN TO_NUMBER(A.chOpTime2Qty)
      WHEN :visit_scope='ALL' AND :time_slot='NIGHT' THEN TO_NUMBER(A.chOpTime3Qty)
      WHEN :visit_scope='OUTPATIENT' AND :time_slot='ALL' THEN TO_NUMBER(A.chOpTime1SQty)+TO_NUMBER(A.chOpTime2Qty)+TO_NUMBER(A.chOpTime3Qty)-TO_NUMBER(A.chEmTime1Qty)-TO_NUMBER(A.chEmTime2Qty)-TO_NUMBER(A.chEmTime3Qty)
      WHEN :visit_scope='OUTPATIENT' AND :time_slot='MORNING' THEN TO_NUMBER(A.chOpTime1SQty)-TO_NUMBER(A.chEmTime1Qty)
      WHEN :visit_scope='OUTPATIENT' AND :time_slot='AFTERNOON' THEN TO_NUMBER(A.chOpTime2Qty)-TO_NUMBER(A.chEmTime2Qty)
      WHEN :visit_scope='OUTPATIENT' AND :time_slot='NIGHT' THEN TO_NUMBER(A.chOpTime3Qty)-TO_NUMBER(A.chEmTime3Qty)
      WHEN :visit_scope='EMERGENCY' AND :time_slot='ALL' THEN TO_NUMBER(A.chEmTime1Qty)+TO_NUMBER(A.chEmTime2Qty)+TO_NUMBER(A.chEmTime3Qty)
      WHEN :visit_scope='EMERGENCY' AND :time_slot='MORNING' THEN TO_NUMBER(A.chEmTime1Qty)
      WHEN :visit_scope='EMERGENCY' AND :time_slot='AFTERNOON' THEN TO_NUMBER(A.chEmTime2Qty)
      WHEN :visit_scope='EMERGENCY' AND :time_slot='NIGHT' THEN TO_NUMBER(A.chEmTime3Qty)
    END AS DayTotalNumber
FROM OpdRegStatsTbl A, GenSectionTbl B, GenDoctorTbl C
WHERE A.chDate LIKE :report_month || '%'
  AND DECODE(SUBSTR(A.chDeptId,1,4),
      '0420','0450','0221','0220','0222','0220','0223','0220','0224','0220',
      '0225','0220','0226','0220','0227','0220','0228','0220','0212','0212*',
      CASE WHEN LENGTH(A.chDeptId)>=5 AND SUBSTR(A.chDeptId,LENGTH(A.chDeptId),1)
        IN ('A','B','C','D','E','F','M','*')
        THEN SUBSTR(A.chDeptId,1,LENGTH(A.chDeptId)-1) ELSE A.chDeptId END)=RTRIM(B.chSecNo(+))
  AND A.chODrId=C.chDocNo
ORDER BY chSecNo,A.chODrId,A.chDate
""";

    private const string ActualVisitTemplate = """
SELECT CASE WHEN LENGTH(J.chSecNo)>=5 AND SUBSTR(J.chSecNo,LENGTH(J.chSecNo),1)
  IN ('A','B','C','D','E','F','M','*') THEN SUBSTR(J.chSecNo,1,LENGTH(J.chSecNo)-1)
  ELSE J.chSecNo END AS chSecNo,
  J.chODrId,J.report_day,J.DayTotalNumber,S.chSecName,D.chDocName
FROM GenSectionTbl S, GenDoctorTbl D, (
 SELECT DECODE(SUBSTR(R.chOp0SecNo,1,4),
   '0420','0450','0221','0220','0222','0220','0223','0220','0224','0220',
   '0225','0220','0226','0220','0227','0220','0228','0220','0212','0212*',
   __SECTION_CASE__) AS chSecNo,
   RTRIM(B.chOp1DrId) AS chODrId,SUBSTR(R.chOp0Date,6,2) AS report_day,
   COUNT(*) AS DayTotalNumber
 FROM OpdRegPtntbl R,OpdBasicTbl B
 WHERE R.chOp0Date=B.chOp1Date AND R.chOp0Time=B.chOp1Time
   AND R.chOp0Room=B.chOp1Room AND R.intOp0No=B.intOp1No
   AND R.chOp0Date=:report_month || :day
   AND R.chOp0Room NOT IN ('RRRR','SSSS','ZZZZ')
   AND R.chOp0Date>=SUBSTR(R.chOp0CDate,1,7)
   AND R.chOp0Type<>'20' AND R.chOp0QuoteFlg<>'N' AND R.chOp0SecNo<>'0297*'
   AND (:report_month || :day < '1051001' OR NOT (R.chOp0SecNo IN ('0330','14011') AND R.chOp0RoomType='U') OR R.chOp0SecNo IS NULL OR R.chOp0RoomType IS NULL)
   AND (:visit_scope='ALL' OR (:visit_scope='OUTPATIENT' AND R.chOp0RoomType<>'E') OR (:visit_scope='EMERGENCY' AND R.chOp0RoomType='E'))
   AND (:time_slot='ALL' OR (:time_slot='MORNING' AND R.chOp0Time='1') OR (:time_slot='AFTERNOON' AND R.chOp0Time='2') OR (:time_slot='NIGHT' AND R.chOp0Time='3'))
   AND R.chOp0PMRNo NOT IN ('C36979','1000000') AND R.chOp0DC='0'
 GROUP BY DECODE(SUBSTR(R.chOp0SecNo,1,4),
   '0420','0450','0221','0220','0222','0220','0223','0220','0224','0220',
   '0225','0220','0226','0220','0227','0220','0228','0220','0212','0212*',
   __SECTION_CASE__),RTRIM(B.chOp1DrId),SUBSTR(R.chOp0Date,6,2)
) J
WHERE RPAD(J.chSecNo,7,' ')=S.chSecNo AND J.chODrId=D.chDocNo
ORDER BY chSecNo,J.chODrId,J.report_day
""";

    private const string OldSectionCase = """
CASE WHEN LENGTH(RTRIM(R.chOp0SecNo))>=5
 AND SUBSTR(R.chOp0SecNo,LENGTH(RTRIM(R.chOp0SecNo)),1) IN ('A','B','C','D','E','*')
 THEN SUBSTR(R.chOp0SecNo,1,LENGTH(RTRIM(R.chOp0SecNo))-1) ELSE RTRIM(R.chOp0SecNo) END
""";

    private const string NewSectionCase = """
CASE WHEN R.chOp0SecNo='0250' AND B.chOp1DrId IN ('92431','93461') THEN '12150'
 WHEN LENGTH(RTRIM(R.chOp0SecNo))>=5
 AND SUBSTR(R.chOp0SecNo,LENGTH(RTRIM(R.chOp0SecNo)),1) IN ('A','B','C','D','E','F','M','*')
 THEN SUBSTR(R.chOp0SecNo,1,LENGTH(RTRIM(R.chOp0SecNo))-1) ELSE RTRIM(R.chOp0SecNo) END
""";

    public static string SelectActualVisit(string rocMonth) => ActualVisitTemplate.Replace(
        "__SECTION_CASE__", string.CompareOrdinal(rocMonth, ActualVisitNewCutoffMonth) >= 0
            ? NewSectionCase : OldSectionCase, StringComparison.Ordinal);
}
