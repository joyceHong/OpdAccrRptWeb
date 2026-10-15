using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C141RecalculationRepository(IConnectionStringProvider connectionStringProvider)
    : IC141RecalculationRepository
{
    public IReadOnlyList<C141RecalculationCase> GetClaimFlaggedCases(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        using var command = new OracleCommand(C141RecalculationSql.ClaimFlaggedCases, connection)
        {
            BindByName = true
        };
        connection.Open();

        using OracleDataReader reader = command.ExecuteReader();
        var cases = new List<C141RecalculationCase>();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            cases.Add(new C141RecalculationCase(
                new C141AdmissionKey(
                    ReadText(reader, "AdmissionDate") ?? string.Empty,
                    ReadText(reader, "AdmissionTime") ?? string.Empty,
                    ReadText(reader, "AdmissionRoom") ?? string.Empty,
                    ReadDecimal(reader, "AdmissionNumber") ?? 0m),
                ReadText(reader, "InsuranceIdentity"),
                ReadText(reader, "PartialPaymentCode"),
                ReadText(reader, "BedNumber")));
        }

        return cases;
    }

    public C141RecalculationTotals GetTotals(
        C141AdmissionKey key,
        string accountingDateRoc,
        bool isCurrentAccountingDay,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(accountingDateRoc);
        cancellationToken.ThrowIfCancellationRequested();

        string sql = isCurrentAccountingDay
            ? C141RecalculationSql.CurrentDayTotals
            : C141RecalculationSql.HistoricalDayTotals;
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        using var command = new OracleCommand(sql, connection) { BindByName = true };
        AddAdmissionKeyParameters(command, key);

        if (!isCurrentAccountingDay)
        {
            C141OracleParameters.AddFixedChar(
                command,
                "accountDate2359",
                $"{accountingDateRoc}2359",
                11);
        }

        connection.Open();
        using OracleDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow);
        if (!reader.Read())
            throw new DataException("C141 重排行金額查詢未回傳彙總列。");

        return new C141RecalculationTotals(
            ReadDecimal(reader, "AllTot1"),
            ReadDecimal(reader, "AllTot2"),
            ReadDecimal(reader, "AllTot3"));
    }

    internal static void AddAdmissionKeyParameters(OracleCommand command, C141AdmissionKey key)
    {
        C141OracleParameters.AddFixedChar(
            command, "opDate", key.AdmissionDate, C141OracleParameters.AdmissionDateLength);
        C141OracleParameters.AddFixedChar(
            command, "opTime", key.AdmissionTime, C141OracleParameters.AdmissionTimeLength);
        C141OracleParameters.AddFixedChar(
            command, "opRoom", key.AdmissionRoom, C141OracleParameters.AdmissionRoomLength);
        C141OracleParameters.AddAdmissionNumber(command, "opNo", key.AdmissionNumber);
    }

    private static string? ReadText(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.TrimEnd();
    }

    private static decimal? ReadDecimal(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }
}
