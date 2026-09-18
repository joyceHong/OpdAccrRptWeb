SELECT
    DECODE(RTRIM(O.chOp1Room), '0000', 'E', 'R') AS RoomType,
    G.chOrdDetal,
    G.chOrdInv,
    G.chOrdHisACode,
    G.chOrdCName,
    G.chOrdUnit,
    O.chOp4OrdNo,
    O.chOp4PSec,
    SUM(O.rlOp4OrdTot) AS Tot
FROM GenOrdBasicTbl G,
     OpdOrdTbl O
WHERE O.chOp4IDate BETWEEN :run_date || '0000' AND :run_date || '9999'
  AND O.chOp4Stat <> 'DC'
  AND O.chOp4OrdNo = G.chOrdNo
  AND RTRIM(G.chOrdDetal) IS NOT NULL
  AND O.chOp4HinCls IN ('50', '99')
  AND (:section_prefix IS NULL OR O.chOp4PSec LIKE :section_prefix || '%')
GROUP BY
    DECODE(RTRIM(O.chOp1Room), '0000', 'E', 'R'),
    G.chOrdDetal,
    G.chOrdInv,
    G.chOrdHisACode,
    G.chOrdCName,
    G.chOrdUnit,
    O.chOp4OrdNo,
    O.chOp4PSec
ORDER BY
    O.chOp4PSec,
    G.chOrdInv
