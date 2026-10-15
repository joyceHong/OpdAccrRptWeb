namespace OpdAccrRptWeb.Repositories;

internal static class C141RecalculationSql
{
    internal const string ClaimFlaggedCases = """
        SELECT chOp1Date AS AdmissionDate,
               chOp1Time AS AdmissionTime,
               chOp1Room AS AdmissionRoom,
               intOp1No AS AdmissionNumber,
               chOp1PFin1 AS InsuranceIdentity,
               chOp1PPay AS PartialPaymentCode,
               chOp1Ebid AS BedNumber
        FROM IPDBasicTbl
        WHERE chOp1ClmFlg='1'
        """;

    internal const string CurrentDayTotals = """
        SELECT SUM(Tot1) AS AllTot1, SUM(Tot2) AS AllTot2, SUM(Tot3) AS AllTot3
        FROM (
            SELECT SUM(rlOp4sub1) AS Tot1, SUM(rlOp4sub2) AS Tot2,
                   SUM(rlop4amt1) AS Tot3
            FROM IPDOrdTbl
            WHERE chOp1Date = :opDate AND chOp1Time = :opTime
              AND chOp1Room = :opRoom AND intOp1No = :opNo
              AND chOp4Stat <> 'DC'
              AND chOp4SPay IN ('0','1','4')
              AND chOp4Stat >= '30'
              AND (chOp4Stat NOT IN ('DC','PR') OR chOp4Stat IS NULL)
              AND (chOp4Proj NOT IN ('I','D') OR chOp4Proj IS NULL)
              AND (rlOp4AMT1 <> 0 OR rlOp4AMT2 <> 0 OR rlOp4AMT1 IS NULL OR rlOp4AMT2 IS NULL)
              AND SUBSTR(chOp4Dct,1,2) < '51'
              AND SUBSTR(chOp4Dct,1,2) <> '49'
              AND NVL(chOp4Idate,' ') > ' '
            UNION ALL
            SELECT SUM(rlOp3sub1) AS Tot1, SUM(rlOp3sub2) AS Tot2,
                   SUM(rlop3amt1) AS Tot3
            FROM IPDDrgTbl
            WHERE chOp1Date = :opDate AND chOp1Time = :opTime
              AND chOp1Room = :opRoom AND intOp1No = :opNo
              AND chOp3Stat <> 'DC'
              AND (chOp3Dcnt NOT IN ('2','3') OR chOp3Dcnt IS NULL)
              AND (chOp3Stat NOT IN ('DC','09','10','11','12') OR chOp3Stat IS NULL)
              AND (chOp3Proj NOT IN ('I','D') OR chOp3Proj IS NULL)
              AND (chOp3Rep3Flg <> 'S' OR chOp3Rep3Flg IS NULL)
              AND (rlOp3AMT1 <> 0 OR rlOp3AMT2 <> 0 OR rlOp3AMT1 IS NULL OR rlOp3AMT2 IS NULL)
              AND (chOp3DrgType <> '1' OR chOp3DrgType IS NULL)
              AND SUBSTR(chOp3Dct,1,2) < '51'
              AND NVL(chOp3Idate,' ') > ' '
        )
        """;

    internal const string HistoricalDayTotals = """
        SELECT SUM(Tot1) AS AllTot1, SUM(Tot2) AS AllTot2, SUM(Tot3) AS AllTot3
        FROM (
            SELECT SUM(rlOp4sub1) AS Tot1, SUM(rlOp4sub2) AS Tot2,
                   SUM(rlop4amt1) AS Tot3
            FROM IPDOrdTbl
            WHERE chOp1Date = :opDate AND chOp1Time = :opTime
              AND chOp1Room = :opRoom AND intOp1No = :opNo
              AND chOp4SPay IN ('0','1','4')
              AND chOp4Stat >= '30'
              AND ((chOp4Stat NOT IN ('DC','PR') OR chOp4Stat IS NULL)
                   OR (chOp4Stat = 'DC' AND chOp4dcdate > :accountDate2359))
              AND (chOp4Proj NOT IN ('I','D') OR chOp4Proj IS NULL)
              AND (rlOp4AMT1 <> 0 OR rlOp4AMT2 <> 0 OR rlOp4AMT1 IS NULL OR rlOp4AMT2 IS NULL)
              AND SUBSTR(chOp4Dct,1,2) < '51'
              AND chOp4Idate <= :accountDate2359
            UNION ALL
            SELECT SUM(rlOp3sub1) AS Tot1, SUM(rlOp3sub2) AS Tot2,
                   SUM(rlop3amt1) AS Tot3
            FROM IPDDrgTbl
            WHERE chOp1Date = :opDate AND chOp1Time = :opTime
              AND chOp1Room = :opRoom AND intOp1No = :opNo
              AND (chOp3Dcnt NOT IN ('2','3') OR chOp3Dcnt IS NULL)
              AND ((chOp3Stat NOT IN ('DC','09','10','11','12') OR chOp3Stat IS NULL)
                   OR (chOp3Stat = 'DC' AND chOp3dcdate > :accountDate2359))
              AND (chOp3Proj NOT IN ('I','D') OR chOp3Proj IS NULL)
              AND (chOp3Rep3Flg <> 'S' OR chOp3Rep3Flg IS NULL)
              AND (rlOp3AMT1 <> 0 OR rlOp3AMT2 <> 0 OR rlOp3AMT1 IS NULL OR rlOp3AMT2 IS NULL)
              AND (chOp3DrgType <> '1' OR chOp3DrgType IS NULL)
              AND SUBSTR(chOp3Dct,1,2) < '51'
              AND chOp3Idate <= :accountDate2359
        )
        """;
}
