namespace OpdAccrRptWeb.Repositories;

public static class M1DoctorDailySql
{
    public const string CutoffRocDate = "1000901";

    public const string OnOrAfterCutoff = """
SELECT
    CASE
        WHEN SUBSTR(A.chDeptId, 1, 4) = '0420' THEN '0450'
        ELSE CASE
            WHEN LENGTH(RTRIM(A.chDeptId)) >= 5
             AND SUBSTR(A.chDeptId, LENGTH(RTRIM(A.chDeptId)), 1)
                 IN ('A','B','C','D','E','F','M','*')
            THEN SUBSTR(A.chDeptId, 1, LENGTH(RTRIM(A.chDeptId)) - 1)
            ELSE RTRIM(A.chDeptId)
        END
    END AS chSecNo,
    B.chSecName,
    A.chODrId,
    C.chDocName,
    SUM(TO_NUMBER(A.chOp1SQty) + TO_NUMBER(A.chOp1HtQty)
      + TO_NUMBER(A.chOp2SQty) + TO_NUMBER(A.chOp2HtQty)) AS s1,
    SUM(TO_NUMBER(A.chOp1HcQty) + TO_NUMBER(A.chOp2HcQty)) AS s2,
    SUM(TO_NUMBER(A.chOpTime1SQty) - TO_NUMBER(A.chEmTime1Qty)) AS s3,
    SUM(TO_NUMBER(A.chOpTime2Qty)  - TO_NUMBER(A.chEmTime2Qty)) AS s4,
    SUM(TO_NUMBER(A.chOpTime3Qty)  - TO_NUMBER(A.chEmTime3Qty)) AS s5,
    SUM(TO_NUMBER(A.chOpBookQty)) AS s6,
    SUM(TO_NUMBER(A.chEm1SQty) + TO_NUMBER(A.chEm1HtQty)
      + TO_NUMBER(A.chEm2SQty) + TO_NUMBER(A.chEm2HtQty)) AS s7,
    SUM(TO_NUMBER(A.chEm1HcQty) + TO_NUMBER(A.chEm2HcQty)) AS s8,
    SUM(TO_NUMBER(A.chEmTime1Qty)) AS s9,
    SUM(TO_NUMBER(A.chEmTime2Qty)) AS s10,
    SUM(TO_NUMBER(A.chEmTime3Qty)) AS s11
FROM OpdRegStatsTbl A, GenSectionTbl B, GenDoctorTbl C
WHERE A.chDate = :report_date
  AND CASE
        WHEN SUBSTR(A.chDeptId, 1, 4) = '0420' THEN '0450'
        WHEN SUBSTR(A.chDeptId, 1, 4) = '0212' THEN '0212*'
        ELSE CASE
            WHEN LENGTH(RTRIM(A.chDeptId)) >= 5
             AND SUBSTR(A.chDeptId, LENGTH(RTRIM(A.chDeptId)), 1)
                 IN ('A','B','C','D','E','F','M','*')
            THEN SUBSTR(A.chDeptId, 1, LENGTH(RTRIM(A.chDeptId)) - 1)
            ELSE RTRIM(A.chDeptId)
        END
      END = RTRIM(B.chSecNo(+))
  AND A.chODrId = C.chDocNo
GROUP BY
    CASE
        WHEN SUBSTR(A.chDeptId, 1, 4) = '0420' THEN '0450'
        ELSE CASE
            WHEN LENGTH(RTRIM(A.chDeptId)) >= 5
             AND SUBSTR(A.chDeptId, LENGTH(RTRIM(A.chDeptId)), 1)
                 IN ('A','B','C','D','E','F','M','*')
            THEN SUBSTR(A.chDeptId, 1, LENGTH(RTRIM(A.chDeptId)) - 1)
            ELSE RTRIM(A.chDeptId)
        END
    END,
    B.chSecName,
    A.chODrId,
    C.chDocName
""";

    public const string BeforeCutoff = """
SELECT
    CASE
        WHEN SUBSTR(A.chDeptId, 1, 4) = '0420' THEN '0450'
        ELSE CASE
            WHEN LENGTH(RTRIM(A.chDeptId)) >= 5
             AND SUBSTR(A.chDeptId, LENGTH(RTRIM(A.chDeptId)), 1)
                 IN ('A','B','C','D','E','F','M','*')
            THEN SUBSTR(A.chDeptId, 1, LENGTH(RTRIM(A.chDeptId)) - 1)
            ELSE RTRIM(A.chDeptId)
        END
    END AS chSecNo,
    B.chSecName,
    A.chODrId,
    C.chDocName,
    SUM(TO_NUMBER(A.chOp1SQty) + TO_NUMBER(A.chOp2SQty)) AS s1,
    SUM(TO_NUMBER(A.chOp1HtQty) + TO_NUMBER(A.chOp1HcQty)
      + TO_NUMBER(A.chOp2HtQty) + TO_NUMBER(A.chOp2HcQty)) AS s2,
    SUM(TO_NUMBER(A.chOpTime1SQty) - TO_NUMBER(A.chEmTime1Qty)) AS s3,
    SUM(TO_NUMBER(A.chOpTime2Qty)  - TO_NUMBER(A.chEmTime2Qty)) AS s4,
    SUM(TO_NUMBER(A.chOpTime3Qty)  - TO_NUMBER(A.chEmTime3Qty)) AS s5,
    SUM(TO_NUMBER(A.chOpBookQty)) AS s6,
    SUM(TO_NUMBER(A.chEm1SQty) + TO_NUMBER(A.chEm2SQty)) AS s7,
    SUM(TO_NUMBER(A.chEm1HtQty) + TO_NUMBER(A.chEm1HcQty)
      + TO_NUMBER(A.chEm2HtQty) + TO_NUMBER(A.chEm2HcQty)) AS s8,
    SUM(TO_NUMBER(A.chEmTime1Qty)) AS s9,
    SUM(TO_NUMBER(A.chEmTime2Qty)) AS s10,
    SUM(TO_NUMBER(A.chEmTime3Qty)) AS s11
FROM OpdRegStatsTbl A, GenSectionTbl B, GenDoctorTbl C
WHERE A.chDate = :report_date
  AND CASE
        WHEN SUBSTR(A.chDeptId, 1, 4) = '0420' THEN '0450'
        WHEN SUBSTR(A.chDeptId, 1, 4) = '0212' THEN '0212*'
        ELSE CASE
            WHEN LENGTH(RTRIM(A.chDeptId)) >= 5
             AND SUBSTR(A.chDeptId, LENGTH(RTRIM(A.chDeptId)), 1)
                 IN ('A','B','C','D','E','F','M','*')
            THEN SUBSTR(A.chDeptId, 1, LENGTH(RTRIM(A.chDeptId)) - 1)
            ELSE RTRIM(A.chDeptId)
        END
      END = RTRIM(B.chSecNo(+))
  AND A.chODrId = C.chDocNo
GROUP BY
    CASE
        WHEN SUBSTR(A.chDeptId, 1, 4) = '0420' THEN '0450'
        ELSE CASE
            WHEN LENGTH(RTRIM(A.chDeptId)) >= 5
             AND SUBSTR(A.chDeptId, LENGTH(RTRIM(A.chDeptId)), 1)
                 IN ('A','B','C','D','E','F','M','*')
            THEN SUBSTR(A.chDeptId, 1, LENGTH(RTRIM(A.chDeptId)) - 1)
            ELSE RTRIM(A.chDeptId)
        END
    END,
    B.chSecName,
    A.chODrId,
    C.chDocName
""";

    public static string Select(string rocDate) =>
        string.CompareOrdinal(rocDate, CutoffRocDate) >= 0 ? OnOrAfterCutoff : BeforeCutoff;
}

