using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;

namespace OpdAccrRptWeb.Repositories;

public sealed class C212BoneBankBalanceRepository(IConnectionStringProvider connectionStringProvider)
    : IC212BoneBankBalanceRepository
{
    private const int CommandTimeoutSeconds = 60;

    public async Task<IReadOnlyList<C212RawRow>> GetRowsAsync(
        string endDateRoc,
        string monthFirstDayRoc,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateRocDate(endDateRoc, nameof(endDateRoc));
        ValidateRocDate(monthFirstDayRoc, nameof(monthFirstDayRoc));

        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new OracleCommand(C212Sql.Report, connection)
        {
            BindByName = true,
            CommandTimeout = CommandTimeoutSeconds
        };
        command.Parameters.Add(Input("end_date", endDateRoc));
        command.Parameters.Add(Input("month_first_day", monthFirstDayRoc));

        var rows = new List<C212RawRow>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var sortBucket = Convert.ToInt32(reader["SortBucket"], CultureInfo.InvariantCulture);
            rows.Add(new C212RawRow(
                sortBucket == 0 ? C212RowKind.OpeningBalance : C212RowKind.Movement,
                NormalizeText(reader["AccountingDateRoc"]),
                NormalizeText(reader["MedicalRecordNo"]),
                NormalizeText(reader["PatientName"]),
                ReadCheckedDecimal(reader, "RawOracleAmount")));
        }

        return rows;
    }

    public async Task<DateTime> GetOracleNowAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new OracleCommand(C212Sql.OracleNow, connection)
        {
            BindByName = true,
            CommandTimeout = CommandTimeoutSeconds
        };
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is DateTime databaseNow
            ? databaseNow
            : throw new DataException("Oracle SYSDATE 未回傳有效日期時間。");
    }

    internal static string NormalizeText(object? value) =>
        value is null or DBNull ? string.Empty : (Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty).Trim();

    private static decimal ReadCheckedDecimal(OracleDataReader reader, string name)
    {
        var ordinal = reader.GetOrdinal(name);
        if (reader.IsDBNull(ordinal))
            throw new DataException("C212 的 Oracle 彙總金額不可為 NULL。");

        OracleDecimal value = reader.GetOracleDecimal(ordinal);
        try
        {
            return value.Value;
        }
        catch (OverflowException exception)
        {
            throw new OverflowException("C212 的 Oracle 金額超出 .NET decimal 可表示範圍。", exception);
        }
    }

    private static OracleParameter Input(string name, string value) => new(name, OracleDbType.Varchar2, 7)
    {
        Direction = ParameterDirection.Input,
        Value = value
    };

    private static void ValidateRocDate(string value, string parameterName)
    {
        if (value.Length != 7 || value.Any(character => character is < '0' or > '9'))
            throw new ArgumentException("C212 Oracle 日期必須是七碼 ASCII 民國日期。", parameterName);
    }
}
