namespace OpdAccrRptWeb.Repositories;

public static class MedicalRecordQuerySql
{
    public const string CountBase = "SELECT COUNT(*) FROM OpdMRBasicTbl B";

    public const string PageBase = """
        SELECT * FROM (
            SELECT
                B.chMrNo,
                B.chID,
                B.chName,
                B.chBirthday,
                B.chIfMarried,
                B.chSex,
                B.chBlood,
                B.chInsuType,
                B.chDCTType,
                B.chTelH,
                B.chTelO,
                B.chSecretLevel,
                B.chSWKNo AS chPatientCondition,
                ROW_NUMBER() OVER (
                    ORDER BY B.chMrNo, B.chID, B.chName, B.ROWID) AS RowNo
            FROM OpdMRBasicTbl B
        """;

    public const string Detail = """
        SELECT
            B.chMrNo,
            B.chID,
            B.chName,
            B.chBirthday,
            B.chIfMarried,
            B.chSex,
            B.chBlood,
            B.chInsuType,
            B.chDCTType,
            B.chTelH,
            B.chTelO,
            B.chSecretLevel,
            B.chSWKNo AS chPatientCondition,
            B.chZipCode1,
            B.chAdd1,
            B.chZipCode2,
            B.chAdd2,
            B.chNTV,
            B.tiRace,
            B.tiLang,
            B.chLocate,
            B.chNewID,
            B.chNewName,
            B.chNewBirthday,
            B.chMKName,
            B.chOldMRNo,
            B.chSWKNo AS chSocialServiceStatus,
            B.chFirstDate,
            B.rlDebt,
            B.chSpouseID,
            B.chFatherID
        FROM OpdMRBasicTbl B
        WHERE B.chMrNo = :MedicalRecordNo
        """;

    public const string MergedMedicalRecordNumbers = """
        SELECT DISTINCT B.chMrNo
        FROM OpdMrBasicTBL A, OpdMrBasicTBL B
        WHERE A.chMrNo = :MedicalRecordNo
          AND (A.chMerge = B.chMerge OR A.chMrNo = B.chMrNo)
        """;

    public const string DebtTotal = """
        SELECT SUM(D.rlDebtAMT) AS rldebt
        FROM GenDebtTbl D
        WHERE D.chMrNo IN ({0})
          AND RTRIM(D.chBackFlg) IS NULL
        """;
}
