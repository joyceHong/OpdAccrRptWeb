namespace OpdAccrRptWeb.Repositories;

public static class C8Sql
{
    public const string PatchBillDetail = """
SELECT DECODE(RTRIM(A.chOp1Room), '0000', 'E', 'R') AS chOp1Room,
       A.chOp1Date AS chOp1Date, A.chOp4PSec AS chOp4PSec,
       B.chOp1MrNo AS chOp1MrNo, A.chOp4PFin1 AS chOp4PFin1,
       A.chOp4OrdNo AS chOp4OrdNo, A.chOp4OrdName AS chOp4OrdName,
       A.rlOp4Pric1 AS rlOp4Pric1, A.rlOp4Pric2 AS rlOp4Pric2,
       A.rlOp4OrdTot AS rlOp4OrdTot, A.rlOp4AMT1 AS rlOp4AMT1,
       A.rlOp4AMT2 AS rlOp4AMT2, A.chOp4CUser AS chOp4CUser
FROM OpdOrdTbl A, OpdBasicTbl B
WHERE A.chOp1Date = B.chOp1Date(+)
  AND A.chOp1Time = B.chOp1Time(+)
  AND A.chOp1Room = B.chOp1Room(+)
  AND A.intOp1No = B.intOp1No(+)
  AND A.chOp4IDate BETWEEN :start_date || '0000' AND :end_date || '9999'
  AND A.chOp4Sys = '9' AND A.chOp4Stat <> 'DC'
  AND (A.rlOp4AMT1 > 0 OR A.rlOp4AMT2 > 0)
  AND (A.chOp4Proj NOT IN ('I', 'S') OR RTRIM(A.chOp4Proj) IS NULL)
UNION ALL
SELECT DECODE(RTRIM(A.chOp1Room), '0000', 'E', 'R') AS chOp1Room,
       A.chOp1Date AS chOp1Date, A.chOp3PSec AS chOp4PSec,
       B.chOp1MrNo AS chOp1MrNo, A.chOp3PFin1 AS chOp4PFin1,
       A.chOp3DrgNo AS chOp4OrdNo, A.chOp3DrgName AS chOp4OrdName,
       A.rlOp3Pric1 AS rlOp4Pric1, A.rlOp3Pric2 AS rlOp4Pric2,
       A.rlOp3DrgTot AS rlOp4OrdTot, A.rlOp3AMT1 AS rlOp4AMT1,
       A.rlOp3AMT2 AS rlOp4AMT2, A.chOp3CUser AS chOp4CUser
FROM OpdDrgTbl A, OpdBasicTbl B
WHERE A.chOp1Date = B.chOp1Date(+)
  AND A.chOp1Time = B.chOp1Time(+)
  AND A.chOp1Room = B.chOp1Room(+)
  AND A.intOp1No = B.intOp1No(+)
  AND A.chOp3IDate BETWEEN :start_date || '0000' AND :end_date || '9999'
  AND A.chOp3Sys = '9' AND A.chOp3Stat <> 'DC'
  AND (A.rlOp3AMT1 > 0 OR A.rlOp3AMT2 > 0)
  AND (A.chOp3Proj NOT IN ('I', 'S') OR RTRIM(A.chOp3Proj) IS NULL)
""";

    public const string SectionLookup = """
SELECT S.chNewSecNo
FROM GenSectionTbl S
WHERE S.chSecNo = :legacy_section_code
""";
}
