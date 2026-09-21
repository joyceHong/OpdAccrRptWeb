namespace OpdAccrRptWeb.Repositories;

public enum C5QueryId
{
    IpdDrugAggregate, IpdDrugDetail, IpdOrderAggregate, IpdOrderDetail,
    OpdDrugAggregate, OpdDrugDetail, OpdOrderAggregate, OpdOrderDetail,
    OpdOrder0430Aggregate, OpdOrder0430Detail
}

public static class C5Sql
{
    public const string IpdDrugAggregate = """
SELECT /*+ rules */
       SUM(A.rlOp3AMT1) AS AMT1,
       SUM(A.rlOp3AMT2) AS AMT2,
       SUM(A.rlOp3DrgTot) AS Tot,
       SUBSTR(A.chOp3CDate, 1, 7) AS chOp1Date,
       B.chOp1RoomType AS chOp1RoomType,
       A.chOp3PSec AS PSec,
       A.chOp3DrgNo AS DrgNo,
       A.chOp3DrgName AS DrgName,
       A.chOp3SPay AS SPay,
       A.rlOp3Pric1 AS Pric1,
       A.rlOp3Pric2 AS Pric2
FROM IpdDrgTbl A
JOIN IpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No = B.intOp1No
WHERE A.chOp3CDate LIKE :run_date || '%'
  AND (A.chOp3Proj NOT IN ('I', 'D', 'S') OR RTRIM(A.chOp3Proj) IS NULL)
  AND (A.chOp3Rep3Flg <> 'S' OR RTRIM(A.chOp3Rep3Flg) IS NULL)
  AND (A.chOp3Stat NOT IN ('09', '10', '11', '12') OR RTRIM(A.chOp3Stat) IS NULL)
  AND A.chOp3Stat <> 'DC'
  AND A.chOp3Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
  AND (:room_no IS NULL OR B.chOp1Room = :room_no)
  AND (:section_code IS NULL OR A.chOp3PSec = :section_code)
  AND (:charge_code IS NULL OR A.chOp3DrgNo = :charge_code)
  AND (:insurance_identity_code IS NULL OR A.chOp3PFin1 = :insurance_identity_code)
GROUP BY SUBSTR(A.chOp3CDate, 1, 7),
         B.chOp1RoomType,
         A.chOp3PSec,
         A.chOp3DrgNo,
         A.chOp3DrgName,
         A.chOp3SPay,
         A.rlOp3Pric1,
         A.rlOp3Pric2
""";

    public const string IpdDrugDetail = """
SELECT /*+ rules */
       SUM(A.rlOp3AMT1) AS AMT1,
       SUM(A.rlOp3AMT2) AS AMT2,
       SUM(A.rlOp3DrgTot) AS Tot,
       SUBSTR(A.chOp3CDate, 1, 7) AS chOp1Date,
       B.chOp1RoomType AS chOp1RoomType,
       A.chOp3PSec AS PSec,
       B.chOp1DrID AS chOp1DrID,
       B.chOp1DrName AS chOp1DrName,
       B.chOp1MrNo AS chOp1MrNo,
       B.chOp1PName AS chOp1PName,
       A.chOp3DrgNo AS DrgNo,
       A.chOp3DrgName AS DrgName,
       A.chOp3SPay AS SPay,
       A.rlOp3Pric1 AS Pric1,
       A.rlOp3Pric2 AS Pric2
FROM IpdDrgTbl A
JOIN IpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No = B.intOp1No
WHERE A.chOp3CDate LIKE :run_date || '%'
  AND (A.chOp3Proj NOT IN ('I', 'D', 'S') OR RTRIM(A.chOp3Proj) IS NULL)
  AND (A.chOp3Rep3Flg <> 'S' OR RTRIM(A.chOp3Rep3Flg) IS NULL)
  AND (A.chOp3Stat NOT IN ('09', '10', '11', '12') OR RTRIM(A.chOp3Stat) IS NULL)
  AND A.chOp3Stat <> 'DC'
  AND A.chOp3Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
  AND (:room_no IS NULL OR B.chOp1Room = :room_no)
  AND (:section_code IS NULL OR A.chOp3PSec = :section_code)
  AND (:charge_code IS NULL OR A.chOp3DrgNo = :charge_code)
  AND (:insurance_identity_code IS NULL OR A.chOp3PFin1 = :insurance_identity_code)
GROUP BY SUBSTR(A.chOp3CDate, 1, 7),
         B.chOp1RoomType,
         A.chOp3PSec,
         B.chOp1DrID,
         B.chOp1DrName,
         B.chOp1MrNo,
         B.chOp1PName,
         A.chOp3DrgNo,
         A.chOp3DrgName,
         A.chOp3SPay,
         A.rlOp3Pric1,
         A.rlOp3Pric2
""";

    public const string IpdOrderAggregate = """
SELECT /*+ rules */
       SUM(A.rlOp4AMT1) AS AMT1,
       SUM(A.rlOp4AMT2) AS AMT2,
       SUM(A.rlOp4OrdTot) AS Tot,
       SUBSTR(A.chOp4CDate, 1, 7) AS chOp1Date,
       B.chOp1RoomType AS chOp1RoomType,
       A.chOp4PSec AS PSec,
       A.chOp4ExtNo AS DrgNo,
       A.chOp4OrdName AS DrgName,
       A.chOp4SPay AS SPay,
       A.rlOp4Pric1 AS Pric1,
       A.rlOp4Pric2 AS Pric2
FROM IpdOrdTbl A
JOIN IpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No = B.intOp1No
WHERE A.chOp4CDate LIKE :run_date || '%'
  AND (A.chOp4Proj NOT IN ('I', 'D', 'S') OR RTRIM(A.chOp4Proj) IS NULL)
  AND A.chOp4Stat <> 'DC'
  AND A.chOp4Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
  AND (:room_no IS NULL OR B.chOp1Room = :room_no)
  AND (:section_code IS NULL OR A.chStation = :section_code)
  AND (:charge_code IS NULL OR A.chOp4OrdNo = :charge_code)
  AND (:insurance_identity_code IS NULL OR A.chOp4PFin1 = :insurance_identity_code)
GROUP BY SUBSTR(A.chOp4CDate, 1, 7),
         B.chOp1RoomType,
         A.chOp4PSec,
         A.chOp4ExtNo,
         A.chOp4OrdName,
         A.chOp4SPay,
         A.rlOp4Pric1,
         A.rlOp4Pric2
""";

    public const string IpdOrderDetail = """
SELECT /*+ rules */
       SUM(A.rlOp4AMT1) AS AMT1,
       SUM(A.rlOp4AMT2) AS AMT2,
       SUM(A.rlOp4OrdTot) AS Tot,
       SUBSTR(A.chOp4CDate, 1, 7) AS chOp1Date,
       B.chOp1RoomType AS chOp1RoomType,
       A.chOp4PSec AS PSec,
       B.chOp1DrID AS chOp1DrID,
       B.chOp1DrName AS chOp1DrName,
       B.chOp1MrNo AS chOp1MrNo,
       B.chOp1PName AS chOp1PName,
       A.chOp4ExtNo AS DrgNo,
       A.chOp4OrdName AS DrgName,
       A.chOp4SPay AS SPay,
       A.rlOp4Pric1 AS Pric1,
       A.rlOp4Pric2 AS Pric2
FROM IpdOrdTbl A
JOIN IpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No = B.intOp1No
WHERE A.chOp4CDate LIKE :run_date || '%'
  AND (A.chOp4Proj NOT IN ('I', 'D', 'S') OR RTRIM(A.chOp4Proj) IS NULL)
  AND A.chOp4Stat <> 'DC'
  AND A.chOp4Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
  AND (:room_no IS NULL OR B.chOp1Room = :room_no)
  AND (:section_code IS NULL OR A.chStation = :section_code)
  AND (:charge_code IS NULL OR A.chOp4OrdNo = :charge_code)
  AND (:insurance_identity_code IS NULL OR A.chOp4PFin1 = :insurance_identity_code)
GROUP BY SUBSTR(A.chOp4CDate, 1, 7),
         B.chOp1RoomType,
         A.chOp4PSec,
         B.chOp1DrID,
         B.chOp1DrName,
         B.chOp1MrNo,
         B.chOp1PName,
         A.chOp4ExtNo,
         A.chOp4OrdName,
         A.chOp4SPay,
         A.rlOp4Pric1,
         A.rlOp4Pric2
""";

    public const string OpdDrugAggregate = """
SELECT /*+ rules */
       SUM(A.rlOp3AMT1) AS AMT1,
       SUM(A.rlOp3AMT2) AS AMT2,
       SUM(A.rlOp3DrgTot) AS Tot,
       B.chOp1Date AS chOp1Date,
       B.chOp1RoomType AS chOp1RoomType,
       A.chOp3PSec AS PSec,
       A.chOp3DrgNo AS DrgNo,
       A.chOp3DrgName AS DrgName,
       A.chOp3SPay AS SPay,
       A.rlOp3Pric1 AS Pric1,
       A.rlOp3Pric2 AS Pric2
FROM OpdDrgTbl A
JOIN OpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No = B.intOp1No
WHERE B.chOp1Date = :run_date
  AND (
        :encounter_type = 'A'
        OR (:encounter_type = 'E' AND B.chOp1Room = '0000')
        OR (:encounter_type = 'R' AND B.chOp1Room <> '0000')
      )
  AND (A.chOp3Proj NOT IN ('I', 'D', 'S') OR RTRIM(A.chOp3Proj) IS NULL)
  AND (A.chOp3Rep3Flg <> 'S' OR RTRIM(A.chOp3Rep3Flg) IS NULL)
  AND (A.chOp3Stat NOT IN ('09', '10', '11', '12') OR RTRIM(A.chOp3Stat) IS NULL)
  AND A.chOp3Stat <> 'DC'
  AND A.chOp3Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
  AND (:room_no IS NULL OR B.chOp1Room = :room_no)
  AND (:section_code IS NULL OR A.chOp3PSec = :section_code)
  AND (:charge_code IS NULL OR A.chOp3DrgNo = :charge_code)
  AND (:insurance_identity_code IS NULL OR A.chOp3PFin1 = :insurance_identity_code)
GROUP BY B.chOp1Date,
         B.chOp1RoomType,
         A.chOp3PSec,
         A.chOp3DrgNo,
         A.chOp3DrgName,
         A.chOp3SPay,
         A.rlOp3Pric1,
         A.rlOp3Pric2
""";

    public const string OpdDrugDetail = """
SELECT /*+ rules */
       SUM(A.rlOp3AMT1) AS AMT1,
       SUM(A.rlOp3AMT2) AS AMT2,
       SUM(A.rlOp3DrgTot) AS Tot,
       B.chOp1Date AS chOp1Date,
       B.chOp1RoomType AS chOp1RoomType,
       A.chOp3PSec AS PSec,
       B.chOp1DrID AS chOp1DrID,
       B.chOp1DrName AS chOp1DrName,
       B.chOp1MrNo AS chOp1MrNo,
       B.chOp1PName AS chOp1PName,
       A.chOp3DrgNo AS DrgNo,
       A.chOp3DrgName AS DrgName,
       A.chOp3SPay AS SPay,
       A.rlOp3Pric1 AS Pric1,
       A.rlOp3Pric2 AS Pric2
FROM OpdDrgTbl A
JOIN OpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No = B.intOp1No
WHERE B.chOp1Date = :run_date
  AND (
        :encounter_type = 'A'
        OR (:encounter_type = 'E' AND B.chOp1Room = '0000')
        OR (:encounter_type = 'R' AND B.chOp1Room <> '0000')
      )
  AND (A.chOp3Proj NOT IN ('I', 'D', 'S') OR RTRIM(A.chOp3Proj) IS NULL)
  AND (A.chOp3Rep3Flg <> 'S' OR RTRIM(A.chOp3Rep3Flg) IS NULL)
  AND (A.chOp3Stat NOT IN ('09', '10', '11', '12') OR RTRIM(A.chOp3Stat) IS NULL)
  AND A.chOp3Stat <> 'DC'
  AND A.chOp3Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
  AND (:room_no IS NULL OR B.chOp1Room = :room_no)
  AND (:section_code IS NULL OR A.chOp3PSec = :section_code)
  AND (:charge_code IS NULL OR A.chOp3DrgNo = :charge_code)
  AND (:insurance_identity_code IS NULL OR A.chOp3PFin1 = :insurance_identity_code)
GROUP BY B.chOp1Date,
         B.chOp1RoomType,
         A.chOp3PSec,
         B.chOp1DrID,
         B.chOp1DrName,
         B.chOp1MrNo,
         B.chOp1PName,
         A.chOp3DrgNo,
         A.chOp3DrgName,
         A.chOp3SPay,
         A.rlOp3Pric1,
         A.rlOp3Pric2
""";

    public const string OpdOrder0430Aggregate = """
SELECT /*+ rules */
       SUM(A.rlOp4AMT1) AS AMT1,
       SUM(A.rlOp4AMT2) AS AMT2,
       SUM(A.rlOp4OrdTot) AS Tot,
       SUM(A.rlOp4Sub3) AS Sub3,
       B.chOp1Date AS chOp1Date,
       B.chOp1RoomType AS chOp1RoomType,
       A.chOp4PSec AS PSec,
       A.chOp4ExtNo AS DrgNo,
       A.chOp4OrdName AS DrgName,
       A.chOp4SPay AS SPay,
       A.rlOp4Pric1 AS Pric1,
       A.rlOp4Pric2 AS Pric2
FROM OpdOrdTbl A
JOIN OpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No = B.intOp1No
WHERE B.chOp1Date = :run_date
  AND (
        :encounter_type = 'A'
        OR (:encounter_type = 'E' AND B.chOp1Room = '0000')
        OR (:encounter_type = 'R' AND B.chOp1Room <> '0000')
      )
  AND (A.chOp4Proj NOT IN ('I', 'D', 'S') OR RTRIM(A.chOp4Proj) IS NULL)
  AND A.chOp4Stat <> 'DC'
  AND A.chOp4Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
  AND (:room_no IS NULL OR B.chOp1Room = :room_no)
  AND (
        A.chOp4PSec = '0430'
        OR A.chOp4OrdNo IN (
             '43-030', 'F00001-1', 'F00001-2', 'F00001-3',
             '43-063', '43-032', '43-033'
           )
      )
  AND (:charge_code IS NULL OR A.chOp4OrdNo = :charge_code)
  AND (:insurance_identity_code IS NULL OR A.chOp4PFin1 = :insurance_identity_code)
GROUP BY B.chOp1Date,
         B.chOp1RoomType,
         A.chOp4PSec,
         A.chOp4ExtNo,
         A.chOp4OrdName,
         A.chOp4SPay,
         A.rlOp4Pric1,
         A.rlOp4Pric2
""";

    public const string OpdOrder0430Detail = """
SELECT /*+ rules */
       SUM(A.rlOp4AMT1) AS AMT1,
       SUM(A.rlOp4AMT2) AS AMT2,
       SUM(A.rlOp4OrdTot) AS Tot,
       SUM(A.rlOp4Sub3) AS Sub3,
       B.chOp1Date AS chOp1Date,
       B.chOp1RoomType AS chOp1RoomType,
       A.chOp4PSec AS PSec,
       B.chOp1DrID AS chOp1DrID,
       B.chOp1DrName AS chOp1DrName,
       B.chOp1MrNo AS chOp1MrNo,
       B.chOp1PName AS chOp1PName,
       A.chOp4ExtNo AS DrgNo,
       A.chOp4OrdName AS DrgName,
       A.chOp4SPay AS SPay,
       A.rlOp4Pric1 AS Pric1,
       A.rlOp4Pric2 AS Pric2
FROM OpdOrdTbl A
JOIN OpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No = B.intOp1No
WHERE B.chOp1Date = :run_date
  AND (
        :encounter_type = 'A'
        OR (:encounter_type = 'E' AND B.chOp1Room = '0000')
        OR (:encounter_type = 'R' AND B.chOp1Room <> '0000')
      )
  AND (A.chOp4Proj NOT IN ('I', 'D', 'S') OR RTRIM(A.chOp4Proj) IS NULL)
  AND A.chOp4Stat <> 'DC'
  AND A.chOp4Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
  AND (:room_no IS NULL OR B.chOp1Room = :room_no)
  AND (
        A.chOp4PSec = '0430'
        OR A.chOp4OrdNo IN (
             '43-030', 'F00001-1', 'F00001-2', 'F00001-3',
             '43-063', '43-032', '43-033'
           )
      )
  AND (:charge_code IS NULL OR A.chOp4OrdNo = :charge_code)
  AND (:insurance_identity_code IS NULL OR A.chOp4PFin1 = :insurance_identity_code)
GROUP BY B.chOp1Date,
         B.chOp1RoomType,
         A.chOp4PSec,
         B.chOp1DrID,
         B.chOp1DrName,
         B.chOp1MrNo,
         B.chOp1PName,
         A.chOp4ExtNo,
         A.chOp4OrdName,
         A.chOp4SPay,
         A.rlOp4Pric1,
         A.rlOp4Pric2
""";

    public const string OpdOrderAggregate = """
SELECT /*+ rules */
       SUM(A.rlOp4AMT1) AS AMT1,
       SUM(A.rlOp4AMT2) AS AMT2,
       SUM(A.rlOp4OrdTot) AS Tot,
       B.chOp1Date AS chOp1Date,
       B.chOp1RoomType AS chOp1RoomType,
       A.chOp4PSec AS PSec,
       A.chOp4ExtNo AS DrgNo,
       A.chOp4OrdName AS DrgName,
       A.chOp4SPay AS SPay,
       A.rlOp4Pric1 AS Pric1,
       A.rlOp4Pric2 AS Pric2
FROM OpdOrdTbl A
JOIN OpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No = B.intOp1No
WHERE B.chOp1Date = :run_date
  AND (
        :encounter_type = 'A'
        OR (:encounter_type = 'E' AND B.chOp1Room = '0000')
        OR (:encounter_type = 'R' AND B.chOp1Room <> '0000')
      )
  AND (A.chOp4Proj NOT IN ('I', 'D', 'S') OR RTRIM(A.chOp4Proj) IS NULL)
  AND A.chOp4Stat <> 'DC'
  AND A.chOp4Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
  AND (:room_no IS NULL OR B.chOp1Room = :room_no)
  AND (
        :section_code IS NULL
        OR A.chOp4PSec = :section_code
        OR (
             :section_code = '0532'
             AND (
                  (A.chOp1Room = '0000' AND A.chOp4Sys = '5')
                  OR (
                       (A.chOp1Room = '3F1' OR A.chOp1Room LIKE 'OP_%')
                       AND A.chOp4Sys <> '7'
                     )
                 )
           )
      )
  AND (:charge_code IS NULL OR A.chOp4OrdNo = :charge_code)
  AND (:insurance_identity_code IS NULL OR A.chOp4PFin1 = :insurance_identity_code)
GROUP BY B.chOp1Date,
         B.chOp1RoomType,
         A.chOp4PSec,
         A.chOp4ExtNo,
         A.chOp4OrdName,
         A.chOp4SPay,
         A.rlOp4Pric1,
         A.rlOp4Pric2
""";

    public const string OpdOrderDetail = """
SELECT /*+ rules */
       SUM(A.rlOp4AMT1) AS AMT1,
       SUM(A.rlOp4AMT2) AS AMT2,
       SUM(A.rlOp4OrdTot) AS Tot,
       B.chOp1Date AS chOp1Date,
       B.chOp1RoomType AS chOp1RoomType,
       A.chOp4PSec AS PSec,
       B.chOp1DrID AS chOp1DrID,
       B.chOp1DrName AS chOp1DrName,
       B.chOp1MrNo AS chOp1MrNo,
       B.chOp1PName AS chOp1PName,
       A.chOp4ExtNo AS DrgNo,
       A.chOp4OrdName AS DrgName,
       A.chOp4SPay AS SPay,
       A.rlOp4Pric1 AS Pric1,
       A.rlOp4Pric2 AS Pric2
FROM OpdOrdTbl A
JOIN OpdBasicTbl B
  ON A.chOp1Date = B.chOp1Date
 AND A.chOp1Time = B.chOp1Time
 AND A.chOp1Room = B.chOp1Room
 AND A.intOp1No = B.intOp1No
WHERE B.chOp1Date = :run_date
  AND (
        :encounter_type = 'A'
        OR (:encounter_type = 'E' AND B.chOp1Room = '0000')
        OR (:encounter_type = 'R' AND B.chOp1Room <> '0000')
      )
  AND (A.chOp4Proj NOT IN ('I', 'D', 'S') OR RTRIM(A.chOp4Proj) IS NULL)
  AND A.chOp4Stat <> 'DC'
  AND A.chOp4Dct NOT IN ('25', '69')
  AND B.chOp1MrNo NOT IN ('C36979', '1000000')
  AND (:room_no IS NULL OR B.chOp1Room = :room_no)
  AND (
        :section_code IS NULL
        OR A.chOp4PSec = :section_code
        OR (
             :section_code = '0532'
             AND (
                  (A.chOp1Room = '0000' AND A.chOp4Sys = '5')
                  OR (
                       (A.chOp1Room = '3F1' OR A.chOp1Room LIKE 'OP_%')
                       AND A.chOp4Sys <> '7'
                     )
                 )
           )
      )
  AND (:charge_code IS NULL OR A.chOp4OrdNo = :charge_code)
  AND (:insurance_identity_code IS NULL OR A.chOp4PFin1 = :insurance_identity_code)
GROUP BY B.chOp1Date,
         B.chOp1RoomType,
         A.chOp4PSec,
         B.chOp1DrID,
         B.chOp1DrName,
         B.chOp1MrNo,
         B.chOp1PName,
         A.chOp4ExtNo,
         A.chOp4OrdName,
         A.chOp4SPay,
         A.rlOp4Pric1,
         A.rlOp4Pric2
""";

    public static string Get(C5QueryId id) => id switch
    {
        C5QueryId.IpdDrugAggregate => IpdDrugAggregate,
        C5QueryId.IpdDrugDetail => IpdDrugDetail,
        C5QueryId.IpdOrderAggregate => IpdOrderAggregate,
        C5QueryId.IpdOrderDetail => IpdOrderDetail,
        C5QueryId.OpdDrugAggregate => OpdDrugAggregate,
        C5QueryId.OpdDrugDetail => OpdDrugDetail,
        C5QueryId.OpdOrderAggregate => OpdOrderAggregate,
        C5QueryId.OpdOrderDetail => OpdOrderDetail,
        C5QueryId.OpdOrder0430Aggregate => OpdOrder0430Aggregate,
        C5QueryId.OpdOrder0430Detail => OpdOrder0430Detail,
        _ => throw new ArgumentOutOfRangeException(nameof(id))
    };
}

