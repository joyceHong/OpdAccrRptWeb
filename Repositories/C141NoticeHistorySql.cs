namespace OpdAccrRptWeb.Repositories;

internal static class C141NoticeHistorySql
{
    internal const string Query = """
        SELECT DISTINCT a.chmrno AS MedicalRecordNumber,
               SUBSTR(a.ipdtdate,1,7) AS NoticeDate,
               a.ipdtnote AS NoticeMethod,
               c.vchAccDate AS PaymentDate,
               a.ipdtbf AS NoticeAmount,
               c.intAccCash1 AS CashAmount,
               c.intAccCash6 AS CreditCardAmount
        FROM IpdTranTbl a
        LEFT JOIN IpdAccCaseDayTbl c
          ON SUBSTR(a.chopdunino,1,7)=c.chop1date
         AND SUBSTR(a.chopdunino,9,6)=c.chop1room
         AND SUBSTR(a.chopdunino,8,1)=c.chop1time
         AND TO_NUMBER(SUBSTR(a.chopdunino,15,4))=c.intop1no
         AND SUBSTR(a.ipdtdate,1,7)<=c.vchAccDate
         AND c.vchaccbid='預繳'
        WHERE a.ipdtdate BETWEEN :startDateTime AND :endDateTime
          AND a.ipdttype='18'
          AND a.chmrno NOT LIKE 'C36979%'
        """;
}
