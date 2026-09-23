using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C9ReportRepository(IConnectionStringProvider connectionStrings)
    : IC9ReportRepository
{
    internal const int CommandTimeoutSeconds = 60;

    public async Task<IReadOnlyList<C9SourceRow>> QueryDayAsync(string rocDate,
        CancellationToken token = default)
    {
        if (rocDate.Length != 7 || !rocDate.All(char.IsAsciiDigit))
            throw new ArgumentException("C9 repository 日期必須為民國七碼。", nameof(rocDate));

        await using var connection = new OracleConnection(connectionStrings.GetConnectionString());
        await connection.OpenAsync(token);
        await using var command = new OracleCommand(C9Sql.Spay6OrderDetail, connection)
        {
            BindByName = true,
            CommandTimeout = CommandTimeoutSeconds
        };
        command.Parameters.Add(new OracleParameter("run_date", OracleDbType.Char, 7, rocDate,
            ParameterDirection.Input));

        var rows = new List<C9SourceRow>();
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, token);
        while (await reader.ReadAsync(token))
        {
            rows.Add(new(
                Text(reader, "chOp1Date"), Text(reader, "chOp1MrNo"),
                Text(reader, "chOp1PName"), Text(reader, "chOp1DRName"),
                Text(reader, "chSecName"), Text(reader, "chOp4CUser"),
                Text(reader, "chOp4OrdNo"), Text(reader, "chOp4OrdName"),
                Number(reader, "rlOp4Sub5"), Number(reader, "rlOp4Sub3")));
        }
        return rows;
    }

    private static string? Text(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static decimal? Number(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null
            : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }
}
