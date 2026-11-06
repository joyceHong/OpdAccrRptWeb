using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C141NoticeHistoryRepository(IConnectionStringProvider connectionStringProvider)
    : IC141NoticeHistoryRepository
{
    public IReadOnlyList<C141NoticeHistoryRow> Query(string startDateRoc,
        string endDateRoc, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        using var command = new OracleCommand(C141NoticeHistorySql.Query, connection)
        {
            BindByName = true
        };
        C141OracleParameters.AddVarchar2(command, "startDateTime", startDateRoc + "000000", 13);
        C141OracleParameters.AddVarchar2(command, "endDateTime", endDateRoc + "999999", 13);
        connection.Open();
        using OracleDataReader reader = command.ExecuteReader();
        var rows = new List<C141NoticeHistoryRow>();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(new C141NoticeHistoryRow(
                ReadText(reader, "MedicalRecordNumber"),
                ReadText(reader, "NoticeDate"),
                ReadText(reader, "NoticeMethod"),
                ReadText(reader, "PaymentDate"),
                ReadAmount(reader, "NoticeAmount"),
                ReadAmount(reader, "CashAmount"),
                ReadAmount(reader, "CreditCardAmount")));
        }
        return rows;
    }

    private static string? ReadText(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal)
            ? null : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.TrimEnd();
    }

    private static decimal? ReadAmount(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal)
            ? null : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }
}
