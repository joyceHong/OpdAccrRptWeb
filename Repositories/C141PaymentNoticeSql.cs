namespace OpdAccrRptWeb.Repositories;

internal static class C141PaymentNoticeSql
{
    internal const string Basic = """
        SELECT B.chOp1MrNo AS MedicalRecordNumber,
               B.chOp1PName AS PatientName,
               B.chOp1Sec AS SectionCode,
               B.chOp1Ebid AS BedNumber,
               B.chOp1EInDate AS InpatientAdmissionDate,
               B.chOp1PPay AS PaymentType,
               B.chOp1PFin1 AS InsuranceIdentity,
               B.chOp1PFin2 AS InsuranceCategory,
               B.chOp1DrName AS DoctorName,
               B.chOp1InSeq AS InpatientSequence,
               B.chOp1ChrNo AS HealthCardNumber,
               S.chSecName AS SectionName
        FROM IPDBasicTbl B
        JOIN GenSectionTbl S ON B.chOp1Sec=S.chSecNo
        WHERE B.chOp1Date=:opDate AND B.chOp1Time=:opTime
          AND B.chOp1Room=:opRoom AND B.intOp1No=:opNo
        """;

    internal const string TemporaryAmount2 = """
        SELECT SUM(A.rlop4AMT2) AS TemporaryAmount2
        FROM IpdordTbl A
        WHERE A.chOp1Date=:opDate AND A.chOp1Time=:opTime
          AND A.chOp1Room=:opRoom AND A.intOp1No=:opNo
          AND A.chop4Dct IN ('06','35')
          AND SUBSTR(A.chop4Dct,1,2) < '51'
          AND A.chop4Stat NOT IN ('DC','PR')
        """;

    internal const string Categories = """
        SELECT SUM(A.rlOp3AMT1) AS Amount1, SUM(A.rlOp3AMT2) AS Amount2,
               SUM(A.rlOp3Sub1) AS Subsidy1, SUM(A.rlOp3Sub2) AS Subsidy2,
               SUM(A.rlOp3Sub3) AS Subsidy3, SUM(A.rlOp3Sub4) AS Subsidy4,
               SUM(A.rlOp3Sub5) AS Subsidy5, SUM(A.rlOp3Sub6) AS Subsidy6,
               A.chOp3DctName AS DepartmentName, A.chOp3Dct AS DepartmentCode
        FROM IpdDrgTbl A
        WHERE A.chOp1Date=:opDate AND A.chOp1Time=:opTime
          AND A.chOp1Room=:opRoom AND A.intOp1No=:opNo
          AND NVL(A.chOp3IDate,'0') > '0'
          AND A.chOp3SPay IN ('0','1','4')
          AND (A.chOp3Dcnt NOT IN ('2','3') OR A.chOp3Dcnt IS NULL)
          AND (A.chOp3Stat NOT IN ('DC','09','10','11','12') OR A.chOp3Stat IS NULL)
          AND (A.chOp3Proj NOT IN ('I','D') OR A.chOp3Proj IS NULL)
          AND (A.chOp3Rep3Flg <> 'S' OR A.chOp3Rep3Flg IS NULL)
          AND (A.rlOp3AMT1<>0 OR A.rlOp3AMT2<>0 OR A.rlOp3AMT1 IS NULL OR A.rlOp3AMT2 IS NULL)
          AND (A.chOp3DrgType<>'1' OR A.chOp3DrgType IS NULL)
          AND SUBSTR(A.chOp3Dct,1,2) < '51'
        GROUP BY A.chOp3DctName,A.chOp3Dct
        UNION ALL
        SELECT SUM(A.rlOp4AMT1), SUM(A.rlOp4AMT2),
               SUM(A.rlOp4Sub1), SUM(A.rlOp4Sub2),
               SUM(A.rlOp4Sub3), SUM(A.rlOp4Sub4),
               SUM(A.rlOp4Sub5), SUM(A.rlOp4Sub6),
               A.chOp4DctName, A.chOp4Dct
        FROM IpdOrdTbl A
        WHERE A.chOp1Date=:opDate AND A.chOp1Time=:opTime
          AND A.chOp1Room=:opRoom AND A.intOp1No=:opNo
          AND NVL(A.chOp4IDate,'0') > '0'
          AND A.chOp4SPay IN ('0','1','4')
          AND A.chOp4Stat >= '30'
          AND (A.chOp4Stat NOT IN ('DC','PR') OR A.chOp4Stat IS NULL)
          AND (A.chOp4Proj NOT IN ('I','D') OR A.chOp4Proj IS NULL)
          AND (A.rlOp4AMT1<>0 OR A.rlOp4AMT2<>0 OR A.rlOp4AMT1 IS NULL OR A.rlOp4AMT2 IS NULL)
          AND SUBSTR(A.chOp4Dct,1,2) < '51'
          AND SUBSTR(A.chOp4Dct,1,2) <> '49'
        GROUP BY A.chOp4DctName,A.chOp4Dct
        ORDER BY DepartmentCode
        """;

    internal const string PayTotal = """
        SELECT SUM(T.intOp2Acc1 + T.intOp2Acc2 + T.intOp2Acc3 + T.intOp2Acc4 + T.intItemI3) AS PayTotal
        FROM IPDRECTBL T
        WHERE T.chOp1Date=:opDate AND T.chOp1Time=:opTime
          AND T.chOp1Room=:opRoom AND T.intOp1No=:opNo
          AND T.vchOp2Stat IN ('C','P')
          AND T.VChOp2Debt<>'T'
        """;

    internal const string DiscountAmount = """
        SELECT SUM(rlOp4Sub3) AS DiscountAmount
        FROM IpdOrdTbl
        WHERE chOp1Date=:opDate AND chOp1Time=:opTime
          AND chOp1Room=:opRoom AND intOp1No=:opNo
          AND chOp4Stat<>'DC'
          AND chOp4OrdNo LIKE '643DIS%'
        """;

    internal const string OracleNow = "SELECT SYSDATE FROM DUAL";

    internal const string UpdateBeforeDocument = """
        UPDATE IPDBasic2Tbl
        SET CHLISTDATE=:serverDate7,vchAPayDate=:serverDate7
        WHERE chOp1Date=:opDate AND chOp1Time=:opTime
          AND chOp1Room=:opRoom AND intOp1No=:opNo
        """;

    internal const string InsertNoticeTransaction = """
        INSERT INTO IpdTranTbl (
            chOpdUniNo,chMRNo,IpdTDate,IpdTType,IpdSeqNo,IpdTNo,
            IpdTNote,IpdTBf,IpdTBedBGrade,IpdTBedBType,IpdTBedBFlag,
            IpdTAf,IpdTBedAGrade,IpdTBedAType,IpdTBedAFlag,IpdTFlg,
            IpdTUser,chSourceType,chSystemType,chCaseNo,chNeedDate,
            chExpDate,chBorrowID,chDeptID,chMdrID,chAdrID,chStatus,
            chCreateDate,chCreateTime,chProcessFG,chResvcol,chFin,chSeqNo
        ) VALUES (
            :opdUniNo,:mrNo,:serverDate13,'18','','',
            '列印繳款通知單',:amountDue,'','','',
            '','','','','1',
            :userId,'','1','',:opDate,
            '','','','','','',
            :serverDate7,:serverTime6,'','','',''
        )
        """;

    internal const string UpdateAfterDocument = """
        UPDATE IPDBasic2Tbl
        SET vchAPayDate=:entryDate7
        WHERE chOp1Date=:opDate AND chOp1Time=:opTime
          AND chOp1Room=:opRoom AND intOp1No=:opNo
        """;
}
