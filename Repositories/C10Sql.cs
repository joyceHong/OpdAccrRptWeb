namespace OpdAccrRptWeb.Repositories;

internal static class C10Sql
{
    internal const string OutpatientDebt = @"
SELECT A.chOp1Date AS VisitDate, A.chOp1Time AS VisitTime,
       A.chOp1Room AS VisitRoom, A.intOp1No AS VisitNumber,
       A.chOp1InSeq AS AdmissionSequence,
       C.chTelH AS HomePhone, C.chAdd1 AS Address1, C.chAdd2 AS Address2,
       C.chMREcall AS ContactName, C.chMRRelation AS ContactRelation,
       C.chMRRTel AS ContactPhone, A.chOp1RoomType AS RoomType,
       RTRIM(A.chOp1MrNo) AS MedicalRecordNumber,
       A.chOp1PName AS PatientName, A.chOp1DrName AS DoctorName,
       S.chSecName AS DepartmentName, A.chOp1EOutDate AS DischargeDate,
       SUM(B.rlDebtAMT) AS DebtAmount
FROM OpdBasicTbl A
JOIN GenDebtTbl B
  ON B.chOp1Date = A.chOp1Date AND B.chOp1Time = A.chOp1Time
 AND B.chOp1Room = A.chOp1Room AND B.intOp1No = A.intOp1No
JOIN OpdMRBasicTbl C ON C.chMrNo = RTRIM(A.chOp1MrNo)
JOIN GenSectionTbl S ON S.chSecNo = A.chOp1Sec
WHERE B.chBillDate BETWEEN :StartDate AND :EndDate
  AND (B.chBackFlg IS NULL OR B.chBackFlg NOT IN ('D', 'R', 'T'))
  AND (:MedicalRecordNumber IS NULL OR RTRIM(B.chMrNo) = :MedicalRecordNumber)
  AND ((:RoomScope = 1 AND A.chOp1RoomType = 'E')
    OR (:RoomScope = 2 AND A.chOp1RoomType <> 'E')
    OR (:RoomScope = 0))
GROUP BY A.chOp1Date, A.chOp1Time, A.chOp1Room, A.intOp1No,
         A.chOp1InSeq, C.chTelH, C.chAdd1, C.chAdd2, C.chMREcall,
         C.chMRRelation, C.chMRRTel, A.chOp1RoomType, A.chOp1MrNo,
         A.chOp1PName, A.chOp1DrName, S.chSecName, A.chOp1EOutDate
ORDER BY A.chOp1Date, A.chOp1Time, A.chOp1Room, A.intOp1No";

    internal const string InpatientDebt = @"
SELECT A.chOp1Date AS VisitDate, A.chOp1Time AS VisitTime,
       A.chOp1Room AS VisitRoom, A.intOp1No AS VisitNumber,
       A.chOp1InSeq AS AdmissionSequence,
       C.chTelH AS HomePhone, C.chAdd1 AS Address1, C.chAdd2 AS Address2,
       C.chMREcall AS ContactName, C.chMRRelation AS ContactRelation,
       C.chMRRTel AS ContactPhone, A.chOp1RoomType AS RoomType,
       RTRIM(A.chOp1MrNo) AS MedicalRecordNumber,
       A.chOp1PName AS PatientName, A.chOp1DrName AS DoctorName,
       S.chSecName AS DepartmentName, A.chOp1EOutDate AS DischargeDate,
       SUM(B.rlDebtAMT) AS DebtAmount
FROM IpdBasicTbl A
JOIN IpdDebtTbl B
  ON B.chOp1Date = A.chOp1Date AND B.chOp1Time = A.chOp1Time
 AND B.chOp1Room = A.chOp1Room AND B.intOp1No = A.intOp1No
JOIN OpdMRBasicTbl C ON C.chMrNo = RTRIM(A.chOp1MrNo)
JOIN GenSectionTbl S ON S.chSecNo = A.chOp1Sec
WHERE B.vchBillDate BETWEEN :StartDate AND :EndDate
  AND (B.vchBackFlg IS NULL OR B.vchBackFlg NOT IN ('D', 'R', 'T'))
  AND (:MedicalRecordNumber IS NULL OR C.chMrNo = :MedicalRecordNumber)
GROUP BY A.chOp1Date, A.chOp1Time, A.chOp1Room, A.intOp1No,
         A.chOp1InSeq, C.chTelH, C.chAdd1, C.chAdd2, C.chMREcall,
         C.chMRRelation, C.chMRRTel, A.chOp1RoomType, A.chOp1MrNo,
         A.chOp1PName, A.chOp1DrName, S.chSecName, A.chOp1EOutDate
ORDER BY A.chOp1Date, A.chOp1Time, A.chOp1Room, A.intOp1No";

    internal static string BuildDetail(string source, int visitCount)
    {
        if (visitCount <= 0) throw new ArgumentOutOfRangeException(nameof(visitCount));
        string visitPredicate = string.Join(" OR ", Enumerable.Range(0, visitCount).Select(i =>
            $"(A.chOp1Date = :VisitDate{i} AND A.chOp1Time = :VisitTime{i} " +
            $"AND A.chOp1Room = :VisitRoom{i} AND A.intOp1No = :VisitNumber{i})"));
        return source == ViewModels.C10Sources.Inpatient
            ? BuildInpatientDetail(visitPredicate)
            : BuildOutpatientDetail(visitPredicate);
    }

    private static string BuildOutpatientDetail(string visits) => $@"
SELECT A.chOp1Date AS VisitDate, A.chOp1Time AS VisitTime,
       A.chOp1Room AS VisitRoom, A.intOp1No AS VisitNumber,
       'Order' AS Source, D.chDctItemName AS ChargeItemName,
       SUM(A.rlOp4Sub6 + A.rlOp4Sub5 + A.rlOp4Sub3) AS Sub6,
       SUM(A.rlOp4Sub3 + A.rlOp4Sub5) AS Sub3,
       SUM(A.rlOp4Sub1) AS Sub1,
       SUM(A.rlOp4Sub2 + A.rlOp4Sub3 + A.rlOp4Sub4 + A.rlOp4Sub6) AS Sub25
FROM OpdOrdTbl A JOIN GenDctItemTbl D ON D.chDctItem = A.chOp4Dct
WHERE ({visits}) AND A.chOp4Stat <> 'DC'
  AND (A.chOp4Proj NOT IN ('I', 'S', 'D') OR RTRIM(A.chOp4Proj) IS NULL)
  AND A.chOp4Dct NOT IN ('58', '56', '57', '80')
GROUP BY A.chOp1Date, A.chOp1Time, A.chOp1Room, A.intOp1No, D.chDctItemName
HAVING SUM(A.rlOp4Sub6 + A.rlOp4Sub5 + A.rlOp4Sub3) <> 0
    OR SUM(A.rlOp4Sub3 + A.rlOp4Sub5) <> 0 OR SUM(A.rlOp4Sub1) <> 0
    OR SUM(A.rlOp4Sub2 + A.rlOp4Sub3 + A.rlOp4Sub4 + A.rlOp4Sub6) <> 0
UNION ALL
SELECT A.chOp1Date, A.chOp1Time, A.chOp1Room, A.intOp1No,
       'Drug', D.chDctItemName,
       SUM(A.rlOp3Sub6 + A.rlOp3Sub5 + A.rlOp3Sub3),
       SUM(A.rlOp3Sub3 + A.rlOp3Sub5), SUM(A.rlOp3Sub1),
       SUM(A.rlOp3Sub2 + A.rlOp3Sub3 + A.rlOp3Sub4 + A.rlOp3Sub6)
FROM OpdDrgTbl A JOIN GenDctItemTbl D ON D.chDctItem = A.chOp3Dct
WHERE ({visits}) AND A.chOp3Stat <> 'DC'
  AND (A.chOp3Proj NOT IN ('I', 'S', 'D') OR RTRIM(A.chOp3Proj) IS NULL)
GROUP BY A.chOp1Date, A.chOp1Time, A.chOp1Room, A.intOp1No, D.chDctItemName
HAVING SUM(A.rlOp3Sub6 + A.rlOp3Sub5 + A.rlOp3Sub3) <> 0
    OR SUM(A.rlOp3Sub3 + A.rlOp3Sub5) <> 0 OR SUM(A.rlOp3Sub1) <> 0
    OR SUM(A.rlOp3Sub2 + A.rlOp3Sub3 + A.rlOp3Sub4 + A.rlOp3Sub6) <> 0";

    private static string BuildInpatientDetail(string visits) => $@"
SELECT A.chOp1Date AS VisitDate, A.chOp1Time AS VisitTime,
       A.chOp1Room AS VisitRoom, A.intOp1No AS VisitNumber,
       'Order' AS Source, D.chDctItemName AS ChargeItemName,
       SUM(A.rlOp4Sub6 + A.rlOp4Sub5 + A.rlOp4Sub3) AS Sub6,
       SUM(A.rlOp4Sub3 + A.rlOp4Sub5) AS Sub3, SUM(A.rlOp4Sub1) AS Sub1,
       SUM(A.rlOp4Sub2 + A.rlOp4Sub3 + A.rlOp4Sub4 + A.rlOp4Sub6) AS Sub25
FROM IpdOrdTbl A JOIN GenDctItemTbl D ON D.chDctItem = A.chOp4Dct
WHERE ({visits}) AND A.chOp4Stat <> 'DC' AND RTRIM(A.chOp4IDate) IS NOT NULL
  AND (A.chOp4Proj NOT IN ('I', 'S', 'D') OR RTRIM(A.chOp4Proj) IS NULL)
  AND A.chOp4Dct NOT IN ('58', '56', '57', '80')
GROUP BY A.chOp1Date, A.chOp1Time, A.chOp1Room, A.intOp1No, D.chDctItemName
HAVING SUM(A.rlOp4Sub6 + A.rlOp4Sub5 + A.rlOp4Sub3) <> 0
    OR SUM(A.rlOp4Sub3 + A.rlOp4Sub5) <> 0 OR SUM(A.rlOp4Sub1) <> 0
    OR SUM(A.rlOp4Sub2 + A.rlOp4Sub3 + A.rlOp4Sub4 + A.rlOp4Sub6) <> 0
UNION ALL
SELECT A.chOp1Date, A.chOp1Time, A.chOp1Room, A.intOp1No,
       'Drug', D.chDctItemName,
       SUM(A.rlOp3Sub6 + A.rlOp3Sub5 + A.rlOp3Sub3),
       SUM(A.rlOp3Sub3 + A.rlOp3Sub5), SUM(A.rlOp3Sub1),
       SUM(A.rlOp3Sub2 + A.rlOp3Sub3 + A.rlOp3Sub4 + A.rlOp3Sub6)
FROM IpdDrgTbl A JOIN GenDctItemTbl D ON D.chDctItem = A.chOp3Dct
WHERE ({visits}) AND A.chOp3Stat <> 'DC'
  AND (A.chOp3Rep3Flg <> 'S' OR RTRIM(A.chOp3Rep3Flg) IS NULL)
  AND (A.chOp3Stat NOT IN ('09', '10', '11', '12') OR RTRIM(A.chOp3Stat) IS NULL)
  AND RTRIM(A.chOp3IDate) IS NOT NULL
  AND (A.chOp3Proj NOT IN ('I', 'S', 'D') OR RTRIM(A.chOp3Proj) IS NULL)
GROUP BY A.chOp1Date, A.chOp1Time, A.chOp1Room, A.intOp1No, D.chDctItemName
HAVING SUM(A.rlOp3Sub6 + A.rlOp3Sub5 + A.rlOp3Sub3) <> 0
    OR SUM(A.rlOp3Sub3 + A.rlOp3Sub5) <> 0 OR SUM(A.rlOp3Sub1) <> 0
    OR SUM(A.rlOp3Sub2 + A.rlOp3Sub3 + A.rlOp3Sub4 + A.rlOp3Sub6) <> 0";
}
