namespace OpdAccrRptWeb.Repositories;

internal static class C11Sql
{
    internal const string InpatientOutstandingToEnd = @"
SELECT /*+ index(b,CON_IPDBASIC) */ SUBSTR(b.doctallowdate, 1, 3) AS chAccDate,
       'I' AS chOp1RoomType, '住院' AS chOp1RoomTypeName,
       SUM(d.rlDebtAmt) AS rlOp1SubDebt_S
FROM IpdDebtTbl d
JOIN IpdBasicTbl b ON b.chOp1Date=d.chOp1Date AND b.chOp1Time=d.chOp1Time
 AND b.chOp1Room=d.chOp1Room AND b.intOp1No=d.intOp1No
WHERE RTRIM(d.vchBackFlg) IS NULL AND d.rlDebtAmt > 0
 AND (d.vchDebtAccType <> '3' OR RTRIM(d.vchDebtAccType) IS NULL)
 AND b.doctallowdate <= :EndDateTime
GROUP BY SUBSTR(b.doctallowdate, 1, 3)
ORDER BY SUBSTR(b.doctallowdate, 1, 3)";

    internal const string OutpatientOutstandingToEnd = @"
SELECT SUBSTR(d.chOp1Date, 1, 3) AS chAccDate,
 DECODE(RTRIM(d.chOp1Room), '0000', 'E', 'R') AS chOp1RoomType,
 DECODE(RTRIM(d.chOp1Room), '0000', '急診', '門診') AS chOp1RoomTypeName,
 SUM(d.rlDebtAmt) AS rlOp1SubDebt_S
FROM GenDebtTbl d
WHERE d.chOp1Date <= :EndDate AND RTRIM(d.chBackFlg) IS NULL AND d.rlDebtAmt > 0
GROUP BY SUBSTR(d.chOp1Date, 1, 3), DECODE(RTRIM(d.chOp1Room), '0000', 'E', 'R'),
 DECODE(RTRIM(d.chOp1Room), '0000', '急診', '門診')
ORDER BY SUBSTR(d.chOp1Date, 1, 3), DECODE(RTRIM(d.chOp1Room), '0000', 'E', 'R'),
 DECODE(RTRIM(d.chOp1Room), '0000', '急診', '門診')";

    internal const string InpatientPeriodOutstanding = @"
SELECT /*+ index(b,CON_IPDBASIC) */ SUBSTR(b.doctallowdate, 1, 3) AS chAccDate,
 'I' AS chOp1RoomType, '住院' AS chOp1RoomTypeName, SUM(d.rlDebtAmt) AS rlOp1SubDebt_S
FROM IpdDebtTbl d
JOIN IpdBasicTbl b ON b.chOp1Date=d.chOp1Date AND b.chOp1Time=d.chOp1Time
 AND b.chOp1Room=d.chOp1Room AND b.intOp1No=d.intOp1No
WHERE RTRIM(d.vchBackFlg) IS NULL AND d.rlDebtAmt > 0
 AND (d.vchDebtAccType <> '3' OR RTRIM(d.vchDebtAccType) IS NULL)
 AND b.doctallowdate BETWEEN :StartDateTime AND :EndDateTime
GROUP BY SUBSTR(b.doctallowdate, 1, 3)
ORDER BY SUBSTR(b.doctallowdate, 1, 3)";

    internal const string OutpatientPeriodOutstanding = @"
SELECT SUBSTR(d.chOp1Date, 1, 3) AS chAccDate,
 DECODE(RTRIM(d.chOp1Room), '0000', 'E', 'R') AS chOp1RoomType,
 DECODE(RTRIM(d.chOp1Room), '0000', '急診', '門診') AS chOp1RoomTypeName,
 SUM(d.rlDebtAmt) AS rlOp1SubDebt_S
FROM GenDebtTbl d
WHERE d.chOp1Date BETWEEN :StartDate AND :EndDate
 AND RTRIM(d.chBackFlg) IS NULL AND d.rlDebtAmt > 0
GROUP BY SUBSTR(d.chOp1Date, 1, 3), DECODE(RTRIM(d.chOp1Room), '0000', 'E', 'R'),
 DECODE(RTRIM(d.chOp1Room), '0000', '急診', '門診')
ORDER BY SUBSTR(d.chOp1Date, 1, 3), DECODE(RTRIM(d.chOp1Room), '0000', 'E', 'R'),
 DECODE(RTRIM(d.chOp1Room), '0000', '急診', '門診')";

    internal const string InpatientPatientCount = @"
SELECT /*+ index(b,CON_IPDBASIC) */ '住院' AS chOp1RoomTypeName,
 COUNT(DISTINCT d.vchMrNo) AS MrNoCount
FROM IpdDebtTbl d
JOIN IpdBasicTbl b ON b.chOp1Date=d.chOp1Date AND b.chOp1Time=d.chOp1Time
 AND b.chOp1Room=d.chOp1Room AND b.intOp1No=d.intOp1No
WHERE RTRIM(d.vchBackFlg) IS NULL AND d.rlDebtAmt > 0
 AND (d.vchDebtAccType <> '3' OR RTRIM(d.vchDebtAccType) IS NULL)
 AND b.doctallowdate BETWEEN :StartDateTime AND :EndDateTime";

    internal const string OutpatientPatientCount = @"
SELECT DECODE(RTRIM(d.chOp1Room), '0000', '急診', '門診') AS chOp1RoomTypeName,
 COUNT(DISTINCT d.chMrNo) AS MrNoCount
FROM GenDebtTbl d
WHERE d.chOp1Date BETWEEN :StartDate AND :EndDate
 AND RTRIM(d.chBackFlg) IS NULL AND d.rlDebtAmt > 0
 AND DECODE(RTRIM(d.chOp1Room), '0000', '急診', '門診') = :RoomTypeName
GROUP BY DECODE(RTRIM(d.chOp1Room), '0000', '急診', '門診')
ORDER BY DECODE(RTRIM(d.chOp1Room), '0000', '急診', '門診')";
}
