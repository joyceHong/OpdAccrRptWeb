using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class MedicalRecordQueryRepository(IConnectionStringProvider connections)
    : IMedicalRecordQueryRepository
{
    public int Count(MedicalRecordQueryFilters filters)
    {
        using OracleConnection connection = CreateConnection();
        connection.Open();
        using OracleCommand command = CreateFilterCommand(connection, MedicalRecordQuerySql.CountBase, filters);
        return Convert.ToInt32(command.ExecuteScalar(), CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<MedicalRecordSource>> QueryPageAsync(
        MedicalRecordQueryFilters filters,
        int offset,
        int pageSize,
        CancellationToken token)
    {
        await using OracleConnection connection = CreateConnection();
        await connection.OpenAsync(token);
        await using OracleCommand command = CreateFilterCommand(connection, MedicalRecordQuerySql.PageBase, filters);
        Add(command, "RowOffset", OracleDbType.Int32, offset);
        Add(command, "PageEnd", OracleDbType.Int32, checked(offset + pageSize));
        command.CommandText += "\n        ) WHERE RowNo > :RowOffset AND RowNo <= :PageEnd ORDER BY RowNo";

        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        var rows = new List<MedicalRecordSource>();
        while (await reader.ReadAsync(token))
        {
            rows.Add(ReadRow(reader));
        }

        return rows;
    }

    public async Task<MedicalRecordDetailSource?> QueryDetailAsync(
        string medicalRecordNo,
        CancellationToken token)
    {
        await using OracleConnection connection = CreateConnection();
        await connection.OpenAsync(token);
        await using OracleCommand command = CreateCommand(connection, MedicalRecordQuerySql.Detail);
        Add(command, "MedicalRecordNo", OracleDbType.Varchar2, medicalRecordNo);
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        return await reader.ReadAsync(token) ? ReadDetail(reader) : null;
    }

    public async Task<IReadOnlyList<string>> QueryMergedMedicalRecordNumbersAsync(
        string medicalRecordNo,
        CancellationToken token)
    {
        await using OracleConnection connection = CreateConnection();
        await connection.OpenAsync(token);
        await using OracleCommand command = CreateCommand(connection, MedicalRecordQuerySql.MergedMedicalRecordNumbers);
        Add(command, "MedicalRecordNo", OracleDbType.Varchar2, medicalRecordNo);
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

    public async Task<decimal?> QueryDebtTotalAsync(
        IReadOnlyCollection<string> medicalRecordNumbers,
        CancellationToken token)
    {
        string[] numbers = medicalRecordNumbers
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (numbers.Length == 0)
        {
            return 0m;
        }

        string placeholders = string.Join(",", numbers.Select((_, index) => $":DebtNo{index}"));
        string sql = string.Format(CultureInfo.InvariantCulture, MedicalRecordQuerySql.DebtTotal, placeholders);
        await using OracleConnection connection = CreateConnection();
        await connection.OpenAsync(token);
        await using OracleCommand command = CreateCommand(connection, sql);
        for (int index = 0; index < numbers.Length; index++)
        {
            Add(command, $"DebtNo{index}", OracleDbType.Char, numbers[index].PadRight(10, ' '));
        }

        object? value = await command.ExecuteScalarAsync(token);
        return value is null || value == DBNull.Value ? null : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    private OracleConnection CreateConnection() => new(connections.GetConnectionString());

    private static OracleCommand CreateCommand(OracleConnection connection, string sql) =>
        new(sql, connection) { BindByName = true };

    private static OracleCommand CreateFilterCommand(
        OracleConnection connection,
        string baseSql,
        MedicalRecordQueryFilters filters)
    {
        OracleCommand command = CreateCommand(connection, baseSql);
        command.CommandText += "\n" + MedicalRecordQueryPredicates.BuildWhere(filters, command);
        return command;
    }

    private static void Add(OracleCommand command, string name, OracleDbType type, object value) =>
        command.Parameters.Add(name, type, value, System.Data.ParameterDirection.Input);

    private static MedicalRecordSource ReadRow(OracleDataReader reader) => new(
        Text(reader, 0), Text(reader, 1), Text(reader, 2), Text(reader, 3), Text(reader, 4),
        Text(reader, 5), Text(reader, 6), Text(reader, 7), Text(reader, 8), Text(reader, 9),
        Text(reader, 10), Text(reader, 11), Text(reader, 12));

    private static MedicalRecordDetailSource ReadDetail(OracleDataReader reader) => new(
        Text(reader, 0), Text(reader, 1), Text(reader, 2), Text(reader, 3), Text(reader, 4),
        Text(reader, 5), Text(reader, 6), Text(reader, 7), Text(reader, 8), Text(reader, 9),
        Text(reader, 10), Text(reader, 11), Text(reader, 12), Text(reader, 13), Text(reader, 14),
        Text(reader, 15), Text(reader, 16), Text(reader, 17), Text(reader, 18), Text(reader, 19),
        Text(reader, 20), Text(reader, 21), Text(reader, 22), Text(reader, 23), Text(reader, 24),
        Text(reader, 25), Text(reader, 26), Text(reader, 27), Decimal(reader, 28), Text(reader, 29),
        Text(reader, 30));

    private static string Text(OracleDataReader reader, int index) =>
        reader.IsDBNull(index) ? string.Empty : Convert.ToString(reader.GetValue(index), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;

    private static decimal? Decimal(OracleDataReader reader, int index) =>
        reader.IsDBNull(index) ? null : Convert.ToDecimal(reader.GetValue(index), CultureInfo.InvariantCulture);
}

public static class MedicalRecordQueryPredicates
{
    public static string BuildWhere(MedicalRecordQueryFilters filters, OracleCommand command)
    {
        ArgumentNullException.ThrowIfNull(filters);
        ArgumentNullException.ThrowIfNull(command);

        var conditions = new List<string>();
        Append(conditions, command, "B.chMrNo", filters.MedicalRecordNo, 10);
        Append(conditions, command, "B.chID", filters.IdentityNumber, 10);
        Append(conditions, command, "B.chName", filters.Name, 10);
        Append(conditions, command, "B.chAdd1", filters.Address1, 60);
        Append(conditions, command, "B.chAdd2", filters.Address2, 60);
        Append(conditions, command, "B.chNewID", filters.NewIdentityNumber, 10);
        Append(conditions, command, "B.chNewName", filters.NewName, 10);
        Append(conditions, command, "B.chNewBirthday", filters.NewBirthday, 10);
        if (conditions.Count == 0)
        {
            throw new ArgumentException("請至少輸入一項查詢條件。", nameof(filters));
        }

        return "WHERE " + string.Join(" AND ", conditions);
    }

    private static void Append(
        ICollection<string> conditions,
        OracleCommand command,
        string column,
        string value,
        int fieldLength)
    {
        if (value.Length == 0)
        {
            return;
        }

        if (value == "!")
        {
            conditions.Add($"{column} > '.'");
            return;
        }

        if (value == "#")
        {
            conditions.Add($"(NOT {column} > '' OR {column} IS NULL)");
            return;
        }

        string? comparison = ComparisonOperator(value);
        string parameterName = $"Filter{command.Parameters.Count}";
        if (comparison is not null)
        {
            string operand = value[comparison.Length..];
            if (operand.Length == 0)
            {
                throw new ArgumentException("比較條件缺少值。");
            }

            AddParameter(command, parameterName, operand);
            conditions.Add($"SUBSTR({column}, 1, {operand.Length}) {comparison} :{parameterName}");
            return;
        }

        AddParameter(command, parameterName, value);
        conditions.Add(value.Length < fieldLength
            ? $"{column} LIKE :{parameterName} || '%'"
            : $"{column} = :{parameterName}");
    }

    private static string? ComparisonOperator(string value) =>
        value.StartsWith(">=", StringComparison.Ordinal) || value.StartsWith("<=", StringComparison.Ordinal) || value.StartsWith("<>", StringComparison.Ordinal)
            ? value[..2]
            : value.StartsWith('>') || value.StartsWith('<')
                ? value[..1]
                : null;

    private static void AddParameter(OracleCommand command, string name, string value) =>
        command.Parameters.Add(name, OracleDbType.Varchar2, value, System.Data.ParameterDirection.Input);
}
