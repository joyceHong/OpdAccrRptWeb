using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C141RankingRepository(IConnectionStringProvider connectionStringProvider)
    : IC141RankingRepository
{
    public DateOnly GetOracleToday(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        using var command = new OracleCommand(
            "SELECT TO_CHAR(TRUNC(SYSDATE), 'YYYY-MM-DD') FROM DUAL",
            connection);
        connection.Open();
        object? value = command.ExecuteScalar();
        string date = Convert.ToString(value, CultureInfo.InvariantCulture)
            ?? throw new DataException("Oracle 未回傳 C141 伺服器日期。");
        return DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public IReadOnlyList<C141RankingSourceRow> Query(
        C141RankingSearch search,
        CancellationToken cancellationToken)
    {
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        using var command = new OracleCommand(C141RankingSql.Query, connection) { BindByName = true };
        AddSearchParameters(command, search);
        connection.Open();

        using OracleDataReader reader = command.ExecuteReader();
        return ReadRows(reader, cancellationToken);
    }

    public int GetCount(C141RankingSearch search, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        using var command = new OracleCommand(C141RankingSql.Count, connection) { BindByName = true };
        AddSearchParameters(command, search);
        AddMinimumNetAmount(command, search.MinimumNetAmount);
        connection.Open();
        object? result = command.ExecuteScalar();
        return Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    public IReadOnlyList<C141RankingSourceRow> GetPage(
        C141RankingSearch search,
        int offset,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(pageSize);
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        using var command = new OracleCommand(C141RankingSql.Page, connection) { BindByName = true };
        AddSearchParameters(command, search);
        AddMinimumNetAmount(command, search.MinimumNetAmount);
        C141OracleParameters.AddInt32(command, "rowOffset", offset);
        C141OracleParameters.AddInt32(command, "pageSize", pageSize);
        connection.Open();
        using OracleDataReader reader = command.ExecuteReader();
        return ReadRows(reader, cancellationToken);
    }

    internal static void AddSearchParameters(OracleCommand command, C141RankingSearch search)
    {
        string? medicalRecordNumber = Normalize(search.MedicalRecordNumber)?.ToUpperInvariant();
        string? bedNumber = Normalize(search.BedNumber);

        C141OracleParameters.AddFixedChar(
            command, "mrNo", medicalRecordNumber, C141OracleParameters.MedicalRecordNumberLength);
        C141OracleParameters.AddFixedChar(command, "bedNo", bedNumber, 8);
        C141OracleParameters.AddInt32(command, "bedNoLength", bedNumber?.Length ?? 0);

        string? prefix = bedNumber is null || bedNumber.Length == 6 ? null : $"{bedNumber}%";
        C141OracleParameters.AddVarchar2(command, "bedNoPrefix", prefix, Math.Max(prefix?.Length ?? 0, 1));
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void AddMinimumNetAmount(OracleCommand command, decimal? minimumNetAmount)
    {
        command.Parameters.Add(new OracleParameter("minimumNetAmount", OracleDbType.Decimal)
        {
            Value = (object?)minimumNetAmount ?? DBNull.Value
        });
    }

    private static IReadOnlyList<C141RankingSourceRow> ReadRows(
        OracleDataReader reader,
        CancellationToken cancellationToken)
    {
        var rows = new List<C141RankingSourceRow>();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            rows.Add(new C141RankingSourceRow(
                new C141AdmissionKey(
                    ReadText(reader, "AdmissionDate") ?? string.Empty,
                    ReadText(reader, "AdmissionTime") ?? string.Empty,
                    ReadText(reader, "AdmissionRoom") ?? string.Empty,
                    ReadDecimal(reader, "AdmissionNumber") ?? 0m),
                ReadText(reader, "MedicalRecordNumber"),
                ReadText(reader, "PatientName"),
                ReadText(reader, "BedNumber"),
                ReadText(reader, "InpatientAdmissionDate"),
                ReadText(reader, "InsuranceIdentity"),
                ReadText(reader, "InsuranceCategory"),
                ReadText(reader, "PaymentType"),
                ReadText(reader, "AdmissionSequence"),
                ReadText(reader, "InpatientSequence"),
                ReadText(reader, "AccountingDate"),
                ReadDecimal(reader, "ReceivableAmount"),
                ReadDecimal(reader, "InsuredReceivableAmount"),
                ReadText(reader, "PaymentDate"),
                ReadText(reader, "NoticeDate"),
                ReadDecimal(reader, "PayTotal"),
                Convert.ToInt32(reader["HasPreferredContact"], CultureInfo.InvariantCulture) == 1));
        }
        return rows;
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
