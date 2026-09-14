namespace OpdAccrRptWeb.Repositories;

public static class C24Sql
{
    public const string HasOpdLegacyResult = "SELECT COUNT(*) FROM OpdRecRpt_PDebtDM_S WHERE chAccDate=:accounting_date";
    public const string HasIpdLegacyResult = "SELECT COUNT(*) FROM IpdRecRpt_PDebtDM_S WHERE chAccDate=:accounting_date";
    public const string ReadOpdLegacyDetails = """
        SELECT chOp1Date AS AccountingDate, chOp1Date2 AS VisitDate, chOp1RoomType AS RoomType,
        chOp1RoomTypeName AS RoomTypeName, chOp1MrNo AS MedicalRecordNo,
        chOp1MrNo2 AS AlternateMedicalRecordNo, chOp1PName AS PatientName, chOp1PTel AS Phone,
        chOp4PSec AS DepartmentCode, chOp4PFin1 AS PayerClassCode, chOp1InSeq AS CardSequenceNo,
        chOp1Dct1 AS ChargeItemCode, chOp1DctName1 AS ChargeItemName, rlOp1Sub6 AS SignedAmount,
        rlOp1Sub3 AS SignedDiscount, rlOp1SubAMT AS AmountDue, chDate AS OtherDate,
        chCUser AS CreatedBy, chCDectUser AS DeletedBy, intFlag AS Flag,
        chDateFlag AS DateFlag, chDC AS DischargeFlag
        FROM OpdRecRpt_PDebtDM WHERE chOp1Date BETWEEN :start_date AND :end_date
        """;
    public const string ReadIpdLegacyDetails = """
        SELECT chOp1Date AS AccountingDate, chOp1Date2 AS VisitDate, chOp1RoomType AS RoomType,
        chOp1RoomTypeName AS RoomTypeName, chOp1MrNo AS MedicalRecordNo,
        chOp1MrNo2 AS AlternateMedicalRecordNo, chOp1PName AS PatientName, chOp1PTel AS Phone,
        chOp4PSec AS DepartmentCode, chOp4PFin1 AS PayerClassCode, chOp1InSeq AS CardSequenceNo,
        chOp1Dct1 AS ChargeItemCode, chOp1DctName1 AS ChargeItemName, rlOp1Sub6 AS SignedAmount,
        rlOp1Sub3 AS SignedDiscount, rlOp1SubAMT AS AmountDue, chDate AS OtherDate,
        chCUser AS CreatedBy, chCDectUser AS DeletedBy, intFlag AS Flag,
        chDateFlag AS DateFlag, CAST(NULL AS VARCHAR2(2)) AS DischargeFlag
        FROM IpdRecRpt_PDebtDM WHERE chOp1Date BETWEEN :start_date AND :end_date
        """;
    public const string ReadOpdLegacySummaries = """
        SELECT chAccDate AS AccountingDate, chOp1RoomType AS RoomType,
        chOp1RoomTypeName AS RoomTypeName, rlOp1SubDebt_S AS DebtAmount,
        rlOp1SubDebt_C AS DebtCount, rlOp1SubPay_S AS PaymentAmount,
        rlOp1SubPay_C AS PaymentCount, rlOp1SubSub_S AS OutstandingAmount,
        rlOp1SubSub_C AS OutstandingCount, chDateFlag AS DateFlag
        FROM OpdRecRpt_PDebtDM_S WHERE chAccDate BETWEEN :start_date AND :end_date
        """;
    public const string ReadIpdLegacySummaries = """
        SELECT chAccDate AS AccountingDate, chOp1RoomType AS RoomType,
        chOp1RoomTypeName AS RoomTypeName, rlOp1SubDebt_S AS DebtAmount,
        rlOp1SubDebt_C AS DebtCount, rlOp1SubPay_S AS PaymentAmount,
        rlOp1SubPay_C AS PaymentCount, rlOp1SubSub_S AS OutstandingAmount,
        rlOp1SubSub_C AS OutstandingCount, chDateFlag AS DateFlag
        FROM IpdRecRpt_PDebtDM_S WHERE chAccDate BETWEEN :start_date AND :end_date
        """;
    public const string LockOpdLegacy = "LOCK TABLE OpdRecRpt_PDebtDM IN SHARE ROW EXCLUSIVE MODE";
    public const string LockIpdLegacy = "LOCK TABLE IpdRecRpt_PDebtDM IN SHARE ROW EXCLUSIVE MODE";
    public const string DeleteOpdLegacyDetails = "DELETE FROM OpdRecRpt_PDebtDM WHERE chOp1Date=:accounting_date";
    public const string DeleteIpdLegacyDetails = "DELETE FROM IpdRecRpt_PDebtDM WHERE chOp1Date=:accounting_date";
    public const string DeleteOpdLegacySummaries = "DELETE FROM OpdRecRpt_PDebtDM_S WHERE chAccDate=:accounting_date";
    public const string DeleteIpdLegacySummaries = "DELETE FROM IpdRecRpt_PDebtDM_S WHERE chAccDate=:accounting_date";
    public const string InsertOpdLegacyDetail = """
        INSERT INTO OpdRecRpt_PDebtDM
        (chOp1Date,chOp1Date2,chOp1RoomType,chOp1RoomTypeName,chOp1MrNo,chOp1MrNo2,
         chOp1PName,chOp1PTel,chOp4PSec,chOp4PFin1,chOp1InSeq,chOp1Dct1,chOp1DctName1,
         rlOp1Sub6,rlOp1Sub3,rlOp1SubAMT,chDate,chCUser,chCDectUser,intFlag,chDateFlag,chDC)
        VALUES
        (:accounting_date,:visit_date,:room_type,:room_name,:mr_no,:mr_no2,:patient_name,:phone,
         :section_code,:fin1,:in_seq,:dct_code,:dct_name,:signed_amount,:signed_discount,
         :amount_due,:other_date,:created_by,:deleted_by,:flag,:date_flag,:discharge_flag)
        """;
    public const string InsertIpdLegacyDetail = """
        INSERT INTO IpdRecRpt_PDebtDM
        (chOp1Date,chOp1Date2,chOp1RoomType,chOp1RoomTypeName,chOp1MrNo,chOp1MrNo2,
         chOp1PName,chOp1PTel,chOp4PSec,chOp4PFin1,chOp1InSeq,chOp1Dct1,chOp1DctName1,
         rlOp1Sub6,rlOp1Sub3,rlOp1SubAMT,chDate,chCUser,chCDectUser,intFlag,chDateFlag)
        VALUES
        (:accounting_date,:visit_date,:room_type,:room_name,:mr_no,:mr_no2,:patient_name,:phone,
         :section_code,:fin1,:in_seq,:dct_code,:dct_name,:signed_amount,:signed_discount,
         :amount_due,:other_date,:created_by,:deleted_by,:flag,:date_flag)
        """;
    public const string InsertOpdLegacySummary = """
        INSERT INTO OpdRecRpt_PDebtDM_S
        (chAccDate,chOp1RoomType,chOp1RoomTypeName,rlOp1SubDebt_S,rlOp1SubDebt_C,
         rlOp1SubPay_S,rlOp1SubPay_C,rlOp1SubSub_S,rlOp1SubSub_C,chDateFlag)
        VALUES (:accounting_date,:room_type,:room_name,:debt_amount,:debt_count,:payment_amount,
         :payment_count,:outstanding_amount,:outstanding_count,:date_flag)
        """;
    public const string InsertIpdLegacySummary = """
        INSERT INTO IpdRecRpt_PDebtDM_S
        (chAccDate,chOp1RoomType,chOp1RoomTypeName,rlOp1SubDebt_S,rlOp1SubDebt_C,
         rlOp1SubPay_S,rlOp1SubPay_C,rlOp1SubSub_S,rlOp1SubSub_C,chDateFlag)
        VALUES (:accounting_date,:room_type,:room_name,:debt_amount,:debt_count,:payment_amount,
         :payment_count,:outstanding_amount,:outstanding_count,:date_flag)
        """;
    public const string A01 = """
        SELECT /* C24-A01 */ chOp1Date AS VisitDate, chOp1Time AS VisitTime, chOp1Room AS VisitRoom,
        intOp1No AS VisitNo, chOp3PFin1 AS PayerClassCode, SUBSTR(chOp3IDate,1,7) AS IDate,
        SUBSTR(chOp3DCDate,1,7) AS DCDate, chOp3Dct AS ChargeItemCode, chOp3CUser AS CreatedBy,
        SUM(rlOp3Sub6) AS QuerySub6, SUM(rlOp3Sub3) AS QuerySub3, 'DRG' AS SourceKind
        FROM OpdDrgTbl WHERE ((chOp1Date < :accounting_date AND (chOp3IDate BETWEEN :day_begin AND :day_end
        OR (RTRIM(chOp3IDate) IS NOT NULL AND chOp3DCDate BETWEEN :day_begin AND :day_end)))
        OR (chOp1Date = :accounting_date AND chOp3IDate BETWEEN '0' AND :day_end AND
        (RTRIM(chOp3DCDate) IS NULL OR chOp3DCDate NOT BETWEEN '0' AND :day_end)))
        AND rlOp3Sub6 <> 0 AND (chOp3Proj NOT IN ('I','S') OR RTRIM(chOp3Proj) IS NULL)
        AND LNNVL(chOp3Stat = '09') GROUP BY chOp1Date,chOp1Time,chOp1Room,intOp1No,chOp3PFin1,
        SUBSTR(chOp3IDate,1,7),SUBSTR(chOp3DCDate,1,7),chOp3Dct,chOp3CUser
        """;
    public const string A02 = """
        SELECT /* C24-A02 */ chOp1Date AS VisitDate, chOp1Time AS VisitTime, chOp1Room AS VisitRoom,
        intOp1No AS VisitNo, chOp4PFin1 AS PayerClassCode, SUBSTR(chOp4IDate,1,7) AS IDate,
        SUBSTR(chOp4DCDate,1,7) AS DCDate, chOp4Dct AS ChargeItemCode, chOp4CUser AS CreatedBy,
        SUM(rlOp4Sub6) AS QuerySub6, SUM(rlOp4Sub3) AS QuerySub3, 'ORD' AS SourceKind
        FROM OpdOrdTbl WHERE ((chOp1Date < :accounting_date AND (chOp4IDate BETWEEN :day_begin AND :day_end
        OR (RTRIM(chOp4IDate) IS NOT NULL AND chOp4DCDate BETWEEN :day_begin AND :day_end)))
        OR (chOp1Date = :accounting_date AND chOp4IDate BETWEEN '0' AND :day_end AND
        (RTRIM(chOp4DCDate) IS NULL OR chOp4DCDate NOT BETWEEN '0' AND :day_end)))
        AND rlOp4Sub6 <> 0 AND (chOp4Proj NOT IN ('I','S') OR RTRIM(chOp4Proj) IS NULL)
        GROUP BY chOp1Date,chOp1Time,chOp1Room,intOp1No,chOp4PFin1,SUBSTR(chOp4IDate,1,7),
        SUBSTR(chOp4DCDate,1,7),chOp4Dct,chOp4CUser
        """;
    public const string A03 = """
        SELECT /* C24-A03 */ chOp1Date AS VisitDate, chOp1Time AS VisitTime, chOp1Room AS VisitRoom,
        intOp1No AS VisitNo, chOp4PFin1 AS PayerClassCode, SUBSTR(chOp4IDate,1,7) AS IDate,
        SUBSTR(chOp4DCDate,1,7) AS DCDate, chOp4Dct AS ChargeItemCode, chOp4CUser AS CreatedBy,
        rlOp4Sub1 * -1 AS QuerySub6, 0 AS QuerySub3, rlOp4Sub1 AS SourceSub1, 'ACC69' AS SourceKind
        FROM OpdOrdTbl WHERE chOp4OrdNo = 'ACC-69' AND rlOp4Sub1 <> 0
        AND (chOp4IDate BETWEEN :day_begin AND :day_end OR chOp4DCDate BETWEEN :day_begin AND :day_end)
        AND (chOp4Proj NOT IN ('I','S','D') OR RTRIM(chOp4Proj) IS NULL)
        """;
    public const string A04 = """
        SELECT /* C24-A04 */ chOp1Date AS VisitDate, chOp1Time AS VisitTime, chOp1Room AS VisitRoom,
        intOp1No AS VisitNo, chOp3PFin1 AS PayerClassCode, SUBSTR(chOp3IDate,1,7) AS IDate,
        SUBSTR(chOp3DCDate,1,7) AS DCDate, chOp3Dct AS ChargeItemCode, chOp3CUser AS CreatedBy,
        SUM(rlOp3Sub6) AS QuerySub6, SUM(rlOp3Sub3) AS QuerySub3, 'DRG' AS SourceKind
        FROM IpdDrgTbl WHERE ((chOp1Date < :accounting_date AND (chOp3IDate BETWEEN :day_begin AND :day_end
        OR (RTRIM(chOp3IDate) IS NOT NULL AND chOp3DCDate BETWEEN :day_begin AND :day_end)))
        OR (chOp1Date = :accounting_date AND chOp3IDate BETWEEN '0' AND :day_end AND
        (RTRIM(chOp3DCDate) IS NULL OR chOp3DCDate NOT BETWEEN '0' AND :day_end)))
        AND rlOp3Sub6 <> 0 AND (chOp3Proj NOT IN ('I','S') OR RTRIM(chOp3Proj) IS NULL)
        AND (chOp3Stat NOT IN ('09','10','11','12') OR RTRIM(chOp3Stat) IS NULL)
        GROUP BY chOp1Date,chOp1Time,chOp1Room,intOp1No,chOp3PFin1,SUBSTR(chOp3IDate,1,7),
        SUBSTR(chOp3DCDate,1,7),chOp3Dct,chOp3CUser
        """;
    public const string A05 = """
        SELECT /* C24-A05 */ chOp1Date AS VisitDate, chOp1Time AS VisitTime, chOp1Room AS VisitRoom,
        intOp1No AS VisitNo, chOp4PFin1 AS PayerClassCode, SUBSTR(chOp4IDate,1,7) AS IDate,
        SUBSTR(chOp4DCDate,1,7) AS DCDate, chOp4Dct AS ChargeItemCode, chOp4CUser AS CreatedBy,
        SUM(rlOp4Sub6) AS QuerySub6, SUM(rlOp4Sub3) AS QuerySub3, 'ORD' AS SourceKind
        FROM IpdOrdTbl WHERE ((chOp1Date < :accounting_date AND (chOp4IDate BETWEEN :day_begin AND :day_end
        OR (RTRIM(chOp4IDate) IS NOT NULL AND chOp4DCDate BETWEEN :day_begin AND :day_end)))
        OR (chOp1Date = :accounting_date AND chOp4IDate BETWEEN '0' AND :day_end AND
        (RTRIM(chOp4DCDate) IS NULL OR chOp4DCDate NOT BETWEEN '0' AND :day_end)))
        AND rlOp4Sub6 <> 0 AND (chOp4Proj NOT IN ('I','S') OR RTRIM(chOp4Proj) IS NULL)
        GROUP BY chOp1Date,chOp1Time,chOp1Room,intOp1No,chOp4PFin1,SUBSTR(chOp4IDate,1,7),
        SUBSTR(chOp4DCDate,1,7),chOp4Dct,chOp4CUser
        """;
    public const string A06 = """
        SELECT /* C24-A06 */ chOp1Date AS VisitDate, chOp1Time AS VisitTime, chOp1Room AS VisitRoom,
        intOp1No AS VisitNo, chOp4PFin1 AS PayerClassCode, SUBSTR(chOp4IDate,1,7) AS IDate,
        SUBSTR(chOp4DCDate,1,7) AS DCDate, chOp4Dct AS ChargeItemCode, chOp4CUser AS CreatedBy,
        rlOp4Sub1 * -1 AS QuerySub6, 0 AS QuerySub3, rlOp4Sub1 AS SourceSub1, 'ACC69' AS SourceKind
        FROM IpdOrdTbl WHERE chOp4OrdNo = 'ACC-69' AND rlOp4Sub1 <> 0
        AND (chOp4IDate BETWEEN :day_begin AND :day_end OR chOp4DCDate BETWEEN :day_begin AND :day_end)
        AND (chOp4Proj NOT IN ('I','S','D') OR RTRIM(chOp4Proj) IS NULL)
        """;

    public const string E01 = """
        SELECT /* C24-E01 */ b.chOp1MrNo AS MedicalRecordNo, b.chOp1RoomType AS RoomType,
        b.chOp1PName AS PatientName, b.chOp1Sec AS DepartmentCode, b.chOp1PFin1 AS PatientFin1,
        b.chOp1InSeq AS CardSequenceNo, m.chTelH AS Phone FROM IpdBasicTbl b
        JOIN OpdMrBasicTbl m ON RTRIM(b.chOp1MrNo) = m.chMrNo WHERE b.chOp1Date=:visit_date
        AND b.chOp1Time=:visit_time AND b.chOp1Room=:visit_room AND b.intOp1No=:visit_no
        AND b.chOp1MrNo NOT IN ('C36979','1000000')
        """;
    public const string E02 = """
        SELECT /* C24-E02 */ chOp0PMrNo AS MedicalRecordNo, chOp0RoomType AS RoomType,
        chOp0PName AS PatientName, chOp0SecNo AS DepartmentCode, chOp0Fin1 AS PatientFin1,
        chOp0Seq AS CardSequenceNo, chOp0Tel1 AS Phone FROM OpdRegPtnTbl WHERE chOp0Date=:visit_date
        AND chOp0Time=:visit_time AND chOp0Room=:visit_room AND intOp0No=:visit_no
        AND chOp0PMrNo NOT IN ('C36979','1000000')
        """;
    public const string E03 = """
        SELECT /* C24-E03 */ chOp1MrNo AS MedicalRecordNo, chOp1RoomType AS RoomType,
        chOp1PName AS PatientName, chOp1Sec AS DepartmentCode, chOp1PFin1 AS PatientFin1,
        chOp1InSeq AS CardSequenceNo, CAST(NULL AS VARCHAR2(32)) AS Phone FROM OpdBasicTbl
        WHERE chOp1Date=:visit_date AND chOp1Time=:visit_time AND chOp1Room=:visit_room
        AND intOp1No=:visit_no AND chOp1Room IN ('EEEE', 'HHHH')
        AND chOp1MrNo NOT IN ('C36979','1000000')
        """;
    public const string E04 = """
        SELECT /* C24-E04 */ chDctItem AS ChargeItemCode, chDctItemName AS ChargeItemName
        FROM GenDctItemTbl WHERE chDctItem = :dct_code
        """;
    public const string B01 = """
        SELECT /* C24-B01 */ b.chOp1MrNo AS MedicalRecordNo, b.chOp1RoomType AS RoomType,
        b.chOp1PName AS PatientName,b.chOp1Sec AS DepartmentCode,b.chOp1PFin1 AS PayerClassCode,
        b.chOp1InSeq AS CardSequenceNo,c.chTelH AS Phone,a.chBillDate AS BillDate,
        a.chBillMan AS CreatedBy,SUM(a.rlDebtAMT) AS Amount FROM GenDebtTbl a JOIN OpdBasicTbl b
        ON a.chOp1Date=b.chOp1Date AND a.chOp1Time=b.chOp1Time AND a.chOp1Room=b.chOp1Room
        AND a.intOp1No=b.intOp1No JOIN OpdMrBasicTbl c ON RTRIM(b.chOp1MrNo)=c.chMrNo
        WHERE a.chBillDate BETWEEN :start_date AND :end_date AND RTRIM(a.chBackFlg) IS NULL
        AND a.rlDebtAMT <> 0 AND b.chOp1MrNo NOT IN ('C36979','1000000')
        AND (:room_scope=0 OR (:room_scope=1 AND b.chOp1RoomType='E') OR (:room_scope=2 AND b.chOp1RoomType<>'E'))
        AND (:mr_no IS NULL OR b.chOp1MrNo=:mr_no) GROUP BY b.chOp1MrNo,b.chOp1RoomType,
        b.chOp1PName,b.chOp1Sec,b.chOp1PFin1,b.chOp1InSeq,c.chTelH,a.chBillDate,a.chBillMan
        """;
    public const string B02 = """
        SELECT /* C24-B02 */ b.chOp1MrNo AS MedicalRecordNo, b.chOp1RoomType AS RoomType,
        b.chOp1PName AS PatientName,b.chOp1Sec AS DepartmentCode,b.chOp1PFin1 AS PayerClassCode,
        b.chOp1InSeq AS CardSequenceNo,c.chTelH AS Phone,a.vchBillDate AS BillDate,
        a.vchBillMan AS CreatedBy,SUM(a.rlDebtAMT) AS Amount FROM IpdDebtTbl a JOIN IpdBasicTbl b
        ON a.chOp1Date=b.chOp1Date AND a.chOp1Time=b.chOp1Time AND a.chOp1Room=b.chOp1Room
        AND a.intOp1No=b.intOp1No JOIN OpdMrBasicTbl c ON RTRIM(b.chOp1MrNo)=c.chMrNo
        WHERE a.vchBillDate BETWEEN :start_date AND :end_date AND RTRIM(a.vchBackFlg) IS NULL
        AND a.rlDebtAMT <> 0 AND b.chOp1MrNo NOT IN ('C36979','1000000')
        AND (:mr_no IS NULL OR b.chOp1MrNo=:mr_no) GROUP BY b.chOp1MrNo,b.chOp1RoomType,
        b.chOp1PName,b.chOp1Sec,b.chOp1PFin1,b.chOp1InSeq,c.chTelH,a.vchBillDate,a.vchBillMan
        """;

    public static IReadOnlyList<(string Code, string Sql)> Accounting(string source) => source switch
    {
        ViewModels.C24Sources.OpdEr => [("A01", A01), ("A02", A02), ("A03", A03)],
        ViewModels.C24Sources.Inpatient => [("A04", A04), ("A05", A05), ("A06", A06)],
        _ => throw new ArgumentException("C24 來源不正確。", nameof(source))
    };
}
