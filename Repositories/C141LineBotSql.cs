namespace OpdAccrRptWeb.Repositories;

internal static class C141LineBotSql
{
    internal const string Case = """
        SELECT B.chOp1MrNo AS MedicalRecordNumber,
               B.chOp1PName AS PatientName,
               B.chOp1Ebid AS BedNumber,
               B.chOp1PFin1 AS InsuranceIdentity,
               B.chOp1PPay AS PaymentType,
               B.chOp1PFin2 AS InsuranceCategory,
               B.chOp1InSeq AS InpatientSequence,
               B.chOp1ChrNo AS HealthCardNumber,
               B2.rlAMT AS ReceivableAmount,
               Pay.PayTot AS PayTotal,
               Contact.VchMrNo AS ContactMedicalRecordNumber,
               Contact.VchPid AS ContactPatientId
        FROM IPDBasicTbl B
        JOIN IPDBasic2Tbl B2
          ON B.chOp1Date=B2.chOp1Date AND B.chOp1Time=B2.chOp1Time
         AND B.chOp1Room=B2.chOp1Room AND B.intOp1No=B2.intOp1No
        LEFT JOIN (
            SELECT T.chOp1Date,T.chOp1Time,T.chOp1Room,T.intOp1No,
                   SUM(T.intOp2Acc1 + T.intOp2Acc2 + T.intOp2Acc3 + T.intOp2Acc4 + T.intItemI3) AS PayTot
            FROM IPDRECTBL T
            WHERE T.vchOp2Stat IN ('C','P') AND T.VChOp2Debt<>'T'
            GROUP BY T.chOp1Date,T.chOp1Time,T.chOp1Room,T.intOp1No
        ) Pay
          ON B.chOp1Date=Pay.chOp1Date AND B.chOp1Time=Pay.chOp1Time
         AND B.chOp1Room=Pay.chOp1Room AND B.intOp1No=Pay.intOp1No
        LEFT JOIN (
            SELECT TRIM(VchMrNo) AS NormalizedMrNo,VchMrNo,VchPid,
                   ROW_NUMBER() OVER (
                       PARTITION BY TRIM(VchMrNo)
                       ORDER BY ChOp1Date DESC NULLS LAST,VchPid ASC
                   ) AS RowNumber
            FROM IPDCONTACTTELTBL
            WHERE VchPriority='Y'
        ) Contact
          ON TRIM(B.chOp1MrNo)=Contact.NormalizedMrNo AND Contact.RowNumber=1
        WHERE B.chOp1Date=:opDate AND B.chOp1Time=:opTime
          AND B.chOp1Room=:opRoom AND B.intOp1No=:opNo
          AND B.chOp1ClmFlg='1'
        """;

    internal const string UpdateNoticeDates = """
        UPDATE IPDBasic2Tbl
        SET CHLISTDATE=:serverDate7,vchAPayDate=:serverDate7
        WHERE chOp1Date=:opDate AND chOp1Time=:opTime
          AND chOp1Room=:opRoom AND intOp1No=:opNo
        """;

    internal const string InsertNoticeLog = """
        INSERT INTO IpdTranTbl (
            chOpdUniNo,chMRNo,IpdTDate,IpdTType,IpdSeqNo,IpdTNo,
            IpdTNote,IpdTBf,IpdTBedBGrade,IpdTBedBType,IpdTBedBFlag,
            IpdTAf,IpdTBedAGrade,IpdTBedAType,IpdTBedAFlag,IpdTFlg,
            IpdTUser,chSourceType,chSystemType,chCaseNo,chNeedDate,
            chExpDate,chBorrowID,chDeptID,chMdrID,chAdrID,chStatus,
            chCreateDate,chCreateTime,chProcessFG,chResvcol,chFin,chSeqNo
        ) VALUES (
            :opdUniNo,:mrNo,:serverDate13,'18','','',
            :printType,:amountDue,'','','',
            '','','','','1',
            :userId,'','1','',:opDate,
            '','','','','','',
            :serverDate7,:serverTime6,'','','',''
        )
        """;

    internal const string DeleteZeroSequencePrepayment = """
        DELETE FROM OPDPSSFEEPREPAYTbl
        WHERE CHOP1DATE=:opDate AND CHOP1TIME=:opTime
          AND CHOP1ROOM=:opRoom AND INTOP1NO=:opNo
          AND INTOP1OrdSeq=0
        """;

    internal const string InsertPrepayment = """
        INSERT INTO OPDPSSFEEPREPAYTbl (
            CHOP1DATE,CHOP1TIME,CHOP1ROOM,INTOP1NO,VCHOPDUNION,
            INTACCDET,VCHSTATUS,VCHSTATUS0,VCHUSERID,VCHDATETIME,
            INTACCDET0,VCHPPAY,VCHPFIN1,VCHPFIN2,VCHINSEQ,INTOP1ORDSEQ
        ) VALUES (
            :opDate,:opTime,:opRoom,:opNo,:opdUnion,
            :amount,'10','10',:userId,:serverDate13,
            :amount,:paymentType,:insuranceIdentity,:insuranceCategory,:inpatientSequence,0
        )
        """;
}
