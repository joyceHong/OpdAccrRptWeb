namespace OpdAccrRptWeb.Repositories;

public static class C9Sql
{
    public const string ResourceId = "C9_SPAY6_ORDER_DETAIL_V1";

    public const string Spay6OrderDetail = """
SELECT B.chOp1Date    AS chOp1Date,
       B.chOp1MrNo    AS chOp1MrNo,
       B.chOp1PName   AS chOp1PName,
       B.chOp1DRName  AS chOp1DRName,
       C.chSecName    AS chSecName,
       A.chOp4CUser   AS chOp4CUser,
       A.chOp4OrdNo   AS chOp4OrdNo,
       A.chOp4OrdName AS chOp4OrdName,
       A.rlOp4Sub5    AS rlOp4Sub5,
       A.rlOp4Sub3    AS rlOp4Sub3
FROM OpdOrdTbl A,
     OpdBasicTbl B,
     GenSectionTbl C
WHERE A.chOp1Date = B.chOp1Date
  AND A.chOp1Time = B.chOp1Time
  AND A.chOp1Room = B.chOp1Room
  AND A.intOp1No = B.intOp1No
  AND B.chOp1Sec = C.chSecNo
  AND A.chOp4SPay = '6'
  AND A.chOp4Stat <> 'DC'
  AND B.chOp1Date = :run_date
  AND (A.chOp4Proj NOT IN ('I', 'S') OR RTRIM(A.chOp4Proj) IS NULL)
""";
}
