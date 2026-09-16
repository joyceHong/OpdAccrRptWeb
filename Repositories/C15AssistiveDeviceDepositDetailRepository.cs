using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C15AssistiveDeviceDepositDetailRepository(
    IConnectionStringProvider connectionStringProvider) : IC15AssistiveDeviceDepositDetailRepository
{
    private const int CommandTimeoutSeconds = 60;

    internal const string QuerySql = """
        SELECT /*+index(o,OPDORDPK)*/
               a.chOp1Date AS VisitDate,
               a.chOp1Time AS VisitTime,
               a.chOp1Room AS VisitRoom,
               a.intOp1No AS EncounterNumber,
               a.chOp1MrNo AS MedicalRecordNumber,
               a.chOp1PName AS PatientName,
               a.chOp4DCDate AS ReturnDate,
               o.chOp4OrdNo AS OrderCode,
               o.rlOp4Sub1 + o.rlOp4Sub2 + o.rlOp4Sub3
                 + o.rlOp4Sub4 + o.rlOp4Sub5 + o.rlOp4Sub6 AS Amount
        FROM OpdAidPayTbl a
        JOIN OpdOrdTbl o
          ON o.chOp1Date = a.chOp1Date
         AND o.chOp1Time = a.chOp1Time
         AND o.chOp1Room = a.chOp1Room
         AND o.intOp1No = a.intOp1No
        WHERE a.chOp1MrNo NOT IN ('C36979', '1000000')
          AND a.chOp4DC IN ('0', '2')
          AND (a.chOp4IDate BETWEEN :AidStartDateTime AND :AidEndDateTime
               OR a.chOp4DCDate BETWEEN :StartDate AND :EndDate)
          AND o.chOp4IDate BETWEEN :OrderStartDateTime AND :OrderEndDateTime
          AND o.chOp4Stat <> 'DC'
          AND o.chOp4OrdNo IN ('696-001', '696-002', '696-003', '696-004', '696-007', '696-008')
          AND o.rlOp4Sub1 + o.rlOp4Sub2 + o.rlOp4Sub3
              + o.rlOp4Sub4 + o.rlOp4Sub5 + o.rlOp4Sub6 <> 0
        ORDER BY a.chOp1Date, a.chOp1Time, a.chOp1Room, a.intOp1No,
                 a.chOp1MrNo, a.chOp1PName, a.chOp4DCDate, o.chOp4OrdNo,
                 o.rlOp4Sub1 + o.rlOp4Sub2 + o.rlOp4Sub3
                   + o.rlOp4Sub4 + o.rlOp4Sub5 + o.rlOp4Sub6
        """;

    public IReadOnlyList<C15SourceRow> Query(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default)
    {
        C15QueryPeriod period = BuildPeriod(condition);
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        connection.Open();
        using var command = new OracleCommand(QuerySql, connection)
        {
            BindByName = true,
            CommandTimeout = CommandTimeoutSeconds
        };
        AddParameters(command, period);
        using OracleDataReader reader = command.ExecuteReaderAsync(cancellationToken).GetAwaiter().GetResult();
        var rows = new List<C15SourceRow>();
        while (reader.Read())
        {
            rows.Add(new C15SourceRow(
                ReadText(reader, "VisitDate"),
                ReadText(reader, "VisitTime"),
                ReadText(reader, "VisitRoom"),
                ReadDecimal(reader, "EncounterNumber"),
                ReadText(reader, "MedicalRecordNumber"),
                ReadText(reader, "PatientName"),
                ReadText(reader, "ReturnDate"),
                ReadText(reader, "OrderCode"),
                ReadNullableDecimal(reader, "Amount")));
        }
        return rows;
    }

    internal static C15QueryPeriod BuildPeriod(SearchReportCondition condition)
    {
        if (!DateOnly.TryParseExact(condition.StartDate, "yyyy-MM-dd", out DateOnly start)
            || !DateOnly.TryParseExact(condition.EndDate, "yyyy-MM-dd", out DateOnly end)
            || start > end || start.Year < 1912)
            throw new ArgumentException("C15 日期格式或範圍不正確。");

        return new C15QueryPeriod(
            $"{start.Year - 1911:000}{start:MMdd}",
            $"{end.Year - 1911:000}{end:MMdd}");
    }

    internal static void AddParameters(OracleCommand command, C15QueryPeriod period)
    {
        Add(command, "StartDate", OracleDbType.Varchar2, 7, period.StartDate);
        Add(command, "EndDate", OracleDbType.Varchar2, 7, period.EndDate);
        Add(command, "AidStartDateTime", OracleDbType.Varchar2, 11, period.StartDate + "0000");
        Add(command, "AidEndDateTime", OracleDbType.Varchar2, 11, period.EndDate + "9999");
        Add(command, "OrderStartDateTime", OracleDbType.Char, 11, period.StartDate + "0000");
        Add(command, "OrderEndDateTime", OracleDbType.Char, 11, period.EndDate + "9999");
    }

    private static void Add(OracleCommand command, string name, OracleDbType type, int size, string value) =>
        command.Parameters.Add(new OracleParameter(name, type)
        {
            Direction = ParameterDirection.Input,
            Size = size,
            Value = value
        });

    private static string ReadText(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal)
            ? string.Empty
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static decimal ReadDecimal(OracleDataReader reader, string name) =>
        Convert.ToDecimal(reader.GetValue(reader.GetOrdinal(name)), CultureInfo.InvariantCulture);

    private static decimal? ReadNullableDecimal(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    internal sealed record C15QueryPeriod(string StartDate, string EndDate);
}
