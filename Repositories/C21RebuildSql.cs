namespace OpdAccrRptWeb.Repositories;

internal static class C21RebuildSql
{
    // Commands intentionally remain separate so the coordinator can preserve the
    // VB6 Helper order while one outer transaction owns every mutation.
    internal static IReadOnlyList<string> Commands =>
    [
        DeleteRoom23Sql,
        InsertRoom23Sql,
        DeleteRoom45Sql,
        InsertRoom45Sql,
        DeleteMrNoSql,
        InsertMrNoDSql,
        InsertMrNoCSql
    ];

    internal const string DeleteRoom23Sql = """
        DELETE FROM IpdTranColeTbl
        WHERE chOp1Date = :SDate AND chOp1RoomType IN ('2', '3')
        """;

    // B01-B06 are kept as explicit UNION ALL branches. Monetary values always
    // come from Sub1-Sub6; rlOp3DrgTot only selects the drug event branch.
    internal const string InsertRoom23Sql = """
        INSERT INTO IpdTranColeTbl
            (chOp1RoomType, chOp1Date, chOp1Dct, intSelfAmt, intClaimAmt,
             intClaimAmt35, intAMT, chOp1Fin1)
        WITH src AS (
          /* B01 DRG IDate positive */
          SELECT '2' room_type, :SDate acc_date, SUBSTR(d.chOp3Dct,1,2) dct,
                 d.chOp3PFin1 fin, DECODE(d.chOp3SPay,'0','1','4','1','3') spay,
                 SUM(d.rlOp3Sub1+d.rlOp3Sub2+d.rlOp3Sub3+d.rlOp3Sub4+d.rlOp3Sub5+d.rlOp3Sub6) amount
          FROM IpdDrgTbl d WHERE SUBSTR(d.chOp3IDate,1,7)=:SDate AND d.rlOp3DrgTot>0
          GROUP BY SUBSTR(d.chOp3Dct,1,2),d.chOp3PFin1,DECODE(d.chOp3SPay,'0','1','4','1','3')
          UNION ALL
          /* B02 DRG DCDate negative, reversed */
          SELECT '2',:SDate,SUBSTR(d.chOp3Dct,1,2),d.chOp3PFin1,DECODE(d.chOp3SPay,'0','1','4','1','3'),
                 SUM(d.rlOp3Sub1+d.rlOp3Sub2+d.rlOp3Sub3+d.rlOp3Sub4+d.rlOp3Sub5+d.rlOp3Sub6)*-1
          FROM IpdDrgTbl d WHERE SUBSTR(d.chOp3DCDate,1,7)=:SDate AND SUBSTR(d.chOp3IDate,1,7)<>:SDate AND d.rlOp3DrgTot<0
          GROUP BY SUBSTR(d.chOp3Dct,1,2),d.chOp3PFin1,DECODE(d.chOp3SPay,'0','1','4','1','3')
          UNION ALL
          /* B03 ORD IDate */
          SELECT '2',:SDate,SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1,DECODE(o.chOp4SPay,'0','1','4','1','3'),
                 SUM(o.rlOp4Sub1+o.rlOp4Sub2+o.rlOp4Sub3+o.rlOp4Sub4+o.rlOp4Sub5+o.rlOp4Sub6)
          FROM IpdOrdTbl o WHERE SUBSTR(o.chOp4IDate,1,7)=:SDate
          GROUP BY SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1,DECODE(o.chOp4SPay,'0','1','4','1','3')
          UNION ALL
          /* B04 DRG DCDate positive, reversed */
          SELECT '3',:SDate,SUBSTR(d.chOp3Dct,1,2),d.chOp3PFin1,DECODE(d.chOp3SPay,'0','1','4','1','3'),
                 SUM(d.rlOp3Sub1+d.rlOp3Sub2+d.rlOp3Sub3+d.rlOp3Sub4+d.rlOp3Sub5+d.rlOp3Sub6)*-1
          FROM IpdDrgTbl d WHERE SUBSTR(d.chOp3DCDate,1,7)=:SDate AND SUBSTR(d.chOp3IDate,1,7)<>:SDate AND d.rlOp3DrgTot>0
          GROUP BY SUBSTR(d.chOp3Dct,1,2),d.chOp3PFin1,DECODE(d.chOp3SPay,'0','1','4','1','3')
          UNION ALL
          /* B05 DRG IDate negative */
          SELECT '3',:SDate,SUBSTR(d.chOp3Dct,1,2),d.chOp3PFin1,DECODE(d.chOp3SPay,'0','1','4','1','3'),
                 SUM(d.rlOp3Sub1+d.rlOp3Sub2+d.rlOp3Sub3+d.rlOp3Sub4+d.rlOp3Sub5+d.rlOp3Sub6)
          FROM IpdDrgTbl d WHERE SUBSTR(d.chOp3IDate,1,7)=:SDate AND d.rlOp3DrgTot<0
          GROUP BY SUBSTR(d.chOp3Dct,1,2),d.chOp3PFin1,DECODE(d.chOp3SPay,'0','1','4','1','3')
          UNION ALL
          /* B06 ORD DCDate, reversed */
          SELECT '3',:SDate,SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1,DECODE(o.chOp4SPay,'0','1','4','1','3'),
                 SUM(o.rlOp4Sub1+o.rlOp4Sub2+o.rlOp4Sub3+o.rlOp4Sub4+o.rlOp4Sub5+o.rlOp4Sub6)*-1
          FROM IpdOrdTbl o WHERE SUBSTR(o.chOp4DCDate,1,7)=:SDate AND SUBSTR(o.chOp4IDate,1,7)<>:SDate
          GROUP BY SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1,DECODE(o.chOp4SPay,'0','1','4','1','3')
        )
        SELECT room_type,acc_date,dct,
               SUM(CASE WHEN spay='1' THEN amount ELSE 0 END),
               SUM(CASE WHEN spay='3' THEN amount ELSE 0 END),0,
               SUM(amount),DECODE(fin,'35','30',fin)
        FROM src WHERE fin IN ('01','30','35') AND dct<>'75'
        GROUP BY room_type,acc_date,dct,DECODE(fin,'35','30',fin)
        HAVING SUM(amount)<>0
        """;

    internal const string DeleteRoom45Sql = """
        DELETE FROM IpdTranColeTbl
        WHERE chOp1Date = :SDate AND chOp1RoomType IN ('4', '5')
        """;

    internal const string InsertRoom45Sql = """
        INSERT INTO IpdTranColeTbl
            (chOp1RoomType,chOp1Date,chOp1Dct,intSelfAmt,intClaimAmt,intClaimAmt35,intAMT,chOp1Fin1)
        WITH close_rows AS (
          SELECT a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No,
                 MAX(a.vchAccDate||SUBSTR(a.vchOp2RecCTime,1,4)) close_time
          FROM IpdAccCaseDayTbl a
          WHERE a.vchAccDate=:SDate AND a.vchAccMrNo NOT IN ('C36979','1000000') AND a.vchAccBid<>'預繳'
          GROUP BY a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No
        ), src AS (
          /* B07 DRG current */ SELECT '4' room_type,SUBSTR(d.chOp3Dct,1,2) dct,d.chOp3PFin1 fin,SUM(d.rlOp3Sub1+d.rlOp3Sub2+d.rlOp3Sub3+d.rlOp3Sub4+d.rlOp3Sub5+d.rlOp3Sub6) amount FROM IpdDrgTbl d JOIN close_rows c ON c.chOp1Date=d.chOp1Date AND c.chOp1Time=d.chOp1Time AND c.chOp1Room=d.chOp1Room AND c.intOp1No=d.intOp1No WHERE d.chOp3IDate<=c.close_time AND (d.chOp3DCDate>c.close_time OR RTRIM(d.chOp3DCDate) IS NULL) GROUP BY SUBSTR(d.chOp3Dct,1,2),d.chOp3PFin1
          UNION ALL /* B08 ORD current */ SELECT '4',SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1,SUM(o.rlOp4Sub1+o.rlOp4Sub2+o.rlOp4Sub3+o.rlOp4Sub4+o.rlOp4Sub5+o.rlOp4Sub6) FROM IpdOrdTbl o JOIN close_rows c ON c.chOp1Date=o.chOp1Date AND c.chOp1Time=o.chOp1Time AND c.chOp1Room=o.chOp1Room AND c.intOp1No=o.intOp1No WHERE o.chOp4IDate<=c.close_time AND (o.chOp4DCDate>c.close_time OR RTRIM(o.chOp4DCDate) IS NULL) GROUP BY SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1
          UNION ALL /* B09 DRG prior reversal */ SELECT '5',SUBSTR(d.chOp3Dct,1,2),d.chOp3PFin1,SUM(d.rlOp3Sub1+d.rlOp3Sub2+d.rlOp3Sub3+d.rlOp3Sub4+d.rlOp3Sub5+d.rlOp3Sub6)*-1 FROM IpdDrgTbl d JOIN close_rows c ON c.chOp1Date=d.chOp1Date AND c.chOp1Time=d.chOp1Time AND c.chOp1Room=d.chOp1Room AND c.intOp1No=d.intOp1No WHERE d.chOp3IDate<c.close_time GROUP BY SUBSTR(d.chOp3Dct,1,2),d.chOp3PFin1
          UNION ALL /* B10 ORD prior reversal */ SELECT '5',SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1,SUM(o.rlOp4Sub1+o.rlOp4Sub2+o.rlOp4Sub3+o.rlOp4Sub4+o.rlOp4Sub5+o.rlOp4Sub6)*-1 FROM IpdOrdTbl o JOIN close_rows c ON c.chOp1Date=o.chOp1Date AND c.chOp1Time=o.chOp1Time AND c.chOp1Room=o.chOp1Room AND c.intOp1No=o.intOp1No WHERE o.chOp4IDate<c.close_time GROUP BY SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1
          UNION ALL /* B11 contract current */ SELECT '4',SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1,SUM(o.rlOp4Sub1+o.rlOp4Sub2+o.rlOp4Sub3+o.rlOp4Sub4+o.rlOp4Sub5+o.rlOp4Sub6) FROM IpdOrdTbl o JOIN close_rows c ON c.chOp1Date=o.chOp1Date AND c.chOp1Time=o.chOp1Time AND c.chOp1Room=o.chOp1Room AND c.intOp1No=o.intOp1No WHERE o.chOp4Dct='64' GROUP BY SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1
          UNION ALL /* B12 contract prior reversal */ SELECT '5',SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1,SUM(o.rlOp4Sub1+o.rlOp4Sub2+o.rlOp4Sub3+o.rlOp4Sub4+o.rlOp4Sub5+o.rlOp4Sub6)*-1 FROM IpdOrdTbl o JOIN close_rows c ON c.chOp1Date=o.chOp1Date AND c.chOp1Time=o.chOp1Time AND c.chOp1Room=o.chOp1Room AND c.intOp1No=o.intOp1No WHERE o.chOp4Dct='64' GROUP BY SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1
        )
        SELECT room_type,:SDate,dct,0,0,0,SUM(amount),DECODE(fin,'35','30',fin)
        FROM src WHERE fin IN ('01','30','35') AND dct<>'75'
        GROUP BY room_type,dct,DECODE(fin,'35','30',fin) HAVING SUM(amount)<>0
        """;

    internal const string DeleteMrNoSql = """
        DELETE FROM IpdTranColeMrNoTbl WHERE chIDate=:SDate AND chType IN ('C','D')
        """;

    // Patient-level mappings retain all four event branches per Helper. The target
    // amounts follow the established Self/Claim/Part split and type-C outer reversal.
    internal const string InsertMrNoDSql = """
        INSERT INTO IpdTranColeMrNoTbl
          (chIDate,chType,chDate,chTime,chRoom,intNo,chMrNo,intSelfAmt,intClaimAmt,intPartAmt)
        WITH src AS (
          /* B13 DRG IDate */ SELECT b.chOp1Date,b.chOp1Time,b.chOp1Room,b.intOp1No,b.chOp1MrNo,d.chOp3PFin1 fin,SUM(d.rlOp3Sub1) self_amt,SUM(d.rlOp3Sub2) claim_amt,SUM(d.rlOp3Sub3+d.rlOp3Sub4+d.rlOp3Sub5+d.rlOp3Sub6) part_amt FROM IpdDrgTbl d JOIN IpdBasicTbl b ON b.chOp1Date=d.chOp1Date AND b.chOp1Time=d.chOp1Time AND b.chOp1Room=d.chOp1Room AND b.intOp1No=d.intOp1No WHERE SUBSTR(d.chOp3IDate,1,7)=:SDate GROUP BY b.chOp1Date,b.chOp1Time,b.chOp1Room,b.intOp1No,b.chOp1MrNo,d.chOp3PFin1
          UNION ALL /* B14 ORD IDate */ SELECT b.chOp1Date,b.chOp1Time,b.chOp1Room,b.intOp1No,b.chOp1MrNo,o.chOp4PFin1,SUM(o.rlOp4Sub1),SUM(o.rlOp4Sub2),SUM(o.rlOp4Sub3+o.rlOp4Sub4+o.rlOp4Sub5+o.rlOp4Sub6) FROM IpdOrdTbl o JOIN IpdBasicTbl b ON b.chOp1Date=o.chOp1Date AND b.chOp1Time=o.chOp1Time AND b.chOp1Room=o.chOp1Room AND b.intOp1No=o.intOp1No WHERE SUBSTR(o.chOp4IDate,1,7)=:SDate GROUP BY b.chOp1Date,b.chOp1Time,b.chOp1Room,b.intOp1No,b.chOp1MrNo,o.chOp4PFin1
          UNION ALL /* B15 DRG DCDate reversal */ SELECT b.chOp1Date,b.chOp1Time,b.chOp1Room,b.intOp1No,b.chOp1MrNo,d.chOp3PFin1,SUM(d.rlOp3Sub1)*-1,SUM(d.rlOp3Sub2)*-1,SUM(d.rlOp3Sub3+d.rlOp3Sub4+d.rlOp3Sub5+d.rlOp3Sub6)*-1 FROM IpdDrgTbl d JOIN IpdBasicTbl b ON b.chOp1Date=d.chOp1Date AND b.chOp1Time=d.chOp1Time AND b.chOp1Room=d.chOp1Room AND b.intOp1No=d.intOp1No WHERE SUBSTR(d.chOp3DCDate,1,7)=:SDate AND SUBSTR(d.chOp3IDate,1,7)<>:SDate GROUP BY b.chOp1Date,b.chOp1Time,b.chOp1Room,b.intOp1No,b.chOp1MrNo,d.chOp3PFin1
          UNION ALL /* B16 ORD DCDate reversal */ SELECT b.chOp1Date,b.chOp1Time,b.chOp1Room,b.intOp1No,b.chOp1MrNo,o.chOp4PFin1,SUM(o.rlOp4Sub1)*-1,SUM(o.rlOp4Sub2)*-1,SUM(o.rlOp4Sub3+o.rlOp4Sub4+o.rlOp4Sub5+o.rlOp4Sub6)*-1 FROM IpdOrdTbl o JOIN IpdBasicTbl b ON b.chOp1Date=o.chOp1Date AND b.chOp1Time=o.chOp1Time AND b.chOp1Room=o.chOp1Room AND b.intOp1No=o.intOp1No WHERE SUBSTR(o.chOp4DCDate,1,7)=:SDate AND SUBSTR(o.chOp4IDate,1,7)<>:SDate GROUP BY b.chOp1Date,b.chOp1Time,b.chOp1Room,b.intOp1No,b.chOp1MrNo,o.chOp4PFin1
        )
        SELECT :SDate,'D',chOp1Date,chOp1Time,chOp1Room,intOp1No,chOp1MrNo,
               SUM(self_amt),SUM(claim_amt),SUM(part_amt)
        FROM src GROUP BY chOp1Date,chOp1Time,chOp1Room,intOp1No,chOp1MrNo
        HAVING SUM(self_amt)<>0 OR SUM(claim_amt)<>0 OR SUM(part_amt)<>0
        """;

    internal const string InsertMrNoCSql = """
        INSERT INTO IpdTranColeMrNoTbl
          (chIDate,chType,chDate,chTime,chRoom,intNo,chMrNo,intSelfAmt,intClaimAmt,intPartAmt)
        WITH closed AS (
          SELECT a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No,a.vchAccMrNo,
                 MAX(a.vchAccDate||SUBSTR(a.vchOp2RecCTime,1,4)) close_time
          FROM IpdAccCaseDayTbl a WHERE a.vchAccDate=:SDate AND a.vchAccBid<>'預繳'
          GROUP BY a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No,a.vchAccMrNo
        ), src AS (
          /* B17 DRG current */ SELECT c.chOp1Date,c.chOp1Time,c.chOp1Room,c.intOp1No,c.vchAccMrNo,SUM(d.rlOp3Sub1) self_amt,SUM(d.rlOp3Sub2) claim_amt,SUM(d.rlOp3Sub3+d.rlOp3Sub4+d.rlOp3Sub5+d.rlOp3Sub6) part_amt FROM IpdDrgTbl d JOIN closed c ON c.chOp1Date=d.chOp1Date AND c.chOp1Time=d.chOp1Time AND c.chOp1Room=d.chOp1Room AND c.intOp1No=d.intOp1No WHERE d.chOp3IDate<=c.close_time AND (d.chOp3DCDate>c.close_time OR RTRIM(d.chOp3DCDate) IS NULL) GROUP BY c.chOp1Date,c.chOp1Time,c.chOp1Room,c.intOp1No,c.vchAccMrNo
          UNION ALL /* B18 ORD current */ SELECT c.chOp1Date,c.chOp1Time,c.chOp1Room,c.intOp1No,c.vchAccMrNo,SUM(o.rlOp4Sub1),SUM(o.rlOp4Sub2),SUM(o.rlOp4Sub3+o.rlOp4Sub4+o.rlOp4Sub5+o.rlOp4Sub6) FROM IpdOrdTbl o JOIN closed c ON c.chOp1Date=o.chOp1Date AND c.chOp1Time=o.chOp1Time AND c.chOp1Room=o.chOp1Room AND c.intOp1No=o.intOp1No WHERE o.chOp4IDate<=c.close_time AND (o.chOp4DCDate>c.close_time OR RTRIM(o.chOp4DCDate) IS NULL) GROUP BY c.chOp1Date,c.chOp1Time,c.chOp1Room,c.intOp1No,c.vchAccMrNo
          UNION ALL /* B19 DRG prior reversal */ SELECT c.chOp1Date,c.chOp1Time,c.chOp1Room,c.intOp1No,c.vchAccMrNo,SUM(d.rlOp3Sub1)*-1,SUM(d.rlOp3Sub2)*-1,SUM(d.rlOp3Sub3+d.rlOp3Sub4+d.rlOp3Sub5+d.rlOp3Sub6)*-1 FROM IpdDrgTbl d JOIN closed c ON c.chOp1Date=d.chOp1Date AND c.chOp1Time=d.chOp1Time AND c.chOp1Room=d.chOp1Room AND c.intOp1No=d.intOp1No WHERE d.chOp3IDate<c.close_time GROUP BY c.chOp1Date,c.chOp1Time,c.chOp1Room,c.intOp1No,c.vchAccMrNo
          UNION ALL /* B20 ORD prior reversal */ SELECT c.chOp1Date,c.chOp1Time,c.chOp1Room,c.intOp1No,c.vchAccMrNo,SUM(o.rlOp4Sub1)*-1,SUM(o.rlOp4Sub2)*-1,SUM(o.rlOp4Sub3+o.rlOp4Sub4+o.rlOp4Sub5+o.rlOp4Sub6)*-1 FROM IpdOrdTbl o JOIN closed c ON c.chOp1Date=o.chOp1Date AND c.chOp1Time=o.chOp1Time AND c.chOp1Room=o.chOp1Room AND c.intOp1No=o.intOp1No WHERE o.chOp4IDate<c.close_time GROUP BY c.chOp1Date,c.chOp1Time,c.chOp1Room,c.intOp1No,c.vchAccMrNo
        )
        SELECT :SDate,'C',chOp1Date,chOp1Time,chOp1Room,intOp1No,vchAccMrNo,
               SUM(self_amt)*-1,SUM(claim_amt)*-1,SUM(part_amt)*-1
        FROM src GROUP BY chOp1Date,chOp1Time,chOp1Room,intOp1No,vchAccMrNo
        HAVING SUM(self_amt)<>0 OR SUM(claim_amt)<>0 OR SUM(part_amt)<>0
        """;

    internal const string IntegritySql = """
        SELECT COUNT(*) FROM (
          SELECT 1 FROM IpdTranColeTbl
          WHERE chOp1Date=:SDate
            AND (chOp1RoomType NOT IN ('2','3','4','5') OR chOp1Fin1 NOT IN ('01','30','35'))
          UNION ALL
          SELECT 1 FROM IpdTranColeMrNoTbl
          WHERE chIDate=:SDate AND chType NOT IN ('C','D')
        )
        """;
}
