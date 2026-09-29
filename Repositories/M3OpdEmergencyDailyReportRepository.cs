using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class M3OpdEmergencyDailyReportRepository(IConnectionStringProvider connectionStrings)
    : IM3OpdEmergencyDailyReportRepository
{
    internal const int CommandTimeoutSeconds = 90;

    public async Task<M3RepositoryResult> QueryAsync(string reportDate, string monthStartDate,
        string yearStartDate, CancellationToken cancellationToken = default)
    {
        ValidateRocDate(reportDate, nameof(reportDate));
        ValidateRocDate(monthStartDate, nameof(monthStartDate));
        ValidateRocDate(yearStartDate, nameof(yearStartDate));
        await using var connection = new OracleConnection(connectionStrings.GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        IReadOnlyList<M3DepartmentAggregate> daily = await AggregatesAsync(connection,
            M3OpdEmergencyDailySql.Daily, reportDate, null, null, cancellationToken);
        IReadOnlyList<M3DepartmentAggregate> monthly = await AggregatesAsync(connection,
            M3OpdEmergencyDailySql.Monthly, reportDate, monthStartDate, null, cancellationToken);
        IReadOnlyList<M3DepartmentAggregate> yearly = await AggregatesAsync(connection,
            M3OpdEmergencyDailySql.Yearly, reportDate, null, yearStartDate, cancellationToken);
        long day = await CountAsync(connection, M3OpdEmergencyDailySql.EmergencyDay,
            reportDate, new("day_start_datetime", reportDate + "0730"),
            new("day_end_datetime", reportDate + "1530"), cancellationToken);
        long evening = await CountAsync(connection, M3OpdEmergencyDailySql.EmergencyEvening,
            reportDate, new("evening_start_datetime", reportDate + "1530"),
            new("evening_end_datetime", reportDate + "2330"), cancellationToken);
        long night = await CountAsync(connection, M3OpdEmergencyDailySql.EmergencyNight,
            reportDate, new("night_first_start_datetime", reportDate + "0000"),
            new("night_first_end_datetime", reportDate + "0730"),
            new("night_second_start_datetime", reportDate + "2330"),
            new("night_second_end_datetime", reportDate + "2359"), cancellationToken);
        M3KpiSource kpis = await KpisAsync(connection, reportDate, cancellationToken);
        return new(daily, monthly, yearly, new(day, evening, night), kpis);
    }

    public async Task<string> ResolveDepartmentNameAsync(string oldDepartmentId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionStrings.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = Command(connection, M3OpdEmergencyDailySql.DepartmentName);
        Add(command, "old_department_id", oldDepartmentId.Trim(), 10);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? "不詳" : Convert.ToString(result, CultureInfo.InvariantCulture)?.Trim() ?? "不詳";
    }

    private static async Task<IReadOnlyList<M3DepartmentAggregate>> AggregatesAsync(
        OracleConnection connection, string sql, string reportDate, string? monthStart,
        string? yearStart, CancellationToken token)
    {
        await using var command = Command(connection, sql);
        Add(command, "report_date", reportDate, 7);
        if (monthStart is not null) Add(command, "month_start_date", monthStart, 7);
        if (yearStart is not null) Add(command, "year_start_date", yearStart, 7);
        var rows = new List<M3DepartmentAggregate>();
        await using OracleDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, token);
        while (await reader.ReadAsync(token))
            rows.Add(new(Text(reader, "chDeptId"), Number(reader, "s1"), Number(reader, "s2"),
                Number(reader, "s3"), Number(reader, "s4"), Number(reader, "s5"), Number(reader, "s6"),
                Number(reader, "s7"), Number(reader, "s8"), Number(reader, "s9"), Number(reader, "s10"),
                Number(reader, "s11"), Number(reader, "s12")));
        return rows;
    }

    private static async Task<long> CountAsync(OracleConnection connection, string sql,
        string reportDate, (string Name, string Value) first, (string Name, string Value) second,
        CancellationToken token) => await CountAsync(connection, sql, reportDate,
            new[] { first, second }, token);

    private static async Task<long> CountAsync(OracleConnection connection, string sql,
        string reportDate, (string Name, string Value) first, (string Name, string Value) second,
        (string Name, string Value) third, (string Name, string Value) fourth,
        CancellationToken token) => await CountAsync(connection, sql, reportDate,
            new[] { first, second, third, fourth }, token);

    private static async Task<long> CountAsync(OracleConnection connection, string sql,
        string reportDate, IEnumerable<(string Name, string Value)> values, CancellationToken token)
    {
        await using var command = Command(connection, sql);
        Add(command, "report_date", reportDate, 7);
        foreach ((string name, string value) in values) Add(command, name, value, 11);
        object? result = await command.ExecuteScalarAsync(token);
        return Convert.ToInt64(result, CultureInfo.InvariantCulture);
    }

    private static async Task<M3KpiSource> KpisAsync(OracleConnection connection, string reportDate,
        CancellationToken token)
    {
        await using var command = Command(connection, M3OpdEmergencyDailySql.Kpis);
        Add(command, "report_date", reportDate, 7);
        await using OracleDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        if (!await reader.ReadAsync(token)) return new(null, null, null, null, null);
        return new(NullableNumber(reader, "s1"), NullableNumber(reader, "s2"), NullableNumber(reader, "s3"),
            NullableNumber(reader, "s7"), NullableNumber(reader, "s8"));
    }

    private static OracleCommand Command(OracleConnection connection, string sql) => new(sql, connection)
        { BindByName = true, CommandTimeout = CommandTimeoutSeconds };
    private static void Add(OracleCommand command, string name, string value, int length) =>
        command.Parameters.Add(new OracleParameter(name, OracleDbType.Varchar2, length, value, ParameterDirection.Input));
    private static string Text(OracleDataReader reader, string name) =>
        Convert.ToString(reader.GetValue(reader.GetOrdinal(name)), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    private static long Number(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        if (reader.IsDBNull(ordinal)) throw new InvalidOperationException($"M3 聚合欄位 {name} 不可為 Null。");
        return checked(Convert.ToInt64(reader.GetValue(ordinal), CultureInfo.InvariantCulture));
    }
    private static long? NullableNumber(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : checked(Convert.ToInt64(reader.GetValue(ordinal), CultureInfo.InvariantCulture));
    }
    private static void ValidateRocDate(string value, string parameterName)
    {
        if (value.Length != 7 || !value.All(char.IsAsciiDigit))
            throw new ArgumentException("M3 repository 日期必須為民國七碼。", parameterName);
    }
}
