namespace OpdAccrRptWeb.Repositories;

public static class OpdPriceQuerySql
{
    public const string VisitWhere = """
        FROM OpdBasicTbl B
        WHERE B.chOp1Date = :VisitDate AND B.chOp1MrNo = :MrNo
          AND (:ApplySection = 0 OR B.chOp1Sec = :LegacySection)
        """;
    public const string VisitCount = "SELECT COUNT(*) " + VisitWhere;
    public const string Visits = """
        SELECT * FROM (
          SELECT B.chOp1Date, B.chOp1Time, B.chOp1Room, B.intOp1No, B.chOp1MrNo,
                 B.chOp1Sec, B.chOp1HinSeq, B.chOp1DrName, B.chOp1PName,
                 CASE WHEN EXISTS (SELECT 1 FROM OpdRegPtnTbl R
                    WHERE R.chOp0Date=B.chOp1Date AND R.chOp0Time=B.chOp1Time
                      AND R.chOp0Room=B.chOp1Room AND R.intOp0No=B.intOp1No
                      AND R.chOp0DC='1') THEN 1 ELSE 0 END IsCancelled,
                 ROW_NUMBER() OVER (ORDER BY B.chOp1Date DESC, B.chOp1Time,
                    B.chOp1Room, B.intOp1No) RowNo
        """ + VisitWhere + """
        ) WHERE RowNo > :Offset AND RowNo <= :PageEnd
        ORDER BY RowNo
        """;
    public const string Visit = """
        SELECT B.chOp1Date,B.chOp1Time,B.chOp1Room,B.intOp1No,B.chOp1MrNo,
               B.chOp1Sec,B.chOp1HinSeq,B.chOp1DrName,B.chOp1PName,0 IsCancelled
        FROM OpdBasicTbl B WHERE B.chOp1Date=:VisitDate AND B.chOp1Time=:VisitTime
          AND B.chOp1Room=:Room AND B.intOp1No=:RegistrationNo
        """;
    public const string Patient = """
        SELECT B.chOp1MrNo,B.chOp1PName,B.chOp1Birth,B.chOp1PID
        FROM OpdBasicTbl B WHERE B.chOp1MrNo=:MrNo ORDER BY B.chOp1Date DESC FETCH FIRST 1 ROW ONLY
        """;
    public const string Drugs = """
        SELECT D.chOp3DrgNo,D.chOp3DrgName,D.rlOp3DrgTot,D.intOp3DrgDay,D.chOp3SPay,
               D.rlOp3Pric1,D.rlOp3Pric2,D.rlOp3AMT1,D.rlOp3AMT2,D.chOp3Stat,D.chop3proj,
               D.chOp3InId,D.chOp3AccId,D.chOp3DCId,D.rlOp3Sub1,D.rlOp3Sub2,D.rlOp3Sub3,
               D.rlOp3Sub4,D.rlOp3Sub5,D.rlOp3Sub6,D.chOp2PDate,D.intOp3RecSeq
        FROM OpdDrgTbl D WHERE D.chOp1Date=:VisitDate AND D.chOp1Time=:VisitTime
          AND D.chOp1Room=:Room AND D.intOp1No=:RegistrationNo
          AND ((:ShowDc=0 AND D.chOp3Stat<'A') OR (:ShowDc=1 AND (D.chOp3Stat<'A' OR D.chOp3Stat='DC')))
          AND (:Room<>'0000' OR D.chop3proj NOT IN ('I','S') OR RTRIM(D.chop3proj) IS NULL)
        """;
    public const string Orders = """
        SELECT O.chOp4OrdNo,O.chOp4ExtNo,O.chOp4OrdName,O.rlOp4OrdTot,O.intOp4Pcnt,O.chOp4SPay,
               O.rlOp4Pric1,O.rlOp4Pric2,O.rlOp4AMT1,O.rlOp4AMT2,O.chOp4Stat,O.chop4proj,
               O.chOp4InId,O.chOp4AccId,O.chOp4DCId,O.rlOp4Sub1,O.rlOp4Sub2,O.rlOp4Sub3,
               O.rlOp4Sub4,O.rlOp4Sub5,O.rlOp4Sub6,O.chOp4IDate,O.intOp4RecSeq
        FROM OpdOrdTbl O WHERE O.chOp1Date=:VisitDate AND O.chOp1Time=:VisitTime
          AND O.chOp1Room=:Room AND O.intOp1No=:RegistrationNo
          AND (:ShowDc=1 OR O.chOp4Stat<'A' OR O.chOp4Stat='PR')
          AND (:Room<>'0000' OR O.chop4proj NOT IN ('I','S') OR RTRIM(O.chop4proj) IS NULL)
        """;
    public const string Receipts = """
        SELECT R.chOp2RecNo,R.chOp2AccDate,R.chOp2AccId,R.intOp2AMT13,R.intOp2Acc1,
               R.intOp2Acc2,R.intOp2Acc4,R.intOp2AMT5,R.intOp2Acc3,R.chOp2DCDate,R.chOp2DCId,
               R.chOp2Stat,R.intOp2Seq
        FROM OpdRecTbl R WHERE R.chOp1Date=:VisitDate AND R.chOp1Time=:VisitTime
          AND R.chOp1Room=:Room AND R.intOp1No=:RegistrationNo
          AND (:ShowDc=1 OR LNNVL(R.chOp2Stat='D')) ORDER BY R.intOp2Seq
        """;
    public const string ReceiptHeader = """
        SELECT F.chSeqNo,A.chOp1MrNo,A.chOp1PName,A.chOp1PID,A.chOp1Date,B.chSecName,
               C.chFin1Name,D.chDctTypeName,A.chOp1HinSeq,A.chOp1DrName,E.chDocNo,G.chPPayName
        FROM OpdBasicTbl A JOIN GenSectionTbl B ON A.chOp1Sec=B.chSecNo
        JOIN GenFin1Tbl C ON A.chOp1PFin1=C.chFin1No JOIN GenDctTypeTbl D ON A.chOp1PFin2=D.chDctType
        JOIN GenDoctorTbl E ON A.chOp1DrName=E.chDocName JOIN OpdRecTbl F
          ON A.chOp1Date=F.chOp1Date AND A.chOp1Time=F.chOp1Time AND A.chOp1Room=F.chOp1Room AND A.intOp1No=F.intOp1No
        JOIN OpdPPayTbl G ON A.chOp1PPay=G.chPPayNo
        WHERE A.chOp1Date=:VisitDate AND A.chOp1Time=:VisitTime AND A.chOp1Room=:Room
          AND A.intOp1No=:RegistrationNo AND F.intOp2Seq=:ReceiptSequence
          AND (F.chOp2Stat NOT IN ('D') OR F.chOp2Stat IS NULL)
        """;
    public const string ReceiptCharges = """
        SELECT chDct,SUM(Sub1),SUM(Sub3),SUM(Sub5),SUM(AMT1),SUM(AMT2) FROM (
          SELECT A.chOp3Dct chDct,A.rlOp3Sub1 Sub1,A.rlOp3Sub3 Sub3,A.rlOp3Sub5 Sub5,A.rlOp3AMT1 AMT1,A.rlOp3AMT2 AMT2
          FROM OpdDrgTbl A WHERE A.chOp1Date=:VisitDate AND A.chOp1Time=:VisitTime AND A.chOp1Room=:Room
            AND A.intOp1No=:RegistrationNo AND A.intOp3RecSeq=:ReceiptSequence
            AND (SUBSTR(A.chOp3Dct,1,2)<'51' OR SUBSTR(A.chOp3Dct,1,2)='69')
            AND (A.chOp3Stat NOT IN ('DC') OR A.chOp3Stat IS NULL)
            AND (:Room<>'0000' OR A.chop3proj NOT IN ('I','S') OR RTRIM(A.chop3proj) IS NULL)
          UNION
          SELECT B.chOp4Dct,B.rlOp4Sub1,B.rlOp4Sub3,B.rlOp4Sub5,B.rlOp4AMT1,B.rlOp4AMT2
          FROM OpdOrdTbl B WHERE B.chOp1Date=:VisitDate AND B.chOp1Time=:VisitTime AND B.chOp1Room=:Room
            AND B.intOp1No=:RegistrationNo AND B.intOp4RecSeq=:ReceiptSequence
            AND (SUBSTR(B.chOp4Dct,1,2)<'51' OR SUBSTR(B.chOp4Dct,1,2)='69')
            AND (RTRIM(B.chOp4IDate) IS NOT NULL OR B.chOp4IDate>'')
            AND (B.chOp4Stat NOT IN ('DC') OR B.chOp4Stat IS NULL)
            AND (:Room<>'0000' OR B.chop4proj NOT IN ('I','S') OR RTRIM(B.chop4proj) IS NULL)
        ) GROUP BY chDct ORDER BY chDct
        """;
}
