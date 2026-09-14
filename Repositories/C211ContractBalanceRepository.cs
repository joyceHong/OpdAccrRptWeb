using System.Data;
using OpdAccrRptWeb.Infrastructure;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C211ContractBalanceRepository(IConnectionStringProvider connectionStringProvider)
    : IC211ContractBalanceRepository
{
    private const int CommandTimeoutSeconds = 60;

    private static readonly C211ContractChoice[] FixedChoices =
    [
        new("TT", "研究經費"), new("UU", "其他"), new("VV", "維康記帳"),
        new("WW", "北縣 65 歲以上老人健檢"), new("XX", "老人照護鑑定"),
        new("YY", "殘障鑑定"), new("ZZ", "聯盟代檢")
    ];

    public async Task<IReadOnlyList<C211Row>> GetRowsAsync(
        string source, DateOnly asOfDate, string? contractCode, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!C211Sources.IsSupported(source))
            throw new ArgumentException("C211 資料來源僅接受 O 或 I。", nameof(source));

        var normalizedContract = string.IsNullOrWhiteSpace(contractCode) ? null : contractCode.Trim();
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new OracleCommand(C211Sql.Report(source, normalizedContract is not null), connection)
        {
            BindByName = true,
            CommandTimeout = CommandTimeoutSeconds
        };
        command.Parameters.Add(Input("end_date", OracleDbType.Varchar2, ToRocDate(asOfDate), 7));
        if (normalizedContract is not null)
            command.Parameters.Add(Input("contract_code", OracleDbType.Varchar2, normalizedContract));

        var rows = new List<C211Row>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add(new C211Row(
                Text(reader, "ContractCode"), Text(reader, "MedicalRecordNo"),
                Text(reader, "VisitDateRoc"), Text(reader, "VisitTime"), Text(reader, "RoomCode"),
                Decimal(reader, "SequenceNo"), Decimal(reader, "SelfAmount"), Decimal(reader, "ClaimAmount")));
        }
        return rows;
    }

    public async Task<IReadOnlyList<C211ContractChoice>> GetContractChoicesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new OracleCommand(C211Sql.ContractChoices, connection)
        {
            BindByName = true,
            CommandTimeout = CommandTimeoutSeconds
        };
        var choices = new List<C211ContractChoice>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            choices.Add(new C211ContractChoice(Text(reader, "Code"), Text(reader, "Name")));

        return NormalizeChoices(choices.Concat(FixedChoices));
    }

    internal static IReadOnlyList<C211ContractChoice> NormalizeChoices(IEnumerable<C211ContractChoice> choices) =>
        choices.Select(choice => new C211ContractChoice(choice.Code.Trim(), choice.Name.Trim()))
            .Where(choice => choice.Code.Length > 0)
            .GroupBy(choice => choice.Code, StringComparer.Ordinal)
            .Select(group => group.First())
            .OrderBy(choice => choice.Code, StringComparer.Ordinal)
            .ToList();

    internal static string ToRocDate(DateOnly date) => $"{date.Year - 1911:000}{date:MMdd}";

    private static OracleParameter Input(string name, OracleDbType type, object value, int size = 0)
    {
        var parameter = new OracleParameter(name, type) { Direction = ParameterDirection.Input, Value = value };
        if (size > 0) parameter.Size = size;
        return parameter;
    }

    private static string Text(IDataRecord row, string name) =>
        row[name] is DBNull ? string.Empty : (Convert.ToString(row[name]) ?? string.Empty).Trim();

    private static decimal Decimal(IDataRecord row, string name) =>
        row[name] is DBNull ? 0m : Convert.ToDecimal(row[name]);
}
