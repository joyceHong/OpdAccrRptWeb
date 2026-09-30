using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class M1DoctorDailyReportRepository(IConnectionStringProvider connectionStrings)
    : IM1DoctorDailyReportRepository
{
    internal const int CommandTimeoutSeconds = 90;

    public async Task<IReadOnlyList<M1DoctorDailyAggregateRow>> QueryAsync(
        string rocDate,
        CancellationToken cancellationToken = default)
    {
        if (rocDate.Length != 7 || !rocDate.All(char.IsAsciiDigit))
            throw new ArgumentException("M1 repository 日期必須為民國七碼。", nameof(rocDate));

        await using var connection = new OracleConnection(connectionStrings.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new OracleCommand(M1DoctorDailySql.Select(rocDate), connection)
        {
            BindByName = true,
            CommandTimeout = CommandTimeoutSeconds
        };
        command.Parameters.Add(new OracleParameter(
            "report_date", OracleDbType.Char, 7, rocDate, ParameterDirection.Input));

        var rows = new List<M1DoctorDailyAggregateRow>();
        await using OracleDataReader reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new(
                Text(reader, "chSecNo"), Text(reader, "chSecName"),
                Text(reader, "chODrId"), Text(reader, "chDocName"),
                Number(reader, "s1"), Number(reader, "s2"), Number(reader, "s3"),
                Number(reader, "s4"), Number(reader, "s5"), Number(reader, "s6"),
                Number(reader, "s7"), Number(reader, "s8"), Number(reader, "s9"),
                Number(reader, "s10"), Number(reader, "s11")));
        }
        return rows;
    }

    private static string Text(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? string.Empty
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }

    private static int Number(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return NumberOrZero(reader.GetValue(ordinal));
    }

    internal static int NumberOrZero(object? value) => value is null or DBNull
        ? 0
        : checked(Convert.ToInt32(value, CultureInfo.InvariantCulture));
}
