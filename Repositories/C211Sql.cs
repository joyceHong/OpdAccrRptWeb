namespace OpdAccrRptWeb.Repositories;

internal static class C211Sql
{
    internal const string ContractChoices = """
        SELECT chDctType AS Code, chDctTypeName AS Name
          FROM GenDctTypeTbl
        """;

    internal const string OutpatientAll = """
        SELECT /*+ INDEX(c, IDX_OPDCONTRACTMRNOTBL_CHSTAT) */
               chFin2 AS ContractCode, chMrNo AS MedicalRecordNo, chDate AS VisitDateRoc,
               chTime AS VisitTime, chRoom AS RoomCode, intNo AS SequenceNo,
               SUM(intSelfAmt) AS SelfAmount, SUM(intClaimAmt) AS ClaimAmount
          FROM OpdContractMrNoTbl c
         WHERE chStat = '0' AND chIDate <= :end_date
         GROUP BY chFin2, chMrNo, chDate, chTime, chRoom, intNo
        HAVING SUM(intSelfAmt) <> 0 OR SUM(intClaimAmt) <> 0
         ORDER BY chFin2, chDate, chTime, chRoom, intNo, chMrNo
        """;

    internal const string OutpatientContract = """
        SELECT /*+ INDEX(c, IDX_OPDCONTRACTMRNOTBL_CHSTAT) */
               chFin2 AS ContractCode, chMrNo AS MedicalRecordNo, chDate AS VisitDateRoc,
               chTime AS VisitTime, chRoom AS RoomCode, intNo AS SequenceNo,
               SUM(intSelfAmt) AS SelfAmount, SUM(intClaimAmt) AS ClaimAmount
          FROM OpdContractMrNoTbl c
         WHERE chStat = '0' AND chIDate <= :end_date AND chFin2 = :contract_code
         GROUP BY chFin2, chMrNo, chDate, chTime, chRoom, intNo
        HAVING SUM(intSelfAmt) <> 0 OR SUM(intClaimAmt) <> 0
         ORDER BY chFin2, chDate, chTime, chRoom, intNo, chMrNo
        """;

    internal const string InpatientAll = """
        SELECT /*+ INDEX(c, IDX_IPDCONTRACTMRNOTBL_CHSTAT) */
               chFin2 AS ContractCode, chMrNo AS MedicalRecordNo, chDate AS VisitDateRoc,
               chTime AS VisitTime, chRoom AS RoomCode, intNo AS SequenceNo,
               SUM(intSelfAmt) AS SelfAmount, SUM(intClaimAmt) AS ClaimAmount
          FROM IpdContractMrNoTbl c
         WHERE chStat = '0' AND chIDate <= :end_date
         GROUP BY chFin2, chMrNo, chDate, chTime, chRoom, intNo
        HAVING SUM(intSelfAmt) <> 0 OR SUM(intClaimAmt) <> 0
         ORDER BY chFin2, chDate, chTime, chRoom, intNo, chMrNo
        """;

    internal const string InpatientContract = """
        SELECT /*+ INDEX(c, IDX_IPDCONTRACTMRNOTBL_CHSTAT) */
               chFin2 AS ContractCode, chMrNo AS MedicalRecordNo, chDate AS VisitDateRoc,
               chTime AS VisitTime, chRoom AS RoomCode, intNo AS SequenceNo,
               SUM(intSelfAmt) AS SelfAmount, SUM(intClaimAmt) AS ClaimAmount
          FROM IpdContractMrNoTbl c
         WHERE chStat = '0' AND chIDate <= :end_date AND chFin2 = :contract_code
         GROUP BY chFin2, chMrNo, chDate, chTime, chRoom, intNo
        HAVING SUM(intSelfAmt) <> 0 OR SUM(intClaimAmt) <> 0
         ORDER BY chFin2, chDate, chTime, chRoom, intNo, chMrNo
        """;

    internal static string Report(string source, bool hasContract) => (source, hasContract) switch
    {
        (C211Sources.Outpatient, false) => OutpatientAll,
        (C211Sources.Outpatient, true) => OutpatientContract,
        (C211Sources.Inpatient, false) => InpatientAll,
        (C211Sources.Inpatient, true) => InpatientContract,
        _ => throw new ArgumentException("C211 資料來源僅接受 O 或 I。", nameof(source))
    };
}
