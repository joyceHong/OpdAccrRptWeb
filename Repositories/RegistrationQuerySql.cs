namespace OpdAccrRptWeb.Repositories;

public static class RegistrationQuerySql
{
    public const string CountBase = "SELECT COUNT(*) FROM OpdRegPtnTbl R";

    public const string PageBase = """
        SELECT * FROM (
            SELECT R.*,
                   ROW_NUMBER() OVER (
                       ORDER BY R.chOp0Date DESC,
                                R.chOp0Time DESC,
                                R.chOp0Room DESC,
                                R.intOp0No DESC) AS RegistrationRowNo
            FROM OpdRegPtnTbl R
        """;

    public const string MergedMedicalRecordNumbers = """
        SELECT DISTINCT B.chMrNo
        FROM OpdMrBasicTbl A, OpdMrBasicTbl B
        WHERE A.chMrNo = :MedicalRecordNo
          AND (A.chMerge = B.chMerge OR A.chMrNo = B.chMrNo)
        """;

    public const string SummaryTotal = """
        SELECT COUNT(*) AS RegCount
        FROM OpdRegPtnTbl
        WHERE chOp0Date = :RegDate
          AND chOp0Time = :RegTime
          AND chOp0Room = :RegRoom
        """;

    public const string SummarySeen = """
        SELECT COUNT(*) AS RegCount
        FROM OpdRegPtnTbl
        WHERE chOp0Date = :RegDate
          AND chOp0Time = :RegTime
          AND chOp0Room = :RegRoom
          AND chOp0DigStat = '3'
        """;

    public const string SummaryUnseen = """
        SELECT COUNT(*) AS RegCount
        FROM OpdRegPtnTbl
        WHERE chOp0Date = :RegDate
          AND chOp0Time = :RegTime
          AND chOp0Room = :RegRoom
          AND chOp0DC = '0'
          AND chOp0DigStat = '0'
        """;

    public const string SummaryCancelled = """
        SELECT *
        FROM OpdRegPtnTbl
        WHERE chOp0Date = :RegDate
          AND chOp0Time = :RegTime
          AND chOp0Room = :RegRoom
          AND chOp0DC = '1'
        """;

    public const string SummaryRoomNumbers = """
        SELECT intRegCurNextNo, intRegPreNextNo
        FROM OpdRegRoomTbl
        WHERE chRegDate = :RegDate
          AND chRegTime = :RegTime
          AND chRegRoom = :RegRoom
        """;

    public const string DoctorOptions = """
        SELECT RTRIM(chDocNo) AS chDocNo,
               RTRIM(chDocName) AS chDocName
        FROM GenDoctorTbl
        WHERE (UPPER(RTRIM(chDocNo)) LIKE :Query ESCAPE '\'
            OR UPPER(RTRIM(chDocName)) LIKE :Query ESCAPE '\')
        ORDER BY RTRIM(chDocNo), RTRIM(chDocName)
        FETCH FIRST :ResultLimit ROWS ONLY
        """;
}
