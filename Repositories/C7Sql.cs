namespace OpdAccrRptWeb.Repositories;

public static class C7Sql
{
    public const string UserLookup = """
SELECT U.chUserID, U.chUserName FROM GenUserProfile1 U
WHERE U.chUserSector IN ('0643', '0644') AND U.chEndDate > :end_date
""";

    public const string DrugDetail = """
SELECT D.chOp3PFin1 AS chOp4PFin1, D.chOp3PSec AS chOp4PSec,
 D.chOp3DrgNo AS chOp4OrdNo, D.chOp3DrgName AS chOp4OrdName, D.chOp3SPay AS chOp4SPay,
 D.chOp3Stat AS chOp4Stat, D.rlOp3DrgTot AS rlOp4OrdTot, D.chOp3CUser AS chOp4CUser,
 D.rlOp3Pric1 AS rlOp4Pric1, D.rlOp3Pric2 AS rlOp4Pric2, D.rlOp3AMT1 AS rlOp4AMT1,
 D.rlOp3AMT2 AS rlOp4AMT2, U.chUserName AS chUserName, B.chOp1Date AS chOp1Date,
 B.chOp1Time AS chOp1Time, B.chOp1MrNo AS chOp1MrNo, B.chOp1RoomType AS chOp1RoomType
FROM OpdDrgTbl D
JOIN OpdBasicTbl B ON D.chOp1Date=B.chOp1Date AND D.chOp1Time=B.chOp1Time
 AND D.chOp1Room=B.chOp1Room AND D.intOp1No=B.intOp1No
JOIN GenUserProfile1 U ON D.chOp3CUser=U.chUserID
WHERE D.chOp3CDate BETWEEN :run_date || :start_time AND :run_date || :end_time
 AND D.chOp1Date=:run_date AND (:input_user_id IS NULL OR D.chOp3CUser=:input_user_id)
 AND B.chOp1MrNo NOT IN ('C36979','1000000')
 AND (D.chOp3Proj NOT IN ('I','S') OR RTRIM(D.chOp3Proj) IS NULL)
""";

    public const string OrderDetail = """
SELECT D.chOp4PFin1 AS chOp4PFin1, D.chOp4PSec AS chOp4PSec,
 D.chOp4OrdNo AS chOp4OrdNo, D.chOp4OrdName AS chOp4OrdName, D.chOp4SPay AS chOp4SPay,
 D.chOp4Stat AS chOp4Stat, D.rlOp4OrdTot AS rlOp4OrdTot, D.chOp4CUser AS chOp4CUser,
 D.rlOp4Pric1 AS rlOp4Pric1, D.rlOp4Pric2 AS rlOp4Pric2, D.rlOp4AMT1 AS rlOp4AMT1,
 D.rlOp4AMT2 AS rlOp4AMT2, U.chUserName AS chUserName, D.chOp1Date AS chOp1Date,
 D.chOp1Time AS chOp1Time, B.chOp1MrNo AS chOp1MrNo, B.chOp1RoomType AS chOp1RoomType
FROM OpdOrdTbl D
JOIN OpdBasicTbl B ON D.chOp1Date=B.chOp1Date AND D.chOp1Time=B.chOp1Time
 AND D.chOp1Room=B.chOp1Room AND D.intOp1No=B.intOp1No
JOIN GenUserProfile1 U ON D.chOp4CUser=U.chUserID
WHERE D.chOp4CDate BETWEEN :run_date || :start_time AND :run_date || :end_time
 AND D.chOp1Date=:run_date AND (:input_user_id IS NULL OR D.chOp4CUser=:input_user_id)
 AND B.chOp1MrNo NOT IN ('C36979','1000000')
 AND (D.chOp4Proj NOT IN ('I','S') OR RTRIM(D.chOp4Proj) IS NULL)
""";
}
