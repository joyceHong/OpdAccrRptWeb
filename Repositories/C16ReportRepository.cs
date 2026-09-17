using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C16ReportRepository(IConnectionStringProvider connectionStringProvider) : IC16ReportRepository
{
    private const int CommandTimeoutSeconds = 60;
    private const string OpdSelect = """
        SELECT m.chName AS PatientName,b.chOp1PID AS PatientId,b.chOp1PBrDt AS BirthDate,
               b.chOp1Date AS VisitDate,SUBSTR(b.chOp1EOutDate,1,7) AS DischargeDate,
               g.chSecName AS SectionName,b.vchOp1Icd101 AS BasicDiagnosis,
               b.chOp1PFin1 AS PFin1,b.chOp1PFin2 AS PFin2,o.chOp4OrdNo AS OrderCode,
               b.chOp1Time AS VisitTime,b.chOp1Room AS VisitRoom,b.intOp1No AS EncounterNumber,
        """;
    private const string OpdFrom = """
        FROM OpdTaipeiSubsidyPtTbl s
        JOIN OpdBasicTbl b ON b.chOp1Date=s.chOp1Date AND b.chOp1Time=s.chOp1Time AND b.chOp1Room=s.chOp1Room AND b.intOp1No=s.intOp1No
        JOIN OpdOrdTbl o ON o.chOp1Date=b.chOp1Date AND o.chOp1Time=b.chOp1Time AND o.chOp1Room=b.chOp1Room AND o.intOp1No=b.intOp1No
        JOIN OpdMRBasicTbl m ON m.chMrNo=RTRIM(b.chOp1MrNo)
        JOIN GenSectionTbl g ON g.chSecNo=b.chOp1Sec
        """;
    private const string CommonFilter = """
          AND (o.chOp4OrdNo IN ('1-25-99','1-49-99','1-50-99') OR (SUBSTR(o.chOp4Dct,1,2)='49' AND o.chOp4OrdNo LIKE '49-U%' AND LENGTH(RTRIM(o.chOp4OrdNo))=5))
          AND (o.chOp4Proj NOT IN ('I','S','D') OR RTRIM(o.chOp4Proj) IS NULL)
          AND (:ReportType NOT IN (1,2) OR (:ReportType=1 AND (b.chOp1PFin2<>'107' OR RTRIM(b.chOp1PFin2) IS NULL)) OR (:ReportType=2 AND b.chOp1PFin2='107'))
        """;
    private const string OpdGroup = """
        GROUP BY m.chName,b.chOp1PID,b.chOp1PBrDt,b.chOp1Date,SUBSTR(b.chOp1EOutDate,1,7),g.chSecName,b.vchOp1Icd101,b.chOp1PFin1,b.chOp1PFin2,o.chOp4OrdNo,b.chOp1Time,b.chOp1Room,b.intOp1No
        """;
    private const string AccountingSub5 = """SUM(CASE WHEN o.chOp4IDate BETWEEN :StartDateTime AND :EndDateTime AND (o.chOp4DCDate NOT BETWEEN :StartDateTime AND :EndDateTime OR RTRIM(o.chOp4DCDate) IS NULL) THEN o.rlOp4Sub5 WHEN o.chOp4DCDate BETWEEN :StartDateTime AND :EndDateTime AND o.chOp4IDate NOT BETWEEN :StartDateTime AND :EndDateTime THEN -o.rlOp4Sub5 ELSE 0 END)""";
    private const string AccountingSub2 = """SUM(CASE WHEN o.chOp4IDate BETWEEN :StartDateTime AND :EndDateTime AND (o.chOp4DCDate NOT BETWEEN :StartDateTime AND :EndDateTime OR RTRIM(o.chOp4DCDate) IS NULL) THEN o.rlOp4Sub2 WHEN o.chOp4DCDate BETWEEN :StartDateTime AND :EndDateTime AND o.chOp4IDate NOT BETWEEN :StartDateTime AND :EndDateTime THEN -o.rlOp4Sub2 ELSE 0 END)""";

    internal static readonly string OutpatientVisitDateSql = OpdSelect + "SUM(o.rlOp4Sub5) AS Sub5,SUM(o.rlOp4Sub2) AS Sub2\n" + OpdFrom + "\n" + """
        WHERE s.chOp1Date BETWEEN :StartDate AND :EndDate AND s.chOp1Time<>'0' AND o.chOp4Stat<>'DC'
        """ + "\n" + CommonFilter + "\n" + OpdGroup + "\n" + """
        HAVING SUM(o.rlOp4Sub5)<>0 OR SUM(o.rlOp4Sub2)<>0
        ORDER BY b.chOp1Date,m.chName,b.chOp1PID,b.chOp1PBrDt,b.vchOp1Icd101,b.chOp1PFin1,b.chOp1PFin2,b.chOp1Time,b.chOp1Room,b.intOp1No,o.chOp4OrdNo
        """;

    internal static readonly string OutpatientAccountingDateSql = OpdSelect + AccountingSub5 + " AS Sub5," + AccountingSub2 + " AS Sub2\n" + OpdFrom + "\n" + """
        WHERE (o.chOp4IDate BETWEEN :StartDateTime AND :EndDateTime OR o.chOp4DCDate BETWEEN :StartDateTime AND :EndDateTime) AND s.chOp1Time<>'0'
        """ + "\n" + CommonFilter + "\n" + OpdGroup + "\nHAVING " + AccountingSub5 + "<>0 OR " + AccountingSub2 + "<>0\n" + """
        ORDER BY b.chOp1Date,m.chName,b.chOp1PID,b.chOp1PBrDt,b.vchOp1Icd101,b.chOp1PFin1,b.chOp1PFin2,b.chOp1Time,b.chOp1Room,b.intOp1No,o.chOp4OrdNo
        """;

    internal static readonly string InpatientAccountingDateSql = """
        SELECT m.chName AS PatientName,b.chOp1PID AS PatientId,b.chOp1PBrDt AS BirthDate,b.chOp1Date AS VisitDate,
               SUBSTR(b.doctAllowDate,1,7) AS DischargeDate,'' AS SectionName,b.chOp1Icd101 AS BasicDiagnosis,
               b.chOp1PFin1 AS PFin1,b.chOp1PFin2 AS PFin2,o.chOp4OrdNo AS OrderCode,b.chOp1Time AS VisitTime,
               b.chOp1Room AS VisitRoom,b.intOp1No AS EncounterNumber,
        """ + AccountingSub5 + " AS Sub5," + AccountingSub2 + " AS Sub2\n" + """
        FROM OpdTaipeiSubsidyPtTbl s
        JOIN IpdBasicTbl b ON b.chOp1Date=s.chOp1Date AND b.chOp1Time=s.chOp1Time AND b.chOp1Room=s.chOp1Room AND b.intOp1No=s.intOp1No
        JOIN IpdOrdTbl o ON o.chOp1Date=b.chOp1Date AND o.chOp1Time=b.chOp1Time AND o.chOp1Room=b.chOp1Room AND o.intOp1No=b.intOp1No
        JOIN OpdMRBasicTbl m ON m.chMrNo=RTRIM(b.chOp1MrNo)
        WHERE (o.chOp4IDate BETWEEN :StartDateTime AND :EndDateTime OR o.chOp4DCDate BETWEEN :StartDateTime AND :EndDateTime) AND s.chOp1Time='0'
        """ + "\n" + CommonFilter + "\n" + """
        GROUP BY m.chName,b.chOp1PID,b.chOp1PBrDt,b.chOp1Date,SUBSTR(b.doctAllowDate,1,7),b.chOp1Icd101,b.chOp1PFin1,b.chOp1PFin2,o.chOp4OrdNo,b.chOp1Time,b.chOp1Room,b.intOp1No
        HAVING 
        """ + AccountingSub5 + "<>0 OR " + AccountingSub2 + "<>0\n" + """
        ORDER BY SUBSTR(b.doctAllowDate,1,7),m.chName,b.chOp1PID,b.chOp1PBrDt,b.chOp1Icd101,b.chOp1PFin1,b.chOp1PFin2,b.chOp1Time,b.chOp1Room,b.intOp1No,o.chOp4OrdNo
        """;

    internal const string FirstDiagnosisSql = """
        SELECT s.chDiag10Code FROM IpdSoapDiagTbl s JOIN GenIcd10Tbl i ON s.chDiag10Code=i.vchIcdNo
        WHERE s.chRegDate=:VisitDate AND s.chRegZone=:VisitTime AND s.chRegRoom=:VisitRoom AND s.chRegNo=:VisitNo
          AND (s.chStat<>'DC' OR RTRIM(s.chStat) IS NULL)
        ORDER BY s.chDiagState,s.chPrintDate,s.intSortNo
        FETCH FIRST 1 ROW ONLY
        """;

    public Task<IReadOnlyList<C16SourceRow>> QueryOutpatientByVisitDateAsync(C16PreviewRequest request, C16QueryPeriod period, CancellationToken token = default) =>
        QueryAsync(OutpatientVisitDateSql, request, period, false, token);
    public Task<IReadOnlyList<C16SourceRow>> QueryOutpatientByAccountingDateAsync(C16PreviewRequest request, C16QueryPeriod period, CancellationToken token = default) =>
        QueryAsync(OutpatientAccountingDateSql, request, period, false, token);
    public Task<IReadOnlyList<C16SourceRow>> QueryInpatientByAccountingDateAsync(C16PreviewRequest request, C16QueryPeriod period, CancellationToken token = default) =>
        QueryAsync(InpatientAccountingDateSql, request, period, true, token);

    private async Task<IReadOnlyList<C16SourceRow>> QueryAsync(string sql, C16PreviewRequest request, C16QueryPeriod period, bool inpatient, CancellationToken token)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(token);
        await using var command = new OracleCommand(sql, connection) { BindByName = true, CommandTimeout = CommandTimeoutSeconds };
        AddParameters(command, request, period, sql == OutpatientVisitDateSql);
        var rows = new List<C16SourceRow>();
        await using (OracleDataReader reader = await command.ExecuteReaderAsync(token))
        {
            while (await reader.ReadAsync(token)) rows.Add(ReadRow(reader, rows.Count));
        }
        if (!inpatient) return rows;
        var diagnoses = new Dictionary<C16VisitKey, string>();
        foreach (C16SourceRow row in rows)
        {
            C16VisitKey key = C16VisitKey.From(row);
            if (!diagnoses.ContainsKey(key)) diagnoses[key] = await QueryFirstDiagnosisAsync(connection, row, token);
        }
        return rows.Select(row => row with { FirstInpatientDiagnosis = diagnoses[C16VisitKey.From(row)] }).ToList();
    }

    internal static void AddParameters(OracleCommand command, C16PreviewRequest request, C16QueryPeriod period, bool visitDateMode)
    {
        if (visitDateMode)
        {
            Add(command, "StartDate", OracleDbType.Char, 7, period.StartDate);
            Add(command, "EndDate", OracleDbType.Char, 7, period.EndDate);
        }
        else
        {
            Add(command, "StartDateTime", OracleDbType.Char, 11, period.StartDateTime);
            Add(command, "EndDateTime", OracleDbType.Char, 11, period.EndDateTime);
        }
        command.Parameters.Add(new OracleParameter("ReportType", OracleDbType.Int32) { Direction = ParameterDirection.Input, Value = (int)request.ReportType });
    }

    private static void Add(OracleCommand command, string name, OracleDbType type, int size, string value) =>
        command.Parameters.Add(new OracleParameter(name, type) { Direction = ParameterDirection.Input, Size = size, Value = value });

    private static C16SourceRow ReadRow(OracleDataReader r, int ordinal) => new(ordinal,
        Text(r,"PatientName"),Text(r,"PatientId"),Text(r,"BirthDate"),Text(r,"VisitDate"),Text(r,"DischargeDate"),
        Text(r,"SectionName"),Text(r,"BasicDiagnosis"),null,Text(r,"PFin1"),Text(r,"PFin2"),Text(r,"OrderCode"),
        Text(r,"VisitTime"),Text(r,"VisitRoom"),Number(r,"EncounterNumber") ?? 0m,Number(r,"Sub5"),Number(r,"Sub2"));
    private static string Text(OracleDataReader r,string name) { int i=r.GetOrdinal(name); return r.IsDBNull(i)?string.Empty:Convert.ToString(r.GetValue(i),CultureInfo.InvariantCulture)??string.Empty; }
    private static decimal? Number(OracleDataReader r,string name) { int i=r.GetOrdinal(name); return r.IsDBNull(i)?null:Convert.ToDecimal(r.GetValue(i),CultureInfo.InvariantCulture); }

    private static async Task<string> QueryFirstDiagnosisAsync(OracleConnection connection, C16SourceRow row, CancellationToken token)
    {
        await using var command = new OracleCommand(FirstDiagnosisSql, connection) { BindByName = true, CommandTimeout = CommandTimeoutSeconds };
        Add(command,"VisitDate",OracleDbType.Char,7,row.VisitDate.Trim()); Add(command,"VisitTime",OracleDbType.Char,1,row.VisitTime.Trim());
        Add(command,"VisitRoom",OracleDbType.Char,6,row.VisitRoom.Trim());
        command.Parameters.Add(new OracleParameter("VisitNo",OracleDbType.Decimal){Direction=ParameterDirection.Input,Value=row.EncounterNumber});
        object? result = await command.ExecuteScalarAsync(token);
        return result is null or DBNull ? string.Empty : Convert.ToString(result,CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }
}
