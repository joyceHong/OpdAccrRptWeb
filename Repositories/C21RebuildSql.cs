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

    // The six VB6 daily-ledger branches feed both direct and derived accounting items.
    internal const string InsertRoom23Sql = """
        INSERT INTO IpdTranColeTbl
            (chOp1RoomType,chOp1Date,chOp1Dct,intSelfAmt,intClaimAmt,intClaimAmt35,intAMT,chOp1Fin1)
        WITH valid_drg AS (
          SELECT d.* FROM IpdDrgTbl d JOIN IpdBasicTbl b
            ON b.chOp1Date=d.chOp1Date AND b.chOp1Time=d.chOp1Time
           AND b.chOp1Room=d.chOp1Room AND b.intOp1No=d.intOp1No
          WHERE b.chOp1MrNo NOT IN ('C36979','1000000')
            AND (d.chOp3Proj NOT IN ('I','D') OR RTRIM(d.chOp3Proj) IS NULL)
            AND (d.chOp3Rep3Flg<>'S' OR RTRIM(d.chOp3Rep3Flg) IS NULL)
            AND (d.chOp3Stat NOT IN ('09','10','11','12') OR RTRIM(d.chOp3Stat) IS NULL)
        ), valid_ord AS (
          SELECT o.* FROM IpdOrdTbl o JOIN IpdBasicTbl b
            ON b.chOp1Date=o.chOp1Date AND b.chOp1Time=o.chOp1Time
           AND b.chOp1Room=o.chOp1Room AND b.intOp1No=o.intOp1No
          WHERE b.chOp1MrNo NOT IN ('C36979','1000000')
            AND (o.chOp4Proj NOT IN ('I','D') OR RTRIM(o.chOp4Proj) IS NULL)
        ), src AS (
          /* B01 DRG IDate positive */
          SELECT '2' room_type,SUBSTR(d.chOp3Dct,1,2) dct,d.chOp3PFin1 fin,
                 DECODE(d.chOp3SPay,'0','1','4','1','3') spay,
                 d.rlOp3Sub1 s1,d.rlOp3Sub2 s2,d.rlOp3Sub3 s3,d.rlOp3Sub4 s4,d.rlOp3Sub5 s5,d.rlOp3Sub6 s6,1 sign_value
          FROM valid_drg d WHERE SUBSTR(d.chOp3IDate,1,7)=:SDate
            AND (SUBSTR(d.chOp3DCDate,1,7)<>:SDate OR RTRIM(d.chOp3DCDate) IS NULL) AND d.rlOp3DrgTot>0
          UNION ALL
          /* B02 DRG DCDate negative, reversed */
          SELECT '2',SUBSTR(d.chOp3Dct,1,2),d.chOp3PFin1,DECODE(d.chOp3SPay,'0','1','4','1','3'),
                 d.rlOp3Sub1,d.rlOp3Sub2,d.rlOp3Sub3,d.rlOp3Sub4,d.rlOp3Sub5,d.rlOp3Sub6,-1
          FROM valid_drg d WHERE SUBSTR(d.chOp3DCDate,1,7)=:SDate
            AND SUBSTR(d.chOp3IDate,1,7)<>:SDate AND RTRIM(d.chOp3IDate) IS NOT NULL AND d.rlOp3DrgTot<0
          UNION ALL
          /* B03 ORD IDate */
          SELECT '2',SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1,DECODE(o.chOp4SPay,'0','1','4','1','3'),
                 o.rlOp4Sub1,o.rlOp4Sub2,o.rlOp4Sub3,o.rlOp4Sub4,o.rlOp4Sub5,o.rlOp4Sub6,1
          FROM valid_ord o WHERE SUBSTR(o.chOp4IDate,1,7)=:SDate
            AND (SUBSTR(o.chOp4DCDate,1,7)<>:SDate OR RTRIM(o.chOp4DCDate) IS NULL)
          UNION ALL
          /* B04 DRG DCDate positive, reversed */
          SELECT '3',SUBSTR(d.chOp3Dct,1,2),d.chOp3PFin1,DECODE(d.chOp3SPay,'0','1','4','1','3'),
                 d.rlOp3Sub1,d.rlOp3Sub2,d.rlOp3Sub3,d.rlOp3Sub4,d.rlOp3Sub5,d.rlOp3Sub6,-1
          FROM valid_drg d WHERE SUBSTR(d.chOp3DCDate,1,7)=:SDate
            AND SUBSTR(d.chOp3IDate,1,7)<>:SDate AND RTRIM(d.chOp3IDate) IS NOT NULL AND d.rlOp3DrgTot>0
          UNION ALL
          /* B05 DRG IDate negative */
          SELECT '3',SUBSTR(d.chOp3Dct,1,2),d.chOp3PFin1,DECODE(d.chOp3SPay,'0','1','4','1','3'),
                 d.rlOp3Sub1,d.rlOp3Sub2,d.rlOp3Sub3,d.rlOp3Sub4,d.rlOp3Sub5,d.rlOp3Sub6,1
          FROM valid_drg d WHERE SUBSTR(d.chOp3IDate,1,7)=:SDate
            AND (SUBSTR(d.chOp3DCDate,1,7)<>:SDate OR RTRIM(d.chOp3DCDate) IS NULL) AND d.rlOp3DrgTot<0
          UNION ALL
          /* B06 ORD DCDate, reversed */
          SELECT '3',SUBSTR(o.chOp4Dct,1,2),o.chOp4PFin1,DECODE(o.chOp4SPay,'0','1','4','1','3'),
                 o.rlOp4Sub1,o.rlOp4Sub2,o.rlOp4Sub3,o.rlOp4Sub4,o.rlOp4Sub5,o.rlOp4Sub6,-1
          FROM valid_ord o WHERE SUBSTR(o.chOp4DCDate,1,7)=:SDate
            AND SUBSTR(o.chOp4IDate,1,7)<>:SDate AND RTRIM(o.chOp4IDate) IS NOT NULL
        ), legacy_groups AS (
          SELECT room_type,dct,fin,spay,
                 SUM(CASE WHEN dct='49' THEN -NVL(s2,0) ELSE
                   NVL(s1,0)+NVL(s2,0)+NVL(s3,0)+NVL(s4,0)+NVL(s5,0)+NVL(s6,0) END * sign_value) amount,
                 SUM(NVL(s1,0)*sign_value) pay_amt,SUM(NVL(s2,0)*sign_value) claim_amt,
                 SUM(NVL(s3,0)*sign_value) vip_amt,SUM(NVL(s4,0)*sign_value) charity_amt,
                 SUM(NVL(s5,0)*sign_value) contract_amt,SUM(NVL(s6,0)*sign_value) debt_amt
          FROM src GROUP BY room_type,dct,fin,spay
          HAVING SUM(NVL(s1,0)*sign_value)<>0 OR SUM(NVL(s2,0)*sign_value)<>0
              OR SUM(NVL(s3,0)*sign_value)<>0 OR SUM(NVL(s4,0)*sign_value)<>0
              OR SUM(NVL(s5,0)*sign_value)<>0 OR SUM(NVL(s6,0)*sign_value)<>0
        ), direct_rows AS (
          SELECT room_type,dct,fin,SUM(ROUND(amount,0)) amount
          FROM legacy_groups WHERE dct<>'75'
          GROUP BY room_type,dct,fin HAVING SUM(ROUND(amount,0))<>0
        ), derived_base AS (
          SELECT room_type,fin,
                 CASE WHEN fin='30' AND (spay='3' OR (spay='1' AND dct='49'))
                      THEN claim_amt ELSE 0 END d66,
                 vip_amt d51,contract_amt d63,debt_amt d75,charity_amt d52,
                 CASE WHEN spay<>'1' THEN 0
                      WHEN fin IN ('01','35') AND dct NOT IN ('69','75') THEN pay_amt
                      WHEN fin='30' AND dct='49' THEN -vip_amt-contract_amt
                      WHEN fin='30' AND dct NOT IN ('49','69','75') THEN pay_amt
                      ELSE 0 END d62
          FROM legacy_groups
          WHERE fin IN ('01','30','35') AND dct NOT IN ('56','57','58','80','64','52','53')
        ), derived_rows AS (
          SELECT room_type,dct,fin,ROUND(SUM(amount),0) amount
          FROM derived_base
          UNPIVOT (amount FOR dct IN (d66 AS '66',d51 AS '51',d63 AS '63',
                                     d75 AS '75',d52 AS '52',d62 AS '62'))
          GROUP BY room_type,dct,fin HAVING ROUND(SUM(amount),0)<>0
        )
        SELECT room_type,:SDate,dct,0,0,0,amount,fin FROM direct_rows
        UNION ALL
        SELECT room_type,:SDate,dct,0,0,0,amount,fin FROM derived_rows
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
        ), prior_rows AS (
          SELECT a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No,
                 MAX(a.vchAccDate||SUBSTR(a.vchOp2RecCTime,1,4)) close_time
          FROM IpdAccCaseDayTbl a JOIN close_rows c
            ON c.chOp1Date=a.chOp1Date AND c.chOp1Time=a.chOp1Time
           AND c.chOp1Room=a.chOp1Room AND c.intOp1No=a.intOp1No
          WHERE a.vchAccDate<:SDate AND a.vchAccBid<>'預繳'
          GROUP BY a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No
        ), contract_close AS (
          SELECT a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No,
                 MAX(a.vchAccDate||SUBSTR(a.vchOp2RecCTime,1,4)) close_time
          FROM IpdContractAccTbl a
          WHERE a.vchAccDate=:SDate AND a.vchAccMrNo NOT IN ('C36979','1000000') AND a.vchAccBid<>'預繳'
          GROUP BY a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No
        ), contract_prior AS (
          SELECT a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No,
                 MAX(a.vchAccDate||SUBSTR(a.vchOp2RecCTime,1,4)) close_time
          FROM IpdContractAccTbl a JOIN contract_close c
            ON c.chOp1Date=a.chOp1Date AND c.chOp1Time=a.chOp1Time
           AND c.chOp1Room=a.chOp1Room AND c.intOp1No=a.intOp1No
          WHERE a.vchAccDate<:SDate AND a.vchAccBid<>'預繳'
          GROUP BY a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No
        ), src AS (
          /* B07 DRG close_rows */
          SELECT '4' room_type,SUBSTR(d.chOp3Dct,1,2) dct,d.chOp3PFin1 fin,
                 DECODE(d.chOp3SPay,'0','1','4','1','3') spay,
                 d.rlOp3Sub1 s1,d.rlOp3Sub2 s2,d.rlOp3Sub3 s3,d.rlOp3Sub4 s4,d.rlOp3Sub5 s5,d.rlOp3Sub6 s6,1 sign_value
          FROM IpdDrgTbl d JOIN close_rows c ON c.chOp1Date=d.chOp1Date AND c.chOp1Time=d.chOp1Time
            AND c.chOp1Room=d.chOp1Room AND c.intOp1No=d.intOp1No
          WHERE d.chOp3IDate<=c.close_time AND RTRIM(d.chOp3IDate) IS NOT NULL
            AND (d.chOp3DCDate>c.close_time OR RTRIM(d.chOp3DCDate) IS NULL)
            AND (d.chOp3Proj NOT IN ('I','D') OR RTRIM(d.chOp3Proj) IS NULL)
            AND (d.chOp3Rep3Flg<>'S' OR RTRIM(d.chOp3Rep3Flg) IS NULL)
            AND (d.chOp3Stat NOT IN ('09','10','11','12') OR RTRIM(d.chOp3Stat) IS NULL)
          UNION ALL
          /* B08 ORD close_rows */
          SELECT '4' room_type,SUBSTR(o.chOp4Dct,1,2) dct,o.chOp4PFin1 fin,
                 DECODE(o.chOp4SPay,'0','1','4','1','3') spay,
                 o.rlOp4Sub1,o.rlOp4Sub2,o.rlOp4Sub3,o.rlOp4Sub4,o.rlOp4Sub5,o.rlOp4Sub6,1 sign_value
          FROM IpdOrdTbl o JOIN close_rows c ON c.chOp1Date=o.chOp1Date AND c.chOp1Time=o.chOp1Time
            AND c.chOp1Room=o.chOp1Room AND c.intOp1No=o.intOp1No
          WHERE o.chOp4IDate<=c.close_time AND RTRIM(o.chOp4IDate) IS NOT NULL
            AND (o.chOp4DCDate>c.close_time OR RTRIM(o.chOp4DCDate) IS NULL)
            AND (o.chOp4Proj NOT IN ('I','D') OR RTRIM(o.chOp4Proj) IS NULL)
          UNION ALL
          /* B09 DRG prior_rows */
          SELECT '5' room_type,SUBSTR(d.chOp3Dct,1,2) dct,d.chOp3PFin1 fin,
                 DECODE(d.chOp3SPay,'0','1','4','1','3') spay,
                 d.rlOp3Sub1,d.rlOp3Sub2,d.rlOp3Sub3,d.rlOp3Sub4,d.rlOp3Sub5,d.rlOp3Sub6,-1 sign_value
          FROM IpdDrgTbl d JOIN prior_rows c ON c.chOp1Date=d.chOp1Date AND c.chOp1Time=d.chOp1Time
            AND c.chOp1Room=d.chOp1Room AND c.intOp1No=d.intOp1No
          WHERE d.chOp3IDate<=c.close_time AND RTRIM(d.chOp3IDate) IS NOT NULL
            AND (d.chOp3DCDate>c.close_time OR RTRIM(d.chOp3DCDate) IS NULL)
            AND (d.chOp3Proj NOT IN ('I','D') OR RTRIM(d.chOp3Proj) IS NULL)
            AND (d.chOp3Rep3Flg<>'S' OR RTRIM(d.chOp3Rep3Flg) IS NULL)
            AND (d.chOp3Stat NOT IN ('09','10','11','12') OR RTRIM(d.chOp3Stat) IS NULL)
          UNION ALL
          /* B10 ORD prior_rows */
          SELECT '5' room_type,SUBSTR(o.chOp4Dct,1,2) dct,o.chOp4PFin1 fin,
                 DECODE(o.chOp4SPay,'0','1','4','1','3') spay,
                 o.rlOp4Sub1,o.rlOp4Sub2,o.rlOp4Sub3,o.rlOp4Sub4,o.rlOp4Sub5,o.rlOp4Sub6,-1 sign_value
          FROM IpdOrdTbl o JOIN prior_rows c ON c.chOp1Date=o.chOp1Date AND c.chOp1Time=o.chOp1Time
            AND c.chOp1Room=o.chOp1Room AND c.intOp1No=o.intOp1No
          WHERE o.chOp4IDate<=c.close_time AND RTRIM(o.chOp4IDate) IS NOT NULL
            AND (o.chOp4DCDate>c.close_time OR RTRIM(o.chOp4DCDate) IS NULL)
            AND (o.chOp4Proj NOT IN ('I','D') OR RTRIM(o.chOp4Proj) IS NULL)
          UNION ALL
          /* B11 ORD contract_close */
          SELECT '4' room_type,SUBSTR(o.chOp4Dct,1,2) dct,o.chOp4PFin1 fin,
                 DECODE(o.chOp4SPay,'0','1','4','1','3') spay,
                 o.rlOp4Sub1,o.rlOp4Sub2,o.rlOp4Sub3,o.rlOp4Sub4,o.rlOp4Sub5,o.rlOp4Sub6,1 sign_value
          FROM IpdOrdTbl o JOIN contract_close c ON c.chOp1Date=o.chOp1Date AND c.chOp1Time=o.chOp1Time
            AND c.chOp1Room=o.chOp1Room AND c.intOp1No=o.intOp1No
          WHERE o.chOp4IDate<=c.close_time AND RTRIM(o.chOp4IDate) IS NOT NULL
            AND (o.chOp4DCDate>c.close_time OR RTRIM(o.chOp4DCDate) IS NULL)
            AND (o.chOp4Proj NOT IN ('I','D') OR RTRIM(o.chOp4Proj) IS NULL)
            AND o.chOp4Dct='64'
          UNION ALL
          /* B12 ORD contract_prior */
          SELECT '5' room_type,SUBSTR(o.chOp4Dct,1,2) dct,o.chOp4PFin1 fin,
                 DECODE(o.chOp4SPay,'0','1','4','1','3') spay,
                 o.rlOp4Sub1,o.rlOp4Sub2,o.rlOp4Sub3,o.rlOp4Sub4,o.rlOp4Sub5,o.rlOp4Sub6,-1 sign_value
          FROM IpdOrdTbl o JOIN contract_prior c ON c.chOp1Date=o.chOp1Date AND c.chOp1Time=o.chOp1Time
            AND c.chOp1Room=o.chOp1Room AND c.intOp1No=o.intOp1No
          WHERE o.chOp4IDate<=c.close_time AND RTRIM(o.chOp4IDate) IS NOT NULL
            AND (o.chOp4DCDate>c.close_time OR RTRIM(o.chOp4DCDate) IS NULL)
            AND (o.chOp4Proj NOT IN ('I','D') OR RTRIM(o.chOp4Proj) IS NULL)
            AND o.chOp4Dct='64'
        ), legacy_groups AS (
          SELECT room_type,dct,fin,spay,
                 SUM(CASE WHEN dct='49' THEN -NVL(s2,0) ELSE
                   NVL(s1,0)+NVL(s2,0)+NVL(s3,0)+NVL(s4,0)+NVL(s5,0)+NVL(s6,0) END * sign_value) amount,
                 SUM(NVL(s1,0)*sign_value) pay_amt,SUM(NVL(s2,0)*sign_value) claim_amt,
                 SUM(NVL(s3,0)*sign_value) vip_amt,SUM(NVL(s4,0)*sign_value) charity_amt,
                 SUM(NVL(s5,0)*sign_value) contract_amt,SUM(NVL(s6,0)*sign_value) debt_amt
          FROM src GROUP BY room_type,dct,fin,spay
          HAVING SUM(NVL(s1,0)*sign_value)<>0 OR SUM(NVL(s2,0)*sign_value)<>0
              OR SUM(NVL(s3,0)*sign_value)<>0 OR SUM(NVL(s4,0)*sign_value)<>0
              OR SUM(NVL(s5,0)*sign_value)<>0 OR SUM(NVL(s6,0)*sign_value)<>0
        ), direct_rows AS (
          SELECT room_type,CASE WHEN dct='75' THEN '24' ELSE dct END dct,fin,SUM(ROUND(amount,0)) amount
          FROM legacy_groups WHERE dct<>'75' OR amount<>0
          GROUP BY room_type,CASE WHEN dct='75' THEN '24' ELSE dct END,fin
          HAVING SUM(ROUND(amount,0))<>0 OR MAX(CASE WHEN dct='75' THEN 1 ELSE 0 END)=1
        ), derived_base AS (
          SELECT room_type,fin,
                 CASE WHEN fin='30' AND (spay='3' OR (spay='1' AND dct='49'))
                      THEN claim_amt ELSE 0 END d66,
                 vip_amt d51,contract_amt d63,
                 CASE WHEN dct='69' THEN debt_amt-pay_amt ELSE debt_amt END d75,
                 charity_amt d52,
                 CASE WHEN spay<>'1' THEN 0
                      WHEN dct='75' THEN pay_amt+debt_amt
                      WHEN fin IN ('01','35') AND dct<>'69' THEN pay_amt
                      WHEN fin='30' AND dct='49' THEN -vip_amt-contract_amt
                      WHEN fin='30' AND dct<>'69' THEN pay_amt
                      ELSE 0 END d62
          FROM legacy_groups
          WHERE fin IN ('01','30','35') AND dct NOT IN ('56','57','58','80','64','52','53')
        ), derived_rows AS (
          SELECT room_type,dct,fin,ROUND(SUM(amount),0) amount
          FROM derived_base
          UNPIVOT (amount FOR dct IN (d66 AS '66',d51 AS '51',d63 AS '63',
                                     d75 AS '75',d52 AS '52',d62 AS '62'))
          GROUP BY room_type,dct,fin HAVING ROUND(SUM(amount),0)<>0
        )
        SELECT room_type,:SDate,dct,0,0,0,amount,fin FROM direct_rows
        UNION ALL
        SELECT room_type,:SDate,dct,0,0,0,amount,fin FROM derived_rows
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
          /* B13 DRG IDate */
          SELECT b.chOp1Date adm_date,b.chOp1Time adm_time,b.chOp1Room adm_room,b.intOp1No adm_no,b.chOp1MrNo mrno,
                 DECODE(d.chOp3PFin1,'35','30',d.chOp3PFin1) fin,SUBSTR(d.chOp3Dct,1,2) dct,
                 DECODE(d.chOp3SPay,'0','1','4','1','3') spay,
                 d.rlOp3Sub1 s1,d.rlOp3Sub2 s2,d.rlOp3Sub3 s3,d.rlOp3Sub4 s4,d.rlOp3Sub5 s5,d.rlOp3Sub6 s6,1 sign_value
          FROM IpdDrgTbl d JOIN IpdBasicTbl b
            ON b.chOp1Date=d.chOp1Date AND b.chOp1Time=d.chOp1Time
           AND b.chOp1Room=d.chOp1Room AND b.intOp1No=d.intOp1No
          WHERE SUBSTR(d.chOp3IDate,1,7)=:SDate
            AND (SUBSTR(d.chOp3DCDate,1,7)<>:SDate OR RTRIM(d.chOp3DCDate) IS NULL)
            AND b.chOp1MrNo NOT IN ('C36979','1000000')
            AND (d.chOp3Proj NOT IN ('I','D') OR RTRIM(d.chOp3Proj) IS NULL)
            AND (d.chOp3SPay IN ('0','4') OR NVL(d.rlOp3Sub5,0)<>0)
            AND d.rlOp3DrgTot<>0
            AND (d.chOp3Rep3Flg<>'S' OR RTRIM(d.chOp3Rep3Flg) IS NULL)
            AND (d.chOp3Stat NOT IN ('09','10','11','12') OR RTRIM(d.chOp3Stat) IS NULL)
          UNION ALL
          /* B14 ORD IDate */
          SELECT b.chOp1Date adm_date,b.chOp1Time adm_time,b.chOp1Room adm_room,b.intOp1No adm_no,b.chOp1MrNo mrno,
                 DECODE(o.chOp4PFin1,'35','30',o.chOp4PFin1) fin,CASE WHEN SUBSTR(o.chOp4Dct,1,2)<='48' OR SUBSTR(o.chOp4Dct,1,2)='50' THEN '01' ELSE SUBSTR(o.chOp4Dct,1,2) END dct,
                 DECODE(o.chOp4SPay,'0','1','4','1','3') spay,
                 o.rlOp4Sub1,o.rlOp4Sub2,o.rlOp4Sub3,o.rlOp4Sub4,o.rlOp4Sub5,o.rlOp4Sub6,1 sign_value
          FROM IpdOrdTbl o JOIN IpdBasicTbl b
            ON b.chOp1Date=o.chOp1Date AND b.chOp1Time=o.chOp1Time
           AND b.chOp1Room=o.chOp1Room AND b.intOp1No=o.intOp1No
          WHERE SUBSTR(o.chOp4IDate,1,7)=:SDate
            AND (SUBSTR(o.chOp4DCDate,1,7)<>:SDate OR RTRIM(o.chOp4DCDate) IS NULL)
            AND b.chOp1MrNo NOT IN ('C36979','1000000')
            AND (o.chOp4Proj NOT IN ('I','D') OR RTRIM(o.chOp4Proj) IS NULL)
            AND (o.chOp4SPay IN ('0','4') OR NVL(o.rlOp4Sub5,0)<>0)
            AND SUBSTR(o.chOp4Dct,1,2)<='50'
          UNION ALL
          /* B15 DRG DCDate */
          SELECT b.chOp1Date adm_date,b.chOp1Time adm_time,b.chOp1Room adm_room,b.intOp1No adm_no,b.chOp1MrNo mrno,
                 DECODE(d.chOp3PFin1,'35','30',d.chOp3PFin1) fin,SUBSTR(d.chOp3Dct,1,2) dct,
                 DECODE(d.chOp3SPay,'0','1','4','1','3') spay,
                 d.rlOp3Sub1,d.rlOp3Sub2,d.rlOp3Sub3,d.rlOp3Sub4,d.rlOp3Sub5,d.rlOp3Sub6,-1 sign_value
          FROM IpdDrgTbl d JOIN IpdBasicTbl b
            ON b.chOp1Date=d.chOp1Date AND b.chOp1Time=d.chOp1Time
           AND b.chOp1Room=d.chOp1Room AND b.intOp1No=d.intOp1No
          WHERE SUBSTR(d.chOp3DCDate,1,7)=:SDate
            AND SUBSTR(d.chOp3IDate,1,7)<>:SDate AND RTRIM(d.chOp3IDate) IS NOT NULL
            AND b.chOp1MrNo NOT IN ('C36979','1000000')
            AND (d.chOp3Proj NOT IN ('I','D') OR RTRIM(d.chOp3Proj) IS NULL)
            AND (d.chOp3SPay IN ('0','4') OR NVL(d.rlOp3Sub5,0)<>0)
            AND d.rlOp3DrgTot<>0
            AND (d.chOp3Rep3Flg<>'S' OR RTRIM(d.chOp3Rep3Flg) IS NULL)
            AND (d.chOp3Stat NOT IN ('09','10','11','12') OR RTRIM(d.chOp3Stat) IS NULL)
          UNION ALL
          /* B16 ORD DCDate */
          SELECT b.chOp1Date adm_date,b.chOp1Time adm_time,b.chOp1Room adm_room,b.intOp1No adm_no,b.chOp1MrNo mrno,
                 DECODE(o.chOp4PFin1,'35','30',o.chOp4PFin1) fin,CASE WHEN SUBSTR(o.chOp4Dct,1,2)<='48' OR SUBSTR(o.chOp4Dct,1,2)='50' THEN '01' ELSE SUBSTR(o.chOp4Dct,1,2) END dct,
                 DECODE(o.chOp4SPay,'0','1','4','1','3') spay,
                 o.rlOp4Sub1,o.rlOp4Sub2,o.rlOp4Sub3,o.rlOp4Sub4,o.rlOp4Sub5,o.rlOp4Sub6,-1 sign_value
          FROM IpdOrdTbl o JOIN IpdBasicTbl b
            ON b.chOp1Date=o.chOp1Date AND b.chOp1Time=o.chOp1Time
           AND b.chOp1Room=o.chOp1Room AND b.intOp1No=o.intOp1No
          WHERE SUBSTR(o.chOp4DCDate,1,7)=:SDate
            AND SUBSTR(o.chOp4IDate,1,7)<>:SDate AND RTRIM(o.chOp4IDate) IS NOT NULL
            AND b.chOp1MrNo NOT IN ('C36979','1000000')
            AND (o.chOp4Proj NOT IN ('I','D') OR RTRIM(o.chOp4Proj) IS NULL)
            AND (o.chOp4SPay IN ('0','4') OR NVL(o.rlOp4Sub5,0)<>0)
            AND SUBSTR(o.chOp4Dct,1,2)<='50'
        ), legacy_groups AS (
          SELECT adm_date,adm_time,adm_room,adm_no,mrno,fin,dct,spay,
                 SUM(CASE WHEN dct='49' THEN -NVL(s2,0) ELSE
                   NVL(s1,0)+NVL(s2,0)+NVL(s3,0)+NVL(s4,0)+NVL(s5,0)+NVL(s6,0) END * sign_value) amount,
                 SUM(NVL(s1,0)*sign_value) s1,SUM(NVL(s2,0)*sign_value) s2,
                 SUM(NVL(s3,0)*sign_value) s3,SUM(NVL(s4,0)*sign_value) s4,
                 SUM(NVL(s5,0)*sign_value) s5,SUM(NVL(s6,0)*sign_value) s6
          FROM src GROUP BY adm_date,adm_time,adm_room,adm_no,mrno,fin,dct,spay
          HAVING SUM(NVL(s1,0)*sign_value)<>0 OR SUM(NVL(s2,0)*sign_value)<>0
              OR SUM(NVL(s3,0)*sign_value)<>0 OR SUM(NVL(s4,0)*sign_value)<>0
              OR SUM(NVL(s5,0)*sign_value)<>0 OR SUM(NVL(s6,0)*sign_value)<>0
        )
        SELECT :SDate,'D',adm_date,adm_time,adm_room,adm_no,mrno,
               SUM(CASE WHEN fin='01' AND dct='01' THEN ROUND(amount,0) ELSE 0 END),
               SUM(CASE WHEN fin='30' AND dct='01' THEN ROUND(amount,0) ELSE 0 END),
               SUM(CASE WHEN fin='30' AND dct='49' THEN ROUND(amount,0) ELSE 0 END)
        FROM legacy_groups
        GROUP BY adm_date,adm_time,adm_room,adm_no,mrno
        """;

    internal const string InsertMrNoCSql = """
        INSERT INTO IpdTranColeMrNoTbl
          (chIDate,chType,chDate,chTime,chRoom,intNo,chMrNo,intSelfAmt,intClaimAmt,intPartAmt)
        WITH close_rows AS (
          SELECT a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No,a.vchAccMrNo mrno,
                 MAX(a.vchAccDate||SUBSTR(a.vchOp2RecCTime,1,4)) close_time
          FROM IpdAccCaseDayTbl a
          WHERE a.vchAccDate=:SDate AND a.vchAccMrNo NOT IN ('C36979','1000000')
            AND a.vchAccBid<>'預繳'
          GROUP BY a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No,a.vchAccMrNo
        ), prior_rows AS (
          SELECT a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No,a.vchAccMrNo mrno,
                 MAX(a.vchAccDate||SUBSTR(a.vchOp2RecCTime,1,4)) close_time
          FROM IpdAccCaseDayTbl a JOIN close_rows c
            ON c.chOp1Date=a.chOp1Date AND c.chOp1Time=a.chOp1Time
           AND c.chOp1Room=a.chOp1Room AND c.intOp1No=a.intOp1No
          WHERE a.vchAccDate<:SDate AND a.vchAccBid<>'預繳'
          GROUP BY a.chOp1Date,a.chOp1Time,a.chOp1Room,a.intOp1No,a.vchAccMrNo
        ), src AS (
          /* B17 DRG close_rows */
          SELECT c.chOp1Date adm_date,c.chOp1Time adm_time,c.chOp1Room adm_room,c.intOp1No adm_no,c.mrno,
                 DECODE(d.chOp3PFin1,'35','30',d.chOp3PFin1) fin,SUBSTR(d.chOp3Dct,1,2) dct,
                 DECODE(d.chOp3SPay,'0','1','4','1','3') spay,
                 d.rlOp3Sub1 s1,d.rlOp3Sub2 s2,d.rlOp3Sub3 s3,d.rlOp3Sub4 s4,d.rlOp3Sub5 s5,d.rlOp3Sub6 s6,1 sign_value
          FROM IpdDrgTbl d JOIN close_rows c
            ON c.chOp1Date=d.chOp1Date AND c.chOp1Time=d.chOp1Time
           AND c.chOp1Room=d.chOp1Room AND c.intOp1No=d.intOp1No
          WHERE d.chOp3IDate<=c.close_time AND RTRIM(d.chOp3IDate) IS NOT NULL
            AND (d.chOp3DCDate>c.close_time OR RTRIM(d.chOp3DCDate) IS NULL)
            AND (d.chOp3Proj NOT IN ('I','D') OR RTRIM(d.chOp3Proj) IS NULL)
            AND (d.chOp3SPay IN ('0','4') OR NVL(d.rlOp3Sub5,0)<>0)
            AND (d.chOp3Rep3Flg<>'S' OR RTRIM(d.chOp3Rep3Flg) IS NULL)
            AND (d.chOp3Stat NOT IN ('09','10','11','12') OR RTRIM(d.chOp3Stat) IS NULL)
          UNION ALL
          /* B18 ORD close_rows */
          SELECT c.chOp1Date adm_date,c.chOp1Time adm_time,c.chOp1Room adm_room,c.intOp1No adm_no,c.mrno,
                 DECODE(o.chOp4PFin1,'35','30',o.chOp4PFin1) fin,CASE WHEN SUBSTR(o.chOp4Dct,1,2)<='48' OR SUBSTR(o.chOp4Dct,1,2)='50' THEN '01' ELSE SUBSTR(o.chOp4Dct,1,2) END dct,
                 DECODE(o.chOp4SPay,'0','1','4','1','3') spay,
                 o.rlOp4Sub1,o.rlOp4Sub2,o.rlOp4Sub3,o.rlOp4Sub4,o.rlOp4Sub5,o.rlOp4Sub6,1 sign_value
          FROM IpdOrdTbl o JOIN close_rows c
            ON c.chOp1Date=o.chOp1Date AND c.chOp1Time=o.chOp1Time
           AND c.chOp1Room=o.chOp1Room AND c.intOp1No=o.intOp1No
          WHERE o.chOp4IDate<=c.close_time AND RTRIM(o.chOp4IDate) IS NOT NULL
            AND (o.chOp4DCDate>c.close_time OR RTRIM(o.chOp4DCDate) IS NULL)
            AND (o.chOp4Proj NOT IN ('I','D') OR RTRIM(o.chOp4Proj) IS NULL)
            AND (o.chOp4SPay IN ('0','4') OR NVL(o.rlOp4Sub5,0)<>0)
            AND (SUBSTR(o.chOp4Dct,1,2)<='50' OR SUBSTR(o.chOp4Dct,1,2) IN ('69','75'))
          UNION ALL
          /* B19 DRG prior_rows */
          SELECT c.chOp1Date adm_date,c.chOp1Time adm_time,c.chOp1Room adm_room,c.intOp1No adm_no,c.mrno,
                 DECODE(d.chOp3PFin1,'35','30',d.chOp3PFin1) fin,SUBSTR(d.chOp3Dct,1,2) dct,
                 DECODE(d.chOp3SPay,'0','1','4','1','3') spay,
                 d.rlOp3Sub1,d.rlOp3Sub2,d.rlOp3Sub3,d.rlOp3Sub4,d.rlOp3Sub5,d.rlOp3Sub6,-1 sign_value
          FROM IpdDrgTbl d JOIN prior_rows c
            ON c.chOp1Date=d.chOp1Date AND c.chOp1Time=d.chOp1Time
           AND c.chOp1Room=d.chOp1Room AND c.intOp1No=d.intOp1No
          WHERE d.chOp3IDate<=c.close_time AND RTRIM(d.chOp3IDate) IS NOT NULL
            AND (d.chOp3DCDate>c.close_time OR RTRIM(d.chOp3DCDate) IS NULL)
            AND (d.chOp3Proj NOT IN ('I','D') OR RTRIM(d.chOp3Proj) IS NULL)
            AND (d.chOp3SPay IN ('0','4') OR NVL(d.rlOp3Sub5,0)<>0)
            AND (d.chOp3Rep3Flg<>'S' OR RTRIM(d.chOp3Rep3Flg) IS NULL)
            AND (d.chOp3Stat NOT IN ('09','10','11','12') OR RTRIM(d.chOp3Stat) IS NULL)
          UNION ALL
          /* B20 ORD prior_rows */
          SELECT c.chOp1Date adm_date,c.chOp1Time adm_time,c.chOp1Room adm_room,c.intOp1No adm_no,c.mrno,
                 DECODE(o.chOp4PFin1,'35','30',o.chOp4PFin1) fin,CASE WHEN SUBSTR(o.chOp4Dct,1,2)<='48' OR SUBSTR(o.chOp4Dct,1,2)='50' THEN '01' ELSE SUBSTR(o.chOp4Dct,1,2) END dct,
                 DECODE(o.chOp4SPay,'0','1','4','1','3') spay,
                 o.rlOp4Sub1,o.rlOp4Sub2,o.rlOp4Sub3,o.rlOp4Sub4,o.rlOp4Sub5,o.rlOp4Sub6,-1 sign_value
          FROM IpdOrdTbl o JOIN prior_rows c
            ON c.chOp1Date=o.chOp1Date AND c.chOp1Time=o.chOp1Time
           AND c.chOp1Room=o.chOp1Room AND c.intOp1No=o.intOp1No
          WHERE o.chOp4IDate<=c.close_time AND RTRIM(o.chOp4IDate) IS NOT NULL
            AND (o.chOp4DCDate>c.close_time OR RTRIM(o.chOp4DCDate) IS NULL)
            AND (o.chOp4Proj NOT IN ('I','D') OR RTRIM(o.chOp4Proj) IS NULL)
            AND (o.chOp4SPay IN ('0','4') OR NVL(o.rlOp4Sub5,0)<>0)
            AND (SUBSTR(o.chOp4Dct,1,2)<='50' OR SUBSTR(o.chOp4Dct,1,2) IN ('69','75'))
        ), legacy_groups AS (
          SELECT adm_date,adm_time,adm_room,adm_no,mrno,fin,dct,spay,
                 -SUM(CASE WHEN dct='49' THEN -NVL(s2,0) ELSE
                   NVL(s1,0)+NVL(s2,0)+NVL(s3,0)+NVL(s4,0)+NVL(s5,0)+NVL(s6,0) END * sign_value) amount,
                 -SUM(NVL(s6,0)*sign_value) debt_amt,
                 SUM(NVL(s1,0)*sign_value) s1,SUM(NVL(s2,0)*sign_value) s2,
                 SUM(NVL(s3,0)*sign_value) s3,SUM(NVL(s4,0)*sign_value) s4,
                 SUM(NVL(s5,0)*sign_value) s5
          FROM src GROUP BY adm_date,adm_time,adm_room,adm_no,mrno,fin,dct,spay
          HAVING SUM(NVL(s1,0)*sign_value)<>0 OR SUM(NVL(s2,0)*sign_value)<>0
              OR SUM(NVL(s3,0)*sign_value)<>0 OR SUM(NVL(s4,0)*sign_value)<>0
              OR SUM(NVL(s5,0)*sign_value)<>0 OR SUM(NVL(s6,0)*sign_value)<>0
        ), contributions AS (
          SELECT adm_date,adm_time,adm_room,adm_no,mrno,
                 CASE WHEN fin='01' AND dct IN ('01','69') THEN ROUND(amount,0)
                   WHEN fin='01' AND dct='75' AND ROUND(amount,0)=0 THEN -ROUND(debt_amt,0)
                   WHEN fin='01' AND dct='75' AND :SDate>='0970401' THEN -ROUND(amount,0)
                   ELSE 0 END self_amt,
                 CASE WHEN fin='30' AND dct IN ('01','69') THEN ROUND(amount,0)
                   WHEN fin='30' AND dct='75' AND ROUND(amount,0)=0 THEN -ROUND(debt_amt,0)
                   WHEN fin='30' AND dct='75' AND :SDate>='0970401' THEN -ROUND(amount,0)
                   ELSE 0 END claim_amt,
                 CASE WHEN fin='30' AND dct='49' THEN ROUND(amount,0) ELSE 0 END part_amt
          FROM legacy_groups
        )
        SELECT :SDate,'C',adm_date,adm_time,adm_room,adm_no,mrno,
               SUM(self_amt),SUM(claim_amt),SUM(part_amt)
        FROM contributions
        GROUP BY adm_date,adm_time,adm_room,adm_no,mrno
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
