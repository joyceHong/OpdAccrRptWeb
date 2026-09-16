namespace OpdAccrRptWeb.Repositories;

internal static class C144Sql
{
    internal static readonly string InpatientProjection = BuildProjection(inpatient: true);
    internal static readonly string OutpatientEmergencyProjection = BuildProjection(inpatient: false);

    internal const string OrderBy = """
         ORDER BY EncounterType, VisitDate, VisitTime, RoomNumber, SequenceNumber,
                  MedicalRecordNumber, PatientName, DischargeDate, SectionCode,
                  SectionName, DoctorId, DoctorName, PatientIdentity, OutstandingAmount
        """;

    internal static string Count(string projection) =>
        $"SELECT COUNT(*) FROM ({projection}) C144Rows";

    internal static string Page(string projection) =>
        $"SELECT * FROM ({projection}) C144Rows{OrderBy} OFFSET :RowOffset ROWS FETCH NEXT :PageSize ROWS ONLY";

    internal static string All(string projection) =>
        $"SELECT * FROM ({projection}) C144Rows{OrderBy}";

    private static string BuildProjection(bool inpatient)
    {
        string debtTable = inpatient ? "IpdDebtTbl" : "GenDebtTbl";
        string basicTable = inpatient ? "IpdBasicTbl" : "OpdBasicTbl";
        string orderTable = inpatient ? "IpdOrdTbl" : "OpdOrdTbl";
        string drugTable = inpatient ? "IpdDrgTbl" : "OpdDrgTbl";
        string mrn = inpatient ? "X.vchMrNo" : "X.chMrNo";
        string backFlag = inpatient ? "X.vchBackFlg" : "X.chBackFlg";
        string dischargeDate = inpatient ? "Y.doctAllowDate" : "Y.chOp1EOutDate";
        string roomType = inpatient ? "'I'" : "DECODE(RTRIM(X.chOp1Room), '0000', 'E', 'R')";
        string generalIdentity = inpatient ? "= '01'" : "IN ('01','35')";
        string insuredIdentity = inpatient ? "IN ('30','35')" : "= '30'";
        string inpatientOrderDate = inpatient ? "AND RTRIM(B.chOp4IDate) IS NOT NULL" : string.Empty;
        string inpatientDrugDate = inpatient ? "AND RTRIM(B.chOp3IDate) IS NOT NULL" : string.Empty;
        string hint = inpatient ? "/*+ index(X PK_IPDDEBTTBL) */" : "/*+ index(X GENDEBTTBL_PK1991039774690) */";

        return $$"""
            WITH tmp_a AS (
                SELECT {{hint}} {{roomType}} AS chOp1RoomType,
                       {{mrn}} AS chMrNo, Y.chOp1PName,
                       SUBSTR({{dischargeDate}}, 1, 7) AS doctAllowDate,
                       Y.chOp1Sec, Z.chSecName, Y.chOp1DrId, Y.chOp1DrName, Y.chOp1PFin1,
                       X.chOp1Date, X.chOp1Time, X.chOp1Room, X.intOp1No,
                       SUM(NVL(X.rlDebtAmt, 0)) AS rlDebtAmt
                FROM {{debtTable}} X
                JOIN {{basicTable}} Y
                  ON X.chOp1Date = Y.chOp1Date AND X.chOp1Time = Y.chOp1Time
                 AND X.chOp1Room = Y.chOp1Room AND X.intOp1No = Y.intOp1No
                LEFT OUTER JOIN GenSectionTbl Z ON Y.chOp1Sec = Z.chSecNo
                WHERE X.chOp1Date BETWEEN :StartDate AND :EndDate
                  AND {{mrn}} NOT IN ('C36979', '1000000')
                  AND ({{backFlag}} NOT IN ('D', 'R', 'T') OR {{backFlag}} IS NULL)
                GROUP BY {{roomType}}, {{mrn}}, Y.chOp1PName,
                         SUBSTR({{dischargeDate}}, 1, 7), Y.chOp1Sec, Z.chSecName,
                         Y.chOp1DrId, Y.chOp1DrName, Y.chOp1PFin1,
                         X.chOp1Date, X.chOp1Time, X.chOp1Room, X.intOp1No
                HAVING SUM(NVL(X.rlDebtAmt, 0)) <> 0
            ),
            tmp_b AS (
                SELECT AA.chOp1RoomType, AA.chOp1Date, AA.chOp1Time, AA.chOp1Room, AA.intOp1No,
                       AA.chMrNo, AA.chOp1PName, AA.doctAllowDate, AA.chOp1Sec, AA.chSecName,
                       AA.chOp1DrId, AA.chOp1DrName, AA.chOp1PFin1, AA.rlDebtAmt,
                       SUBSTR(B.chOp4Dct,1,2) AS chOp4Dct,
                       SUM(B.rlOp4Sub1+B.rlOp4Sub6) AS sub16,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2)<>'49' AND B.chOp4PFin1 {{generalIdentity}} THEN B.rlOp4Sub1+B.rlOp4Sub6 ELSE 0 END) AS sub16_no49_01,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2)<>'49' AND B.chOp4PFin1 {{insuredIdentity}} THEN B.rlOp4Sub1+B.rlOp4Sub6 ELSE 0 END) AS sub16_no49_30,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2)='49' AND NOT (B.chOp4OrdNo LIKE '49-U%' AND LENGTH(RTRIM(B.chOp4OrdNo))=5) THEN -B.rlOp4Sub2 ELSE 0 END) AS sub2_noDrgPPay,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2)='49' AND (B.chOp4OrdNo LIKE '49-U%' AND LENGTH(RTRIM(B.chOp4OrdNo))=5) THEN -B.rlOp4Sub2 ELSE 0 END) AS sub2_DrgPPay,
                       SUM(B.rlOp4Sub3) AS sub3, SUM(B.rlOp4Sub5) AS sub5,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2) IN ('18','19') AND B.chOp4PFin1 {{generalIdentity}} THEN B.rlOp4Sub1+B.rlOp4Sub6 ELSE 0 END) AS sub16_1819_01,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2) IN ('18','19') AND B.chOp4PFin1 {{insuredIdentity}} THEN B.rlOp4Sub1+B.rlOp4Sub6 ELSE 0 END) AS sub16_1819_30,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2)='36' AND B.chOp4PFin1 {{generalIdentity}} THEN B.rlOp4Sub1+B.rlOp4Sub6 ELSE 0 END) AS sub16_36_01,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2)='36' AND B.chOp4PFin1 {{insuredIdentity}} THEN B.rlOp4Sub1+B.rlOp4Sub6 ELSE 0 END) AS sub16_36_30,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2)='38' AND B.chOp4PFin1 {{generalIdentity}} THEN B.rlOp4Sub1+B.rlOp4Sub6 ELSE 0 END) AS sub16_38_01,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2)='38' AND B.chOp4PFin1 {{insuredIdentity}} THEN B.rlOp4Sub1+B.rlOp4Sub6 ELSE 0 END) AS sub16_38_30,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2)='01' AND B.chOp4PFin1 {{generalIdentity}} THEN B.rlOp4Sub1+B.rlOp4Sub6 ELSE 0 END) AS sub16_01_01,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2)='01' AND B.chOp4PFin1 {{insuredIdentity}} THEN B.rlOp4Sub1+B.rlOp4Sub6 ELSE 0 END) AS sub16_01_30,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2)='25' AND B.chOp4PFin1 {{generalIdentity}} THEN B.rlOp4Sub1+B.rlOp4Sub6 ELSE 0 END) AS sub16_25_01,
                       SUM(CASE WHEN SUBSTR(B.chOp4Dct,1,2)='25' AND B.chOp4PFin1 {{insuredIdentity}} THEN B.rlOp4Sub1+B.rlOp4Sub6 ELSE 0 END) AS sub16_25_30
                FROM tmp_a AA JOIN {{orderTable}} B
                  ON AA.chOp1Date=B.chOp1Date AND AA.chOp1Time=B.chOp1Time
                 AND AA.chOp1Room=B.chOp1Room AND AA.intOp1No=B.intOp1No
                WHERE SUBSTR(B.chOp4Dct,1,2) <= 50
                  AND LNNVL(B.chOp4Stat='DC')
                  AND (B.chOp4Proj NOT IN ('I','S','D') OR RTRIM(B.chOp4Proj) IS NULL)
                  AND (B.rlOp4Sub1<>0 OR B.rlOp4Sub3<>0 OR B.rlOp4Sub5<>0 OR B.rlOp4Sub6<>0)
                  {{inpatientOrderDate}}
                GROUP BY AA.chOp1RoomType, AA.chOp1Date, AA.chOp1Time, AA.chOp1Room, AA.intOp1No,
                         AA.chMrNo, AA.chOp1PName, AA.doctAllowDate, AA.chOp1Sec, AA.chSecName,
                         AA.chOp1DrId, AA.chOp1DrName, AA.chOp1PFin1, AA.rlDebtAmt, SUBSTR(B.chOp4Dct,1,2)
                UNION ALL
                SELECT AA.chOp1RoomType, AA.chOp1Date, AA.chOp1Time, AA.chOp1Room, AA.intOp1No,
                       AA.chMrNo, AA.chOp1PName, AA.doctAllowDate, AA.chOp1Sec, AA.chSecName,
                       AA.chOp1DrId, AA.chOp1DrName, AA.chOp1PFin1, AA.rlDebtAmt,
                       SUBSTR(B.chOp3Dct,1,2) AS chOp4Dct,
                       SUM(B.rlOp3Sub1+B.rlOp3Sub6) AS sub16,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2)<>'49' AND B.chOp3PFin1 {{generalIdentity}} THEN B.rlOp3Sub1+B.rlOp3Sub6 ELSE 0 END) AS sub16_no49_01,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2)<>'49' AND B.chOp3PFin1 {{insuredIdentity}} THEN B.rlOp3Sub1+B.rlOp3Sub6 ELSE 0 END) AS sub16_no49_30,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2)='49' AND NOT (B.chOp3DrgNo LIKE '49-U%' AND LENGTH(RTRIM(B.chOp3DrgNo))=5) THEN -B.rlOp3Sub2 ELSE 0 END) AS sub2_noDrgPPay,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2)='49' AND (B.chOp3DrgNo LIKE '49-U%' AND LENGTH(RTRIM(B.chOp3DrgNo))=5) THEN -B.rlOp3Sub2 ELSE 0 END) AS sub2_DrgPPay,
                       SUM(B.rlOp3Sub3) AS sub3, SUM(B.rlOp3Sub5) AS sub5,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2) IN ('18','19') AND B.chOp3PFin1 {{generalIdentity}} THEN B.rlOp3Sub1+B.rlOp3Sub6 ELSE 0 END) AS sub16_1819_01,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2) IN ('18','19') AND B.chOp3PFin1 {{insuredIdentity}} THEN B.rlOp3Sub1+B.rlOp3Sub6 ELSE 0 END) AS sub16_1819_30,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2)='36' AND B.chOp3PFin1 {{generalIdentity}} THEN B.rlOp3Sub1+B.rlOp3Sub6 ELSE 0 END) AS sub16_36_01,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2)='36' AND B.chOp3PFin1 {{insuredIdentity}} THEN B.rlOp3Sub1+B.rlOp3Sub6 ELSE 0 END) AS sub16_36_30,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2)='38' AND B.chOp3PFin1 {{generalIdentity}} THEN B.rlOp3Sub1+B.rlOp3Sub6 ELSE 0 END) AS sub16_38_01,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2)='38' AND B.chOp3PFin1 {{insuredIdentity}} THEN B.rlOp3Sub1+B.rlOp3Sub6 ELSE 0 END) AS sub16_38_30,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2)='01' AND B.chOp3PFin1 {{generalIdentity}} THEN B.rlOp3Sub1+B.rlOp3Sub6 ELSE 0 END) AS sub16_01_01,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2)='01' AND B.chOp3PFin1 {{insuredIdentity}} THEN B.rlOp3Sub1+B.rlOp3Sub6 ELSE 0 END) AS sub16_01_30,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2)='25' AND B.chOp3PFin1 {{generalIdentity}} THEN B.rlOp3Sub1+B.rlOp3Sub6 ELSE 0 END) AS sub16_25_01,
                       SUM(CASE WHEN SUBSTR(B.chOp3Dct,1,2)='25' AND B.chOp3PFin1 {{insuredIdentity}} THEN B.rlOp3Sub1+B.rlOp3Sub6 ELSE 0 END) AS sub16_25_30
                FROM tmp_a AA JOIN {{drugTable}} B
                  ON AA.chOp1Date=B.chOp1Date AND AA.chOp1Time=B.chOp1Time
                 AND AA.chOp1Room=B.chOp1Room AND AA.intOp1No=B.intOp1No
                WHERE SUBSTR(B.chOp3Dct,1,2) <= 50
                  AND LNNVL(B.chOp3Stat='DC')
                  AND (B.chOp3Proj NOT IN ('I','S','D') OR RTRIM(B.chOp3Proj) IS NULL)
                  AND (B.rlOp3Sub1<>0 OR B.rlOp3Sub3<>0 OR B.rlOp3Sub5<>0 OR B.rlOp3Sub6<>0)
                  AND LNNVL(B.chOp3Rep3Flg='S')
                  AND (B.chOp3Stat NOT IN ('09','10','11','12') OR RTRIM(B.chOp3Stat) IS NULL)
                  {{inpatientDrugDate}}
                GROUP BY AA.chOp1RoomType, AA.chOp1Date, AA.chOp1Time, AA.chOp1Room, AA.intOp1No,
                         AA.chMrNo, AA.chOp1PName, AA.doctAllowDate, AA.chOp1Sec, AA.chSecName,
                         AA.chOp1DrId, AA.chOp1DrName, AA.chOp1PFin1, AA.rlDebtAmt, SUBSTR(B.chOp3Dct,1,2)
            )
            SELECT chOp1RoomType AS EncounterType, chOp1Date AS VisitDate, chOp1Time AS VisitTime,
                   chOp1Room AS RoomNumber, intOp1No AS SequenceNumber, chMrNo AS MedicalRecordNumber,
                   chOp1PName AS PatientName, doctAllowDate AS DischargeDate, chOp1Sec AS SectionCode,
                   chSecName AS SectionName, chOp1DrId AS DoctorId, chOp1DrName AS DoctorName,
                   chOp1PFin1 AS PatientIdentity, ROUND(rlDebtAmt,0) AS OutstandingAmount,
                   ROUND(SUM(sub16),0) AS TotalSelfPayAmount,
                   ROUND(SUM(sub16_no49_01),0) AS GeneralSelfPayAmount,
                   ROUND(SUM(sub16_no49_30),0) AS InsuredSelfPayAmount,
                   ROUND(SUM(sub2_noDrgPPay),0) AS CopaymentAmount,
                   ROUND(SUM(sub2_DrgPPay),0) AS DrugCopaymentAmount,
                   ROUND(SUM(sub3),0) AS DiscountAmount, ROUND(SUM(sub5),0) AS OnAccountAmount,
                   ROUND(SUM(sub16_1819_01),0) AS GeneralMaterialAmount,
                   ROUND(SUM(sub16_1819_30),0) AS InsuredMaterialAmount,
                   ROUND(SUM(sub16_36_01),0) AS GeneralSurgeryAmount,
                   ROUND(SUM(sub16_36_30),0) AS InsuredSurgeryAmount,
                   ROUND(SUM(sub16_38_01),0) AS GeneralAnesthesiaAmount,
                   ROUND(SUM(sub16_38_30),0) AS InsuredAnesthesiaAmount,
                   ROUND(SUM(sub16_01_01),0) AS GeneralDrugAmount,
                   ROUND(SUM(sub16_01_30),0) AS InsuredDrugAmount,
                   ROUND(SUM(sub16_25_01),0) AS GeneralRegistrationAmount,
                   ROUND(SUM(sub16_25_30),0) AS InsuredRegistrationAmount
            FROM tmp_b
            GROUP BY chOp1RoomType, chOp1Date, chOp1Time, chOp1Room, intOp1No, chMrNo,
                     chOp1PName, doctAllowDate, chOp1Sec, chSecName, chOp1DrId, chOp1DrName,
                     chOp1PFin1, rlDebtAmt
            """;
    }
}
