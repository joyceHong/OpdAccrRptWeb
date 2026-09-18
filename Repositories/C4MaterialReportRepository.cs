using System.Data;
using System.Globalization;
using System.Reflection;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C4MaterialReportRepository(IConnectionStringProvider connectionStringProvider)
    : IC4MaterialReportRepository
{
    internal static readonly string DailySql = LoadSql();

    public async Task<IReadOnlyList<C4MaterialSourceRow>> QueryDayAsync(string runDate,
        string? sectionPrefix, CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new OracleCommand(DailySql, connection)
        {
            BindByName = true,
            CommandTimeout = 60
        };
        command.Parameters.Add("run_date", OracleDbType.Char, 7).Value = runDate;
        command.Parameters.Add("section_prefix", OracleDbType.Varchar2, 10).Value =
            sectionPrefix is null ? DBNull.Value : sectionPrefix;
        var rows = new List<C4MaterialSourceRow>();
        await using OracleDataReader reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new(Text(reader, 0) ?? "R", Text(reader, 1), Text(reader, 2), Text(reader, 3),
                Text(reader, 4), Text(reader, 5), Text(reader, 6), Text(reader, 7),
                Convert.ToDecimal(reader.GetValue(8), CultureInfo.InvariantCulture)));
        return rows;
    }

    private static string? Text(OracleDataReader reader, int ordinal) => reader.IsDBNull(ordinal)
        ? null : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.Trim();

    private static string LoadSql()
    {
        Assembly assembly = typeof(C4MaterialReportRepository).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(
            "Sql.C4.Report.Daily.sql", StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
