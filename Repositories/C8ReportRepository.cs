using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C8ReportRepository(IConnectionStringProvider connectionStrings) : IC8ReportRepository
{
    internal const int CommandTimeoutSeconds = 60;

    public async Task<IReadOnlyList<C8SourceRow>> QueryAsync(string rocStartDate,
        string rocEndDate, CancellationToken token = default)
    {
        await using var connection = new OracleConnection(connectionStrings.GetConnectionString());
        await connection.OpenAsync(token);
        await using var command = new OracleCommand(C8Sql.PatchBillDetail, connection)
        {
            BindByName = true,
            CommandTimeout = CommandTimeoutSeconds
        };
        Add(command, "start_date", rocStartDate);
        Add(command, "end_date", rocEndDate);
        var rows = new List<C8SourceRow>();
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, token);
        while (await reader.ReadAsync(token))
        {
            rows.Add(new(T(reader, "chOp1Room"), T(reader, "chOp1Date"),
                T(reader, "chOp4PSec"), T(reader, "chOp1MrNo"), T(reader, "chOp4PFin1"),
                T(reader, "chOp4OrdNo"), T(reader, "chOp4OrdName"), N(reader, "rlOp4Pric1"),
                N(reader, "rlOp4Pric2"), N(reader, "rlOp4OrdTot"), N(reader, "rlOp4AMT1"),
                N(reader, "rlOp4AMT2"), T(reader, "chOp4CUser")));
        }
        return rows;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetSectionMappingsAsync(
        IReadOnlyCollection<string> legacySectionCodes, CancellationToken token = default)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (legacySectionCodes.Count == 0) return result;
        await using var connection = new OracleConnection(connectionStrings.GetConnectionString());
        await connection.OpenAsync(token);
        foreach (string code in legacySectionCodes.Distinct(StringComparer.Ordinal))
        {
            await using var command = new OracleCommand(C8Sql.SectionLookup, connection)
            {
                BindByName = true,
                CommandTimeout = CommandTimeoutSeconds
            };
            Add(command, "legacy_section_code", code);
            object? value = await command.ExecuteScalarAsync(token);
            if (value is not null and not DBNull)
                result[code] = Convert.ToString(value, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
        }
        return result;
    }

    private static void Add(OracleCommand command, string name, string value) =>
        command.Parameters.Add(new OracleParameter(name, OracleDbType.Char, 7, value, ParameterDirection.Input));
    private static string? T(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }
    private static decimal? N(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }
}
