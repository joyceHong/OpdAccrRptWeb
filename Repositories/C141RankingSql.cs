namespace OpdAccrRptWeb.Repositories;

internal static class C141RankingSql
{
    internal const string Query = """
        SELECT DISTINCT b1.chOp1Date AS AdmissionDate,
               b1.chOp1Time AS AdmissionTime,
               b1.chOp1Room AS AdmissionRoom,
               b1.intOp1No AS AdmissionNumber,
               b1.chOp1MrNo AS MedicalRecordNumber,
               b1.chOp1PName AS PatientName,
               b1.chOp1Ebid AS BedNumber,
               b1.chOp1Eindate AS InpatientAdmissionDate,
               b1.chOp1PFin1 AS InsuranceIdentity,
               b1.chOp1PFin2 AS InsuranceCategory,
               b1.chOp1PPay AS PaymentType,
               b1.chOp11st AS AdmissionSequence,
               b1.chOp1InSeq AS InpatientSequence,
               b2.chCDate AS AccountingDate,
               b2.rlAMT AS ReceivableAmount,
               b2.rlAMT2 AS InsuredReceivableAmount,
               b2.vchAPayDate AS PaymentDate,
               b2.chListDate AS NoticeDate,
               pay.PayTot AS PayTotal,
               (NVL(b2.rlAMT,0) - NVL(pay.PayTot,0)) AS NetAmount,
               CASE WHEN LENGTH(TRIM(contact.vchpid))=10 THEN 1 ELSE 0 END AS HasPreferredContact
        FROM IPDBasicTbl b1
        JOIN IPDBasic2Tbl b2
          ON b1.chOp1date=b2.chOp1date AND b1.chOp1time=b2.chOp1time
         AND b1.chOp1room=b2.chOp1room AND b1.intOp1no=b2.intOp1no
        LEFT JOIN (
            SELECT T.chOp1Date, T.chOp1Time, T.chOp1Room, T.intOp1No,
                   SUM(T.intOp2Acc1 + T.intOp2Acc2 + T.intOp2Acc3 + T.intOp2Acc4 + T.intitemI3) AS PayTot
            FROM IPDRECTBL T
            JOIN IPDBasicTbl B
              ON T.chOp1date=B.chOp1date AND T.chOp1time=B.chOp1time
             AND T.chOp1room=B.chOp1room AND T.intOp1no=B.intOp1no
            WHERE B.chOp1clmFlg='1'
              AND (:mrNo IS NULL OR B.chOp1MrNo=:mrNo)
              AND (:bedNo IS NULL OR (:bedNoLength=6 AND B.chOp1EBid=:bedNo)
                   OR (:bedNoLength<>6 AND B.chOp1EBid LIKE :bedNoPrefix))
              AND T.vchOp2Stat IN ('C','P')
              AND T.VChOp2Debt<>'T'
            GROUP BY T.chOp1Date,T.chOp1Time,T.chOp1Room,T.intOp1No
        ) pay
          ON b1.chOp1date=pay.chOp1date AND b1.chOp1time=pay.chOp1time
         AND b1.chOp1room=pay.chOp1room AND b1.intOp1no=pay.intOp1no
        LEFT JOIN (
            SELECT VchMRno,
                   ROW_NUMBER() OVER (PARTITION BY VchMRno ORDER BY CHOP1DATE DESC) AS sno,
                   vchpid
            FROM IPDCONTACTTELTBL
            WHERE vchpriority='Y'
        ) contact
          ON TRIM(b1.chOp1MRNO)=contact.VCHMRNO AND contact.sno=1
        WHERE b1.chOp1ClmFlg='1'
          AND (:mrNo IS NULL OR b1.chOp1MrNo=:mrNo)
          AND (:bedNo IS NULL OR (:bedNoLength=6 AND b1.chOp1EBid=:bedNo)
               OR (:bedNoLength<>6 AND b1.chOp1EBid LIKE :bedNoPrefix))
        """;

    internal static string Count => $"""
        SELECT COUNT(*)
        FROM ({Query}) ranked
        WHERE (:minimumNetAmount IS NULL OR ranked.NetAmount > :minimumNetAmount)
        """;

    internal static string Page => $"""
        SELECT *
        FROM ({Query}) ranked
        WHERE (:minimumNetAmount IS NULL OR ranked.NetAmount > :minimumNetAmount)
        ORDER BY ranked.NetAmount DESC,
                 ranked.AdmissionDate ASC,
                 ranked.AdmissionTime ASC,
                 ranked.AdmissionRoom ASC,
                 ranked.AdmissionNumber ASC
        OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY
        """;
}
