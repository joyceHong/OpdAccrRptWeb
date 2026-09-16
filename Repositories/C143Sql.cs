namespace OpdAccrRptWeb.Repositories;

internal static class C143Sql
{
    internal const string OutpatientProjection = """
        WITH tmp_a AS (
            SELECT /*+ index(A I_OPDRECRPT_PDEBTDM_OP1DATE) */
                   A.chOp1RoomTypeName, A.chOp1MrNo, A.chOp1Date2,
                   SUM(CASE WHEN A.chOp4PFin1 IN ('01', '35') THEN A.rlOp1Sub6 ELSE 0 END) AS sub6_01_35,
                   SUM(CASE WHEN A.chOp4PFin1 = '30' THEN A.rlOp1Sub6 ELSE 0 END) AS sub6_30,
                   SUM(A.rlOp1Sub6) AS sub6
            FROM OpdRecRpt_PDebtDM A
            WHERE A.chOp1Date BETWEEN :StartDate AND :EndDate
              AND A.chOp1Date2 >= :StartDate
              AND (A.chDC = '0' OR RTRIM(A.chDC) IS NULL)
            GROUP BY A.chOp1RoomTypeName, A.chOp1MrNo, A.chOp1Date2
            HAVING ABS(SUM(CASE WHEN A.chOp4PFin1 IN ('01', '35') THEN A.rlOp1Sub6 ELSE 0 END)) > 10
                OR ABS(SUM(CASE WHEN A.chOp4PFin1 = '30' THEN A.rlOp1Sub6 ELSE 0 END)) > 10
        ),
        tmp_b AS (
            SELECT * FROM (
                SELECT /*+ index(GenDebtTbl IDX_GENDEBTTBL_CHBILLDATE) */
                       DECODE(RTRIM(chOp1Room), '0000', '急診', '門診') AS chOp1RoomTypeName,
                       RTRIM(chMrNo) AS chMrNo, chOp1Date,
                       SUM(DECODE(chBackFlg, 'D', 0, NVL(rlDebtAmt, 0))) AS rlDebtAmt,
                       SUM(CASE WHEN SUBSTR(chBackDate, 1, 7) > :EndDate THEN NVL(rlDebtAmt, 0)
                                ELSE DECODE(chBackFlg, 'D', 0, NVL(rlDebtAmt, 0) - NVL(rlBackAmt, 0)) END) AS rlDebtAmt2
                FROM GenDebtTbl
                WHERE chBillDate BETWEEN :StartDate AND :EndDate
                  AND chOp1Date >= :StartDate
                  AND (((SUBSTR(chBackDate, 1, 7) <= :EndDate OR chBackDate IS NULL)
                        AND (chBackFlg NOT IN ('D', 'T') OR RTRIM(chBackFlg) IS NULL))
                       OR (SUBSTR(chBackDate, 1, 7) > :EndDate AND chBackFlg IN ('D', 'T', 'R')))
                GROUP BY DECODE(RTRIM(chOp1Room), '0000', '急診', '門診'), chMrNo, chOp1Date
            ) WHERE rlDebtAmt2 <> 0 AND ABS(rlDebtAmt2) > 10
        ),
        tmp_c AS (
            SELECT COALESCE(AA.chOp1RoomTypeName, BB.chOp1RoomTypeName) AS chOp1RoomTypeName,
                   COALESCE(AA.chOp1Date2, BB.chOp1Date) AS chOp1Date,
                   COALESCE(AA.chOp1MrNo, BB.chMrNo) AS chOp1MrNo,
                   AA.sub6_01_35, AA.sub6_30, AA.sub6, BB.rlDebtAmt, BB.rlDebtAmt2,
                   NVL(AA.sub6, 0) - NVL(BB.rlDebtAmt2, 0) AS diff
            FROM tmp_a AA FULL OUTER JOIN tmp_b BB
              ON AA.chOp1RoomTypeName = BB.chOp1RoomTypeName
             AND AA.chOp1MrNo = BB.chMrNo AND AA.chOp1Date2 = BB.chOp1Date
            WHERE :ReportType = '2' OR ABS(NVL(AA.sub6, 0) - NVL(BB.rlDebtAmt2, 0)) <> 0
        ),
        opd_basic_ranked AS (
            SELECT chOp1Date, chOp1MrNo, chOp1EOutDate, chOp1Room,
                   ROW_NUMBER() OVER (PARTITION BY chOp1Date, chOp1MrNo
                     ORDER BY DECODE(RTRIM(chOp1Room), '0000', 0, 1), chOp1EOutDate DESC NULLS LAST) AS rn
            FROM OpdBasicTbl
        ),
        opd_basic_one AS (
            SELECT chOp1Date, chOp1MrNo, chOp1EOutDate, chOp1Room FROM opd_basic_ranked WHERE rn = 1
        )
        SELECT CC.chOp1RoomTypeName AS EncounterType, CC.chOp1Date AS VisitDate,
               CC.chOp1MrNo AS MedicalRecordNumber, SUBSTR(B.chOp1EOutDate, 1, 7) AS EmergencyDepartureDate,
               CC.sub6_01_35 AS AccountingGeneralSelfPay, CC.sub6_30 AS AccountingInsuranceSelfPay,
               CC.sub6 AS AccountingOutstanding, CC.rlDebtAmt AS BillingDebt,
               CC.rlDebtAmt2 AS BillingOutstanding, CC.diff AS Difference
        FROM tmp_c CC LEFT OUTER JOIN opd_basic_one B
          ON DECODE(CC.chOp1RoomTypeName, '急診', '0000  ') = B.chOp1Room
         AND CC.chOp1Date = B.chOp1Date AND RPAD(CC.chOp1MrNo, 10, ' ') = B.chOp1MrNo
         AND B.chOp1Room = '0000'
        WHERE CC.chOp1RoomTypeName = '門診'
           OR (CC.chOp1RoomTypeName = '急診' AND B.chOp1EOutDate IS NOT NULL)
        """;

    internal const string OutpatientOrderBy =
        " ORDER BY CASE WHEN Difference = 0 THEN 0 ELSE 1 END DESC, EncounterType, VisitDate, MedicalRecordNumber";

    internal const string InpatientProjection = """
        WITH tmp_a AS (
            SELECT /*+ index(A IPDTRANCOLEMRNOTBL_PK) index(C CON_IPDBASIC) use_hash(A,C) */
                   A.chMrNo, A.chDate, A.chTime, A.chRoom, A.intNo,
                   SUBSTR(C.doctAllowDate, 1, 7) AS doctAllowDate, C.chOp1ClmFlg,
                   SUM(NVL(A.intSelfAmt, 0)) AS intSelfAmt,
                   SUM(NVL(A.intClaimAmt, 0)) AS intClaimAmt,
                   SUM(NVL(A.intPartAmt, 0)) AS intPartAmt
            FROM IpdTranColeMrNoTbl A JOIN IpdBasicTbl C
              ON A.chDate = C.chOp1Date AND A.chTime = C.chOp1Time
             AND A.chRoom = RTRIM(C.chOp1Room) AND A.intNo = C.intOp1No
            WHERE A.chIDate <= :EndDate AND A.chDate >= :StartDate
              AND (A.chDC = '0' OR RTRIM(A.chDC) IS NULL)
              AND (A.chDate, A.chTime, A.chRoom, A.intNo) NOT IN (
                    SELECT /*+ index(T IPDTRANCOLEMRNOTBL_PK) */ T.chDate, T.chTime, T.chRoom, T.intNo
                    FROM IpdTranColeMrNoTbl T WHERE T.chIDate <= :EndDate AND T.chDC = '1')
            GROUP BY A.chMrNo, A.chDate, A.chTime, A.chRoom, A.intNo,
                     SUBSTR(C.doctAllowDate, 1, 7), C.chOp1ClmFlg
        ),
        tmp_b AS (
            SELECT AA.*,
                   SUM(intSelfAmt) OVER (PARTITION BY chDate, chTime, chRoom, intNo) AS intSelfAmt2,
                   SUM(intClaimAmt) OVER (PARTITION BY chDate, chTime, chRoom, intNo) AS intClaimAmt2,
                   SUM(intPartAmt) OVER (PARTITION BY chDate, chTime, chRoom, intNo) AS intPartAmt2
            FROM tmp_a AA
        ),
        tmp_c AS (
            SELECT chMrNo, chDate, chTime, chRoom, intNo, doctAllowDate,
                   intSelfAmt, intClaimAmt, intPartAmt,
                   intSelfAmt + intClaimAmt + intPartAmt AS s,
                   intSelfAmt2 + intClaimAmt2 + intPartAmt2 AS s2
            FROM tmp_b
            WHERE (ABS(intSelfAmt2 + intClaimAmt2) > 10 OR ABS(intPartAmt2) > 10)
              AND LNNVL(chOp1ClmFlg = 'D')
        ),
        tmp_x AS (
            SELECT * FROM (
                SELECT /*+ index(D PK_IPDDEBTTBL) */ B.chOp1MrNo, D.chOp1Date, D.chOp1Time,
                       RTRIM(D.chOp1Room) AS chOp1Room, D.intOp1No,
                       SUBSTR(B.doctAllowDate, 1, 7) AS doctAllowDate,
                       SUM(DECODE(D.vchBackFlg, 'D', 0, NVL(D.rlDebtAmt, 0))) AS rlDebtAmt,
                       SUM(CASE WHEN SUBSTR(D.vchBackDate, 1, 7) > :EndDate THEN NVL(D.rlDebtAmt, 0)
                                ELSE DECODE(D.vchBackFlg, 'D', 0,
                                     NVL(D.rlDebtAmt, 0) - NVL(D.rlBackAmt, 0)) END) AS rlDebtAmt2
                FROM IpdBasicTbl B JOIN IpdDebtTbl D
                  ON D.chOp1Date = B.chOp1Date AND D.chOp1Time = B.chOp1Time
                 AND D.chOp1Room = B.chOp1Room AND D.intOp1No = B.intOp1No
                LEFT OUTER JOIN IpdBasic2Tbl C
                  ON B.chOp1Date = C.chOp1Date AND B.chOp1Time = C.chOp1Time
                 AND B.chOp1Room = C.chOp1Room AND B.intOp1No = C.intOp1No
                 AND LNNVL(C.vchAccLock = '1')
                WHERE D.vchBillDate BETWEEN :StartDate AND :EndDate AND B.chOp1Date >= :StartDate
                  AND LNNVL(B.chOp1ClmFlg = 'D')
                  AND (((SUBSTR(D.vchBackDate, 1, 7) <= :EndDate OR RTRIM(D.vchBackDate) IS NULL)
                        AND (D.vchBackFlg NOT IN ('D', 'T') OR RTRIM(D.vchBackFlg) IS NULL))
                       OR (SUBSTR(D.vchBackDate, 1, 7) > :EndDate AND D.vchBackFlg IN ('D', 'T', 'R')))
                GROUP BY B.chOp1MrNo, D.chOp1Date, D.chOp1Time, D.chOp1Room,
                         D.intOp1No, SUBSTR(B.doctAllowDate, 1, 7)
            ) WHERE rlDebtAmt2 <> 0 AND ABS(rlDebtAmt2) > 10
        )
        SELECT COALESCE(CC.chMrNo, XX.chOp1MrNo) AS MedicalRecordNumber,
               COALESCE(CC.chDate, XX.chOp1Date) AS VisitDate,
               COALESCE(CC.intNo, XX.intOp1No) AS SequenceNumber,
               COALESCE(CC.doctAllowDate, XX.doctAllowDate) AS DischargeDate,
               CC.intSelfAmt AS AccountingGeneralSelfPay, CC.intClaimAmt AS AccountingInsuranceSelfPay,
               CC.intPartAmt AS AccountingCopayment, CC.s AS AccountingOutstanding,
               CC.s2 AS AccountingMergedOutstanding, XX.rlDebtAmt AS BillingDebt,
               XX.rlDebtAmt2 AS BillingOutstanding,
               NVL(CC.s, 0) - NVL(XX.rlDebtAmt2, 0) AS Difference
        FROM tmp_c CC FULL OUTER JOIN tmp_x XX
          ON CC.chDate = XX.chOp1Date AND CC.chTime = XX.chOp1Time
         AND CC.chRoom = XX.chOp1Room AND CC.intNo = XX.intOp1No
        WHERE (:ReportType = '2' OR
              (ABS(NVL(CC.s2, 0) - NVL(XX.rlDebtAmt2, 0)) > 10
               AND COALESCE(CC.doctAllowDate, XX.doctAllowDate) <= :EndDate))
          AND ((:DischargeGroup = 1 AND COALESCE(CC.doctAllowDate, XX.doctAllowDate) IS NOT NULL)
               OR (:DischargeGroup = 2 AND COALESCE(CC.doctAllowDate, XX.doctAllowDate) IS NULL))
        """;

    internal const string InpatientOrderBy = """
         ORDER BY CASE WHEN DischargeDate <= :EndDate THEN 1 WHEN DischargeDate IS NOT NULL THEN 2 ELSE 3 END,
                  CASE WHEN ABS(NVL(AccountingMergedOutstanding, 0) - NVL(BillingOutstanding, 0)) <= 10 THEN 0 ELSE 1 END DESC,
                  VisitDate, SequenceNumber, MedicalRecordNumber
        """;

    internal static string Count(string projection) => $"SELECT COUNT(*) FROM ({projection}) C143Rows";
    internal static string Page(string projection, string orderBy) =>
        $"SELECT * FROM ({projection}) C143Rows{orderBy} OFFSET :RowOffset ROWS FETCH NEXT :PageSize ROWS ONLY";
}
