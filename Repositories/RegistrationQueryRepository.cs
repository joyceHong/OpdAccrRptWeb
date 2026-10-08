using System.Globalization;
using System.Data;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class RegistrationQueryRepository(IConnectionStringProvider connections)
    : IRegistrationQueryRepository
{
    public int Count(RegistrationQueryFilters filters)
    {
        using OracleConnection connection = CreateConnection();
        connection.Open();
        using OracleCommand command = CreateFilterCommand(connection, RegistrationQuerySql.CountBase, filters);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<RegistrationSource>> QueryPageAsync(
        RegistrationQueryFilters filters,
        int offset,
        int pageSize,
        CancellationToken token)
    {
        await using OracleConnection connection = CreateConnection();
        await connection.OpenAsync(token);
        await using OracleCommand command = CreateFilterCommand(connection, RegistrationQuerySql.PageBase, filters);
        Add(command, "RowOffset", OracleDbType.Int32, offset);
        Add(command, "RowEnd", OracleDbType.Int32, checked(offset + pageSize));
        command.CommandText += """
            )
            WHERE RegistrationRowNo > :RowOffset
              AND RegistrationRowNo <= :RowEnd
            ORDER BY chOp0Date DESC, chOp0Time DESC, chOp0Room DESC, intOp0No DESC
            """;

        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        var rows = new List<RegistrationSource>();
        while (await reader.ReadAsync(token))
        {
            rows.Add(ReadRegistration(reader));
        }

        return rows;
    }

    public async Task<IReadOnlyList<string>> QueryMergedMedicalRecordNumbersAsync(
        string medicalRecordNo,
        CancellationToken token)
    {
        await using OracleConnection connection = CreateConnection();
        await connection.OpenAsync(token);
        await using OracleCommand command = CreateCommand(connection, RegistrationQuerySql.MergedMedicalRecordNumbers);
        AddMedicalRecordNumber(command, "MedicalRecordNo", medicalRecordNo);

        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        var numbers = new List<string>();
        while (await reader.ReadAsync(token))
        {
            string value = Text(reader, 0);
            if (value.Length > 0 && !numbers.Contains(value, StringComparer.Ordinal))
            {
                numbers.Add(value);
            }
        }

        return numbers;
    }

    public async Task<RegistrationSummarySource> QuerySummaryAsync(
        string registrationDate,
        string time,
        string room,
        CancellationToken token)
    {
        await using OracleConnection connection = CreateConnection();
        await connection.OpenAsync(token);

        int totalCount = await ExecuteCountAsync(connection, RegistrationQuerySql.SummaryTotal,
            registrationDate, time, room, token);
        int seenCount = await ExecuteCountAsync(connection, RegistrationQuerySql.SummarySeen,
            registrationDate, time, room, token);
        int unseenCount = await ExecuteCountAsync(connection, RegistrationQuerySql.SummaryUnseen,
            registrationDate, time, room, token);
        IReadOnlyList<int> cancelledNumbers = await QueryCancelledNumbersAsync(
            connection, registrationDate, time, room, token);
        (int currentNumber, int prebookNumber, bool missing) = await QueryRoomNumbersAsync(
            connection, registrationDate, time, room, token);

        return new(
            totalCount,
            seenCount,
            unseenCount,
            cancelledNumbers,
            currentNumber,
            prebookNumber,
            missing);
    }

    public async Task<IReadOnlyList<RegistrationDoctorOption>> SearchDoctorsAsync(
        string query,
        CancellationToken token)
    {
        string normalized = query.Trim().ToUpperInvariant();
        if (normalized.Length == 0)
        {
            return [];
        }

        await using OracleConnection connection = CreateConnection();
        await connection.OpenAsync(token);
        await using OracleCommand command = CreateCommand(connection, RegistrationQuerySql.DoctorOptions);
        Add(command, "Query", OracleDbType.Varchar2, $"%{EscapeLike(normalized)}%");
        Add(command, "ResultLimit", OracleDbType.Int32, 20);

        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        var options = new List<RegistrationDoctorOption>();
        while (await reader.ReadAsync(token))
        {
            string code = Text(reader, 0);
            string name = Text(reader, 1);
            if (code.Length > 0)
            {
                options.Add(new(code, name));
            }
        }

        return options;
    }

    private async Task<int> ExecuteCountAsync(
        OracleConnection connection,
        string sql,
        string registrationDate,
        string time,
        string room,
        CancellationToken token)
    {
        await using OracleCommand command = CreateCommand(connection, sql);
        AddSummaryParameters(command, registrationDate, time, room);
        object? value = await command.ExecuteScalarAsync(token);
        return value is null || value == DBNull.Value
            ? 0
            : Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static async Task<IReadOnlyList<int>> QueryCancelledNumbersAsync(
        OracleConnection connection,
        string registrationDate,
        string time,
        string room,
        CancellationToken token)
    {
        await using OracleCommand command = CreateCommand(connection, RegistrationQuerySql.SummaryCancelled);
        AddSummaryParameters(command, registrationDate, time, room);
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        var numbers = new List<int>();
        while (await reader.ReadAsync(token))
        {
            numbers.Add(Integer(reader, "intOp0No"));
        }

        return numbers;
    }

    private static async Task<(int CurrentNumber, int PrebookNumber, bool Missing)> QueryRoomNumbersAsync(
        OracleConnection connection,
        string registrationDate,
        string time,
        string room,
        CancellationToken token)
    {
        await using OracleCommand command = CreateCommand(connection, RegistrationQuerySql.SummaryRoomNumbers);
        AddSummaryParameters(command, registrationDate, time, room);
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        if (!await reader.ReadAsync(token))
        {
            return (0, 0, true);
        }

        return (Integer(reader, 0), Integer(reader, 1), false);
    }

    private OracleConnection CreateConnection() => new(connections.GetConnectionString());

    private static OracleCommand CreateCommand(OracleConnection connection, string sql) =>
        new(sql, connection)
        {
            BindByName = true,
            CommandTimeout = 60
        };

    private static OracleCommand CreateFilterCommand(
        OracleConnection connection,
        string baseSql,
        RegistrationQueryFilters filters)
    {
        OracleCommand command = CreateCommand(connection, baseSql);
        command.CommandText += "\n" + RegistrationQueryPredicates.BuildWhere(filters, command);
        return command;
    }

    private static void AddSummaryParameters(
        OracleCommand command,
        string registrationDate,
        string time,
        string room)
    {
        Add(command, "RegDate", OracleDbType.Varchar2, registrationDate);
        Add(command, "RegTime", OracleDbType.Varchar2, time);
        Add(command, "RegRoom", OracleDbType.Varchar2, room);
    }

    private static void Add(OracleCommand command, string name, OracleDbType type, object value) =>
        command.Parameters.Add(name, type, value, ParameterDirection.Input);

    internal static void AddMedicalRecordNumber(
        OracleCommand command,
        string parameterName,
        string medicalRecordNo) =>
        command.Parameters.Add(
            parameterName,
            OracleDbType.Char,
            10,
            medicalRecordNo.PadRight(10, ' '),
            ParameterDirection.Input);

    private static RegistrationSource ReadRegistration(OracleDataReader reader) => new(
        RequiredText(reader, "chOp0Type"),
        RequiredText(reader, "chOp0Date"),
        RequiredText(reader, "chOp0Time"),
        IntegerOrNull(reader, "intOp0No"),
        RequiredText(reader, "chOp0Room"),
        RequiredText(reader, "chOp0PMrNo"),
        RequiredText(reader, "chOp0PName"),
        RequiredText(reader, "chOp0PtID"),
        RequiredText(reader, "chOp0BirDate"),
        RequiredText(reader, "chOp0Sex"),
        RequiredText(reader, "chOp0SecNo"),
        RequiredText(reader, "chOp0SecName"),
        RequiredText(reader, "chOp0DrNo"),
        RequiredText(reader, "chOp0DocName"),
        RequiredText(reader, "chOp0Fin1"),
        RequiredText(reader, "chOp0Fin2"),
        RequiredText(reader, "chOp0Payt"),
        RequiredText(reader, "chOp0Seq"),
        RequiredText(reader, "chOp0RoomAddr"),
        RequiredText(reader, "chOp0CUser"),
        RequiredText(reader, "chOp0CDate"),
        RequiredText(reader, "chOp0DCUserID"),
        RequiredText(reader, "chOp0DCDate"),
        OptionalText(reader, "chOp01st"),
        OptionalText(reader, "chOp0DigStat"),
        OptionalText(reader, "chOp0DC"),
        OptionalText(reader, "chOp0QuoteF1"),
        OptionalText(reader, "chOp0ER24HBack"));

    private static string RequiredText(OracleDataReader reader, string name) =>
        Text(reader, reader.GetOrdinal(name));

    private static string? OptionalText(OracleDataReader reader, string name)
    {
        try
        {
            return RequiredText(reader, name);
        }
        catch (IndexOutOfRangeException)
        {
            return null;
        }
    }

    private static string Text(OracleDataReader reader, int index) =>
        reader.IsDBNull(index)
            ? string.Empty
            : Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;

    private static int Integer(OracleDataReader reader, string name) =>
        Convert.ToInt32(reader.GetValue(reader.GetOrdinal(name)), CultureInfo.InvariantCulture);

    private static int Integer(OracleDataReader reader, int index) =>
        reader.IsDBNull(index)
            ? 0
            : Convert.ToInt32(reader.GetValue(index), CultureInfo.InvariantCulture);

    private static int? IntegerOrNull(OracleDataReader reader, string name)
    {
        int index = reader.GetOrdinal(name);
        return reader.IsDBNull(index) ? null : Integer(reader, index);
    }

    private static string EscapeLike(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}

public static class RegistrationQueryPredicates
{
    public static string BuildWhere(RegistrationQueryFilters filters, OracleCommand command)
    {
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentNullException.ThrowIfNull(command);

        var conditions = new List<string>();
        AddText(conditions, command, "R.chOp0Date", "RegDate", filters.RegDate);
        AddText(conditions, command, "R.chOp0Time", "RegTime", filters.Time);
        AddText(conditions, command, "R.chOp0Room", "RegRoom", filters.Room);
        if (filters.RegistrationNo.HasValue)
        {
            conditions.Add("R.intOp0No = :RegNo");
            Add(command, "RegNo", OracleDbType.Int32, filters.RegistrationNo.Value);
        }

        if (filters.MedicalRecordNo.Length > 0)
        {
            if (filters.PatientId.Length > 0)
            {
                conditions.Add("R.chOp0PMrNo = :MrNo");
                RegistrationQueryRepository.AddMedicalRecordNumber(command, "MrNo", filters.MedicalRecordNo);
            }
            else
            {
                IReadOnlyList<string> numbers = filters.MedicalRecordNumbers.Count > 0
                    ? filters.MedicalRecordNumbers
                    : [filters.MedicalRecordNo];
                var placeholders = new List<string>(numbers.Count);
                for (int index = 0; index < numbers.Count; index++)
                {
                    string parameterName = $"MrNo{index}";
                    placeholders.Add($":{parameterName}");
                    RegistrationQueryRepository.AddMedicalRecordNumber(command, parameterName, numbers[index]);
                }

                conditions.Add($"R.chOp0PMrNo IN ({string.Join(", ", placeholders)})");
            }
        }

        AddText(conditions, command, "R.chOp0PtID", "PatientId", filters.PatientId);
        AddText(conditions, command, "R.chOp0BirDate", "BirthDate", filters.BirthDate);
        AddText(conditions, command, "R.chOp0SecNo", "SectionNo", filters.SectionNo);
        AddText(conditions, command, "R.chOp0DrNo", "DoctorNo", filters.DoctorNo);
        AppendStatus(conditions, filters);
        conditions.Add("R.chOp0Room <> 'ZZZZ'");
        conditions.Add("R.chOp0Room <> 'RRRR'");
        conditions.Add("R.chOp0Room <> 'SSSS'");
        conditions.Add("R.chOp0Room <> 'AAAA'");

        return "WHERE " + string.Join("\n  AND ", conditions);
    }

    private static void AppendStatus(ICollection<string> conditions, RegistrationQueryFilters filters)
    {
        bool hasInput = filters.HasAnyCondition;
        switch (filters.Mode)
        {
            case RegistrationQueryMode.Registered when !hasInput:
            case RegistrationQueryMode.Seen when !hasInput:
            case RegistrationQueryMode.Unpriced when !hasInput:
                conditions.Add("R.chOp0DigStat = '0' AND R.chOp0DC = '0'");
                break;
            case RegistrationQueryMode.Registered:
                break;
            case RegistrationQueryMode.Cancelled:
                conditions.Add("R.chOp0DigStat = '0' AND R.chOp0DC = '1'");
                break;
            case RegistrationQueryMode.Unseen:
                conditions.Add("R.chOp0DigStat = '0' AND R.chOp0DC = '0'");
                break;
            case RegistrationQueryMode.Seen:
                conditions.Add("R.chOp0DigStat = '3' AND R.chOp0DC = '0'");
                break;
            case RegistrationQueryMode.Unpriced:
                conditions.Add("R.chOp0DigStat = '3' AND R.chOp0DC = '0'");
                conditions.Add("(R.chOp0QuoteFlg <> 'S' AND R.chOp0QuoteFlg <> 'U' AND R.chOp0QuoteFlg <> 'Y' OR R.chOp0QuoteFlg IS NULL)");
                break;
            case RegistrationQueryMode.Summary:
                throw new ArgumentException("報診模式不使用掛號清單條件。", nameof(filters));
            default:
                throw new ArgumentOutOfRangeException(nameof(filters.Mode));
        }
    }

    private static void AddText(
        ICollection<string> conditions,
        OracleCommand command,
        string column,
        string parameterName,
        string value)
    {
        if (value.Length == 0)
        {
            return;
        }

        conditions.Add($"{column} = :{parameterName}");
        Add(command, parameterName, OracleDbType.Varchar2, value);
    }

    private static void Add(OracleCommand command, string name, OracleDbType type, object value) =>
        command.Parameters.Add(name, type, value, ParameterDirection.Input);
}
