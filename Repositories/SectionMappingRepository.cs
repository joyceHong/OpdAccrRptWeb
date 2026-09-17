using System.Data;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class SectionMappingRepository(
    IConnectionStringProvider connectionStringProvider,
    ILogger<SectionMappingRepository> logger) : ISectionMappingRepository
{
    internal static readonly IReadOnlyDictionary<string, SectionMapping> Constants = BuildConstants();

    public Task<bool> LocationExistsAsync(string location, CancellationToken token = default) =>
        ExistsAsync("SELECT 1 FROM GenSectionTbl WHERE chLocation=:code FETCH FIRST 1 ROW ONLY", location, token);

    public Task<SectionMapping?> FindPlaceAsync(string code, CancellationToken token = default) =>
        FindAsync("SELECT RTRIM(chNewPlaNo),RTRIM(chPlaName) FROM GenPlaceTbl WHERE RTRIM(chPlaNo)=:code", code, token);

    public Task<SectionMapping?> FindSectionAsync(string code, CancellationToken token = default) =>
        FindAsync("SELECT RTRIM(chNewSecNo),RTRIM(chSecName) FROM GenSectionTbl WHERE RTRIM(chSecNo)=:code", code, token);

    public async Task<SectionMapping?> FindLocationAsync(string sectionCode, CancellationToken token = default)
    {
        const string sql = "SELECT RTRIM(p.chNewPlaNo),RTRIM(p.chPlaName) FROM GenSectionTbl s JOIN GenPlaceTbl p ON RTRIM(p.chPlaNo)=RTRIM(s.chLocation) WHERE RTRIM(s.chSecNo)=:code FETCH FIRST 1 ROW ONLY";
        return await FindAsync(sql, sectionCode, token);
    }

    public async Task<SectionMapping> TranslateAsync(string oldCode, string roomType = "",
        CancellationToken cancellationToken = default)
    {
        string code = oldCode.Trim().ToUpperInvariant();
        if (roomType == "E")
        {
            string? emergency = code switch { "0201" => "11910", "0281" => "11920", "0220" or "0221" => "11930", "0230" => "11309", _ => null };
            if (emergency is not null) return new SectionMapping(emergency,
                (await FindSectionAsync(code, cancellationToken))?.DisplayName ?? string.Empty);
        }
        SectionMapping? mapping = await FindSectionAsync(code, cancellationToken)
            ?? await FindPlaceAsync(code, cancellationToken);
        if (mapping is not null) return mapping;
        if (Constants.TryGetValue(code, out SectionMapping? constant)) return constant;
        logger.LogWarning("C3 section mapping missing. OldCode={OldCode}", code);
        return new SectionMapping(string.Empty, string.Empty);
    }

    private async Task<bool> ExistsAsync(string sql, string code, CancellationToken token)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(token);
        await using var command = Command(connection, sql, code);
        return await command.ExecuteScalarAsync(token) is not null;
    }

    private async Task<SectionMapping?> FindAsync(string sql, string code, CancellationToken token)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(token);
        await using var command = Command(connection, sql, code);
        await using OracleDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow, token);
        if (!await reader.ReadAsync(token)) return null;
        return new SectionMapping(reader.IsDBNull(0) ? string.Empty : reader.GetString(0).Trim(),
            reader.IsDBNull(1) ? string.Empty : reader.GetString(1).Trim());
    }

    private static OracleCommand Command(OracleConnection connection, string sql, string code)
    {
        var command = new OracleCommand(sql, connection) { BindByName = true, CommandTimeout = 60 };
        command.Parameters.Add(new OracleParameter("code", OracleDbType.Varchar2, 10, code, ParameterDirection.Input));
        return command;
    }

    private static IReadOnlyDictionary<string, SectionMapping> BuildConstants()
    {
        var values = new Dictionary<string, SectionMapping>(StringComparer.Ordinal)
        {
            ["15HD3"] = new("15HD3", "血液透析室3樓"), ["15HD4"] = new("15HD4", "血液透析室4樓"),
            ["1013P"] = new("1013P", "腹膜透析室"), ["150MI"] = new("150MI", "內科ICU-10床"),
            ["15OPD"] = new("15OPD", "門診護理站_1"), ["1OR4F"] = new("1OR4F", "四樓手術室"),
            ["12710"] = new("12710", "藥品調劑科"), ["1510M"] = new("1510M", "骨髓移植病房"),
            ["15WD1"] = new("15WD1", "傷造口科")
        };
        for (int i = 1; i <= 27; i++) values[$"1OR{i:00}"] = new($"1OR{i:00}", $"三樓手術室_{i:00}");
        for (int i = 1; i <= 4; i++)
        {
            values[$"1CV{i:00}"] = new($"1CV{i:00}", $"心導管室-{i:00}");
            values[$"1XA{i:00}"] = new($"1XA{i:00}", $"影像醫學科-放射組{i:00}");
        }
        for (int i = 1; i <= 20; i++) values[$"1ED{i:00}"] = new($"1ED{i:00}", $"1ED{i:00}");
        return values;
    }
}
