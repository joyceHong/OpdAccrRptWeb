using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C11ReceivablesCollectionRepository(IConnectionStringProvider connectionStringProvider)
    : IC11ReceivablesCollectionRepository
{
    private const int CommandTimeoutSeconds = 60;

    public Task<IReadOnlyList<C11AggregateRow>> QueryOutstandingToEndAsync(
        string source, string endDate, CancellationToken cancellationToken = default) =>
        QueryAggregatesAsync(
            source == C10Sources.Inpatient ? C11Sql.InpatientOutstandingToEnd : C11Sql.OutpatientOutstandingToEnd,
            source, null, endDate, cancellationToken);

    public Task<IReadOnlyList<C11AggregateRow>> QueryPeriodOutstandingAsync(
        string source, string startDate, string endDate, CancellationToken cancellationToken = default) =>
        QueryAggregatesAsync(
            source == C10Sources.Inpatient ? C11Sql.InpatientPeriodOutstanding : C11Sql.OutpatientPeriodOutstanding,
            source, startDate, endDate, cancellationToken);

    public async Task<C11PatientCountRow?> QueryPeriodPatientCountAsync(
        string source, string startDate, string endDate, string roomTypeName,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        bool inpatient = source == C10Sources.Inpatient;
        await using var command = CreateCommand(
            inpatient ? C11Sql.InpatientPatientCount : C11Sql.OutpatientPatientCount, connection);
        AddPeriodParameters(command, inpatient, startDate, endDate);
        if (!inpatient) command.Parameters.Add(Input("RoomTypeName", OracleDbType.Varchar2, roomTypeName));
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new C11PatientCountRow(ReadText(reader, "chOp1RoomTypeName"), ReadDecimal(reader, "MrNoCount"));
    }

    private async Task<IReadOnlyList<C11AggregateRow>> QueryAggregatesAsync(
        string sql, string source, string? startDate, string endDate, CancellationToken cancellationToken)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        bool inpatient = source == C10Sources.Inpatient;
        await using var command = CreateCommand(sql, connection);
        if (startDate is null)
        {
            string name = inpatient ? "EndDateTime" : "EndDate";
            command.Parameters.Add(Input(name, OracleDbType.Varchar2, inpatient ? endDate + "999999" : endDate));
        }
        else AddPeriodParameters(command, inpatient, startDate, endDate);

        var rows = new List<C11AggregateRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new(ReadText(reader, "chAccDate"), ReadText(reader, "chOp1RoomType"),
                ReadText(reader, "chOp1RoomTypeName"), ReadDecimal(reader, "rlOp1SubDebt_S")));
        return rows;
    }

    private static OracleCommand CreateCommand(string sql, OracleConnection connection) => new(sql, connection)
    {
        BindByName = true,
        CommandTimeout = CommandTimeoutSeconds
    };

    private static void AddPeriodParameters(OracleCommand command, bool inpatient, string startDate, string endDate)
    {
        command.Parameters.Add(Input(inpatient ? "StartDateTime" : "StartDate", OracleDbType.Varchar2,
            inpatient ? startDate + "000000" : startDate));
        command.Parameters.Add(Input(inpatient ? "EndDateTime" : "EndDate", OracleDbType.Varchar2,
            inpatient ? endDate + "999999" : endDate));
    }

    private static OracleParameter Input(string name, OracleDbType type, string value) => new(name, type)
    {
        Direction = ParameterDirection.Input,
        Value = value
    };

    private static string ReadText(OracleDataReader reader, string name) =>
        reader.IsDBNull(reader.GetOrdinal(name)) ? string.Empty :
        (Convert.ToString(reader[name], CultureInfo.InvariantCulture) ?? string.Empty).Trim();

    private static decimal ReadDecimal(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        if (reader.IsDBNull(ordinal)) throw new DataException($"C11 Oracle 欄位 {name} 不可為 NULL。");
        return Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }
}
