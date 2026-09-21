using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C5ReportRepository(IConnectionStringProvider connectionStringProvider)
    : IC5ReportRepository
{
    internal const int CommandTimeoutSeconds = 60;
    public async Task<IC5ReportQuerySession> OpenSessionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        return new QuerySession(connection);
    }

    private sealed class QuerySession(OracleConnection connection) : IC5ReportQuerySession
    {
        public async Task<IReadOnlyList<C5SourceRow>> QueryDayAsync(C5ValidatedRequest request,
            string runDate, C5QueryId queryId, CancellationToken cancellationToken = default)
        {
            await using var command = new OracleCommand(C5Sql.Get(queryId), connection)
            {
                BindByName = true,
                CommandTimeout = CommandTimeoutSeconds
            };
            AddParameters(command, request, runDate, queryId);
            var rows = new List<C5SourceRow>();
            await using OracleDataReader reader = await command.ExecuteReaderAsync(
                CommandBehavior.SequentialAccess, cancellationToken);
            while (await reader.ReadAsync(cancellationToken)) rows.Add(Read(reader));
            return rows;
        }

        public ValueTask DisposeAsync() => connection.DisposeAsync();
    }

    internal static void AddParameters(OracleCommand command, C5ValidatedRequest request,
        string runDate, C5QueryId queryId)
    {
        Add(command, "run_date", OracleDbType.Char, 7, runDate);
        if (queryId.ToString().StartsWith("Opd", StringComparison.Ordinal))
            Add(command, "encounter_type", OracleDbType.Char, 1, request.EncounterType switch
            { C5EncounterType.Emergency => "E", C5EncounterType.Outpatient => "R", _ => "A" });
        AddNullable(command, "room_no", 20, request.RoomNo);
        if (queryId is not C5QueryId.OpdOrder0430Aggregate and not C5QueryId.OpdOrder0430Detail)
            AddNullable(command, "section_code", 10, request.LegacySectionCode);
        AddNullable(command, "charge_code", 30, request.ChargeCode);
        AddNullable(command, "insurance_identity_code", 10, request.InsuranceIdentityCode);
    }

    private static C5SourceRow Read(OracleDataReader r) => new(
        Text(r, "chOp1RoomType") ?? string.Empty, Text(r, "PSec") ?? string.Empty,
        Text(r, "DrgNo") ?? string.Empty, Text(r, "DrgName") ?? string.Empty,
        Text(r, "chOp1Date") ?? string.Empty, Text(r, "chOp1DrID"), Text(r, "chOp1DrName"),
        Text(r, "chOp1MrNo"), Text(r, "chOp1PName"), Text(r, "SPay"), Number(r, "Tot"),
        Number(r, "Pric1"), Number(r, "Pric2"), Number(r, "AMT1"), Number(r, "AMT2"),
        Has(r, "Sub3") ? Number(r, "Sub3") : null);

    private static bool Has(OracleDataReader r, string name)
    {
        try { _ = r.GetOrdinal(name); return true; }
        catch (IndexOutOfRangeException) { return false; }
    }
    private static decimal Number(OracleDataReader r, string name)
    {
        int index = r.GetOrdinal(name);
        return r.IsDBNull(index) ? 0m : Convert.ToDecimal(r.GetValue(index), CultureInfo.InvariantCulture);
    }
    private static string? Text(OracleDataReader r, string name)
    {
        if (!Has(r, name)) return null;
        int index = r.GetOrdinal(name);
        return r.IsDBNull(index) ? null : Convert.ToString(r.GetValue(index), CultureInfo.InvariantCulture)?.Trim();
    }
    private static void Add(OracleCommand command, string name, OracleDbType type, int size, object value) =>
        command.Parameters.Add(new OracleParameter(name, type, size, value, ParameterDirection.Input));
    private static void AddNullable(OracleCommand command, string name, int size, string? value) =>
        Add(command, name, OracleDbType.Varchar2, size, value is null ? DBNull.Value : value);
}
