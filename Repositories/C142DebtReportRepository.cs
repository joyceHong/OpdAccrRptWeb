using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public interface IC142DebtReportRepository
{
    (List<C142DebtRow> Rows, List<C142RowError> Errors) Query(
        string startDate, string endDate, string source, string sectionCode, CancellationToken cancellationToken);
    (string? Phone, string? Address) GetContact(string? medicalRecordNumber, CancellationToken cancellationToken);
    string? GetLargeOrder(C142DebtRow row, CancellationToken cancellationToken);
    decimal? GetOrderTotal(C142DebtRow row, CancellationToken cancellationToken);
    IReadOnlyList<C142Transaction> GetTransactions(string source, C142DebtRow row, CancellationToken cancellationToken);
}

public sealed record C142Transaction(string? Type, string? DateTime, string? User, string? Note);

public sealed class C142DebtReportRepository(IConnectionStringProvider connectionStringProvider) : IC142DebtReportRepository
{
    private const string OutpatientSql = """
        SELECT A.chOp1Date, A.chOp1Time, A.chOp1Room, A.intOp1No,
               SUM(A.rlDebtAMT) AS DebtAMT, B.CHOP1INSEQ
        FROM GenDEBTTBL A JOIN OPDBASICTBL B
          ON A.chOp1Date=B.chOp1Date AND A.chOp1Time=B.chOp1Time
         AND A.chOp1Room=B.chOp1Room AND A.intOp1No=B.intOp1No
        WHERE A.chOp1Date BETWEEN :bDate AND :eDate AND A.chOp1Room <> '0000'
          AND (:applySec=0 OR A.chDeptNo=:secNo)
          AND (chBackFlg NOT IN ('D','R','T') OR chBackFlg IS NULL)
          AND rlDebtAMT > 0
        GROUP BY A.chOp1Date,A.chOp1Time,A.chOp1Room,A.intOp1No,B.CHOP1INSEQ
        ORDER BY A.chOp1Date
        """;
    private const string EmergencySql = """
        SELECT A.chOp1Date, A.chOp1Time, A.chOp1Room, A.intOp1No,
               SUM(A.rlDebtAMT) AS DebtAMT, B.CHOP1INSEQ
        FROM GenDEBTTBL A JOIN OPDBASICTBL B
          ON A.chOp1Date=B.chOp1Date AND A.chOp1Time=B.chOp1Time
         AND A.chOp1Room=B.chOp1Room AND A.intOp1No=B.intOp1No
        WHERE A.chOp1Date BETWEEN :bDate AND :eDate AND A.chOp1Room = '0000'
          AND (:applySec=0 OR A.chDeptNo=:secNo)
          AND (chBackFlg NOT IN ('D','R','T') OR chBackFlg IS NULL)
          AND rlDebtAMT > 0
        GROUP BY A.chOp1Date,A.chOp1Time,A.chOp1Room,A.intOp1No,B.CHOP1INSEQ
        ORDER BY A.chOp1Date
        """;
    private const string InpatientSql = """
        SELECT * FROM (
          SELECT D.chOp1Date,D.chOp1Time,D.chOp1Room,D.intOp1No,
                 SUM(D.rlDebtAMT) AS DebtAMT,B.CHOP1INSEQ
          FROM IPDDEBTTBL D, IPDBasicTbl B
          WHERE D.chOp1Date=B.chOp1Date AND D.chOp1Time=B.chOp1Time
            AND D.chOp1Room=B.chOp1Room AND D.intOp1No=B.intOp1No
            AND (:applySec=0 OR D.vchDeptNo=:secNo)
            AND (D.vchBackFlg NOT IN ('D','R','T') OR D.vchBackFlg IS NULL)
            AND SUBSTR(B.DoctAllowDate,1,7) BETWEEN :bDate AND :eDate
          GROUP BY D.chOp1Date,D.chOp1Time,D.chOp1Room,D.intOp1No,B.CHOP1INSEQ
        ) D2 WHERE DebtAMT > 0 ORDER BY D2.chOp1Date
        """;
    private const string OpdDetailSql = """
        SELECT chOp1mrno,chOp1Pname,'' AS EoutDate,chOp1Date,
               SS.AMT2,SS.AMT4,SS.AMT8,chop1sec,chop1drname
        FROM OpdBasicTbl,
          (SELECT intOp2Amt2 AS AMT2,intOp2AMT4 AS AMT4,intOp2AMT8 AS AMT8
           FROM OPDRECTBL WHERE chOp1Date=:opDate AND chOp1Time=:opTime
             AND chOp1Room=:opRoom AND intOp1No=:opNo
             AND intOp2Seq=(SELECT MAX(intOp2Seq) FROM OPDRECTBL
               WHERE chOp1Date=:opDate AND chOp1Time=:opTime
                 AND chOp1Room=:opRoom AND intOp1No=:opNo)) SS
        WHERE chOp1Date=:opDate AND chOp1Time=:opTime
          AND chOp1Room=:opRoom AND intOp1No=:opNo
        """;
    private const string IpdDetailSql = """
        SELECT chOp1mrno,chOp1Pname,DoctAllowDate AS EoutDate,chOp1Date,
               SS.AMT2,SS.AMT4,SS.AMT8,chop1sec,chop1drname
        FROM IPDBasicTbl,
          (SELECT intOp2Amt2 AS AMT2,intOp2AMT4 AS AMT4,intOp2AMT8 AS AMT8
           FROM IPDRECTBL WHERE chOp1Date=:opDate AND chOp1Time=:opTime
             AND chOp1Room=:opRoom AND intOp1No=:opNo
             AND intOp2Seq=(SELECT MAX(intOp2Seq) FROM IPDRECTBL
               WHERE chOp1Date=:opDate AND chOp1Time=:opTime
                 AND chOp1Room=:opRoom AND intOp1No=:opNo)) SS
        WHERE chOp1Date=:opDate AND chOp1Time=:opTime
          AND chOp1Room=:opRoom AND intOp1No=:opNo
        """;
    private const string LargeOrderSql = """
        SELECT CHOP4ORDNO || '(' || DECODE(NVL(CHOP4EXEDATE,'0'),'0','未做') || ')  '
               || RLOP4AMT2 || '  ' || CHOP4ORDNAME AS ORDNAME
        FROM OPDORDTBL WHERE chOp1Date=:opDate AND chOp1Time=:opTime
          AND chOp1Room=:opRoom AND intOp1No=:opNo
          AND CHOP4STAT <> 'DC' AND RLOP4AMT2 >= 1000
        """;
    private const string OrderTotalSql = """
        SELECT SUM(RLOP4AMT2) AS RLOP4AMT2 FROM OPDORDTBL
        WHERE chOp1Date=:opDate AND chOp1Time=:opTime
          AND chOp1Room=:opRoom AND intOp1No=:opNo AND CHOP4STAT <> 'DC'
          AND SUBSTR(CHOP4DCT,1,2) NOT IN ('02','06','25','30','35','49')
          AND SUBSTR(CHOP4DCT,1,2) < '50'
        """;

    public (List<C142DebtRow> Rows, List<C142RowError> Errors) Query(
        string startDate, string endDate, string source, string sectionCode, CancellationToken cancellationToken)
    {
        using var connection = Open();
        string sql = source switch { "Outpatient" => OutpatientSql, "Emergency" => EmergencySql,
            "Inpatient" => InpatientSql, _ => throw new ArgumentException("就醫類別不正確。") };
        var keys = new List<(string Date, string Time, string Room, decimal Number, decimal Debt, string? Card)>();
        using (OracleCommand command = Command(connection, sql))
        {
            Add(command, "bDate", startDate); Add(command, "eDate", endDate);
            Add(command, "applySec", sectionCode.Length == 0 ? 0 : 1);
            AddSectionCode(command, source, sectionCode);
            using OracleDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                keys.Add((Value(reader, 0), Value(reader, 1), Value(reader, 2),
                    Number(reader, 3), Number(reader, 4), Text(reader, 5)));
            }
        }
        var rows = new List<C142DebtRow>();
        var errors = new List<C142RowError>();
        foreach (var key in keys)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string encounterKey = $"{key.Date},{key.Time},{key.Room},{key.Number}";
            using OracleCommand detail = Command(connection, source == "Inpatient" ? IpdDetailSql : OpdDetailSql);
            AddKey(detail, key.Date, key.Time, key.Room, key.Number);
            using OracleDataReader reader = detail.ExecuteReader();
            if (!reader.Read()) { errors.Add(new(encounterKey, "查無收據")); continue; }
            string? mrNo = Text(reader, 0), name = Text(reader, 1), discharge = Text(reader, 2);
            decimal amount2 = NullableNumber(reader, 4) ?? 0;
            decimal amount4 = NullableNumber(reader, 5) ?? 0;
            decimal amount8 = NullableNumber(reader, 6) ?? 0;
            string? section = Text(reader, 7), doctor = Text(reader, 8);
            if (reader.Read()) { errors.Add(new(encounterKey, "最新收據序號有多筆")); continue; }
            reader.Close();
            string? sectionName = ScalarText(connection,
                "SELECT chsecname FROM gensectiontbl WHERE chsecno=:secNoFromBasic",
                command => Add(command, "secNoFromBasic", section ?? ""));
            string? bed = source == "Inpatient" ? null : ScalarText(connection,
                "SELECT chbedno FROM OpdSoapInHLicenseTbl WHERE chOp1Date=:opDate AND chOp1Time=:opTime AND chOp1Room=:opRoom AND intOp1No=:opNo",
                command => AddKey(command, key.Date, key.Time, key.Room, key.Number));
            rows.Add(new C142DebtRow {
                MedicalRecordNumber = mrNo, PatientName = name, VisitDate = key.Date,
                DischargeDate = source == "Inpatient" ? discharge?[..Math.Min(7, discharge.Length)] : null,
                SelfPayAmount = amount2 + amount4,
                PaidAmount = amount2 + amount4 - amount8 - key.Debt,
                DiscountAmount = amount8, DifferenceAmount = key.Debt,
                OutstandingAmount = key.Debt, SectionName = sectionName,
                DoctorName = doctor, BedNumber = bed, CardNumber = key.Card,
                VisitTime = key.Time, RoomNumber = key.Room, SequenceNumber = key.Number
            });
        }
        return (rows, errors);
    }

    public (string? Phone, string? Address) GetContact(string? medicalRecordNumber, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = Open();
        using OracleCommand command = Command(connection,
            "SELECT chtelh,chadd1 FROM opdmrbasictbl WHERE chmrno=:mrNo");
        Add(command, "mrNo", medicalRecordNumber ?? "");
        using OracleDataReader reader = command.ExecuteReader();
        return reader.Read() ? (Text(reader, 0), Text(reader, 1)) : (null, null);
    }

    public string? GetLargeOrder(C142DebtRow row, CancellationToken cancellationToken) =>
        ScalarForRow(LargeOrderSql, row, cancellationToken);

    public decimal? GetOrderTotal(C142DebtRow row, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = Open();
        using OracleCommand command = Command(connection, OrderTotalSql);
        AddKey(command, row.VisitDate ?? "", row.VisitTime, row.RoomNumber, row.SequenceNumber);
        object? value = command.ExecuteScalar();
        return value is null or DBNull ? null : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    public IReadOnlyList<C142Transaction> GetTransactions(string source, C142DebtRow row, CancellationToken cancellationToken)
    {
        string sql = source == "Inpatient" ? """
            SELECT CHIPDTYPE,CHIPDTDATETIME,CHIPDTUSER,CHIPDTNOTE FROM IPDAccTranTbl
            WHERE chOp1Date=:opDate AND chOp1Time=:opTime AND chOp1Room=:opRoom AND intOp1No=:opNo
            ORDER BY CHIPDTDATETIME
            """ : """
            SELECT CHOPDTTYPE,CHOPDTDATETIME,CHOPDTUSER,CHOPDTNOTE FROM OPDTRANTBL
            WHERE chOp1Date=:opDate AND chOp1Time=:opTime AND chOp1Room=:opRoom AND intOp1No=:opNo
            ORDER BY CHOPDTDATETIME
            """;
        using var connection = Open();
        using OracleCommand command = Command(connection, sql);
        AddKey(command, row.VisitDate ?? "", row.VisitTime, row.RoomNumber, row.SequenceNumber);
        using OracleDataReader reader = command.ExecuteReader();
        var result = new List<C142Transaction>();
        while (reader.Read()) { cancellationToken.ThrowIfCancellationRequested(); result.Add(new(Text(reader, 0), Text(reader, 1), Text(reader, 2), Text(reader, 3))); }
        return result;
    }

    private string? ScalarForRow(string sql, C142DebtRow row, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = Open();
        return ScalarText(connection, sql,
            command => AddKey(command, row.VisitDate ?? "", row.VisitTime, row.RoomNumber, row.SequenceNumber));
    }
    private static string? ScalarText(OracleConnection connection, string sql, Action<OracleCommand> bind)
    {
        using OracleCommand command = Command(connection, sql); bind(command);
        object? value = command.ExecuteScalar();
        return value is null or DBNull ? null : Convert.ToString(value, CultureInfo.InvariantCulture);
    }
    private OracleConnection Open() { var connection = new OracleConnection(connectionStringProvider.GetConnectionString()); connection.Open(); return connection; }
    private static OracleCommand Command(OracleConnection connection, string sql) => new(sql, connection) { BindByName = true, CommandTimeout = 600 };
    private static void Add(OracleCommand command, string name, object value) => command.Parameters.Add(new OracleParameter(name, value));
    private static void AddSectionCode(OracleCommand command, string source, string sectionCode)
    {
        if (source != "Inpatient" && sectionCode.Length > 0)
        {
            command.Parameters.Add(new OracleParameter("secNo", OracleDbType.Char, 7,
                sectionCode, System.Data.ParameterDirection.Input));
            return;
        }

        Add(command, "secNo", sectionCode);
    }

    private static void AddKey(OracleCommand command, string date, string time, string room, decimal number)
    { Add(command, "opDate", date); Add(command, "opTime", time); Add(command, "opRoom", room); Add(command, "opNo", number); }
    private static string Value(OracleDataReader reader, int ordinal) => Text(reader, ordinal) ?? "";
    private static string? Text(OracleDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    private static decimal Number(OracleDataReader reader, int ordinal) => NullableNumber(reader, ordinal) ?? 0;
    private static decimal? NullableNumber(OracleDataReader reader, int ordinal) => reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
}
