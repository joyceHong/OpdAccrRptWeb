namespace OpdAccrRptWeb.Repositories;

internal static class C212Sql
{
    internal const string OracleNow = "SELECT SYSDATE FROM DUAL";

    internal const string Report = """
        SELECT sort_bucket AS SortBucket,
               chIDate AS AccountingDateRoc,
               chMrNo AS MedicalRecordNo,
               chPName AS PatientName,
               intAmt AS RawOracleAmount
          FROM (
                SELECT 0 AS sort_bucket,
                       ' ' AS chIDate,
                       '' AS chMrNo,
                       '期初餘額' AS chPName,
                       SUM(intAmt) AS intAmt
                  FROM GenCONTRACT42MRNOTBL
                 WHERE chIDate < :month_first_day
                HAVING SUM(intAmt) <> 0
                UNION ALL
                SELECT 1 AS sort_bucket,
                       chIDate,
                       chMrNo,
                       chPName,
                       SUM(intAmt) AS intAmt
                  FROM GenCONTRACT42MRNOTBL
                 WHERE chIDate BETWEEN :month_first_day AND :end_date
                 GROUP BY chIDate, chMrNo, chPName
                HAVING SUM(intAmt) <> 0
               )
         ORDER BY sort_bucket, chIDate, chMrNo, chPName
        """;
}
