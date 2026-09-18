using System.Data;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class OrganizationUnitMappingRepository(
    IConnectionStringProvider connectionStringProvider) : IOrganizationUnitMappingRepository
{
    internal const string FindByNewCodeSql = """
        SELECT source_type, legacy_code, new_code, display_name, is_active
        FROM (
            SELECT 'Section' source_type,
                   RTRIM(chSecNo) legacy_code,
                   RTRIM(chNewSecNo) new_code,
                   RTRIM(chSecName) display_name,
                   1 is_active
            FROM GenSectionTbl
            UNION ALL
            SELECT 'Place' source_type,
                   RTRIM(chPlaNo) legacy_code,
                   RTRIM(chNewPlaNo) new_code,
                   RTRIM(chPlaName) display_name,
                   CASE WHEN chPlaWork = '1' THEN 1 ELSE 0 END is_active
            FROM GenPlaceTbl
        )
        WHERE new_code = :new_code
          AND (:active_place_only = 0 OR source_type <> 'Place' OR is_active = 1)
        ORDER BY source_type, legacy_code
        """;

    internal const string FindByLegacyCodeSql = """
        SELECT source_type, legacy_code, new_code, display_name, is_active
        FROM (
            SELECT 'Section' source_type, RTRIM(chSecNo) legacy_code,
                   RTRIM(chNewSecNo) new_code, RTRIM(chSecName) display_name, 1 is_active
            FROM GenSectionTbl
            UNION ALL
            SELECT 'Place' source_type, RTRIM(chPlaNo) legacy_code,
                   RTRIM(chNewPlaNo) new_code, RTRIM(chPlaName) display_name,
                   CASE WHEN chPlaWork = '1' THEN 1 ELSE 0 END is_active
            FROM GenPlaceTbl
        )
        WHERE legacy_code = :legacy_code
          AND (:include_places = 1 OR source_type = 'Section')
        ORDER BY source_type, new_code
        """;

    internal const string SearchSql = """
        SELECT source_type, legacy_code, new_code, display_name, is_active
        FROM (
            SELECT 'Section' source_type, RTRIM(chSecNo) legacy_code,
                   RTRIM(chNewSecNo) new_code, RTRIM(chSecName) display_name, 1 is_active
            FROM GenSectionTbl
            UNION ALL
            SELECT 'Place' source_type, RTRIM(chPlaNo) legacy_code,
                   RTRIM(chNewPlaNo) new_code, RTRIM(chPlaName) display_name,
                   CASE WHEN chPlaWork = '1' THEN 1 ELSE 0 END is_active
            FROM GenPlaceTbl
        )
        WHERE ((:include_sections = 1 AND source_type = 'Section')
            OR (:include_places = 1 AND source_type = 'Place'))
          AND (:active_place_only = 0 OR source_type <> 'Place' OR is_active = 1)
          AND (UPPER(legacy_code) LIKE :query ESCAPE '\'
            OR UPPER(new_code) LIKE :query ESCAPE '\'
            OR UPPER(display_name) LIKE :query ESCAPE '\')
        ORDER BY legacy_code, source_type, display_name
        FETCH FIRST :result_limit ROWS ONLY
        """;

    public async Task<IReadOnlyList<OrganizationUnitMapping>> FindByNewCodeAsync(
        string newCode,
        bool activePlaceOnly,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new OracleCommand(FindByNewCodeSql, connection)
        {
            BindByName = true,
            CommandTimeout = 60
        };
        command.Parameters.Add("new_code", OracleDbType.Varchar2, 10).Value = newCode;
        command.Parameters.Add("active_place_only", OracleDbType.Int32).Value = activePlaceOnly ? 1 : 0;

        return await ReadAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationUnitMapping>> FindByLegacyCodeAsync(
        string legacyCode, OrganizationUnitMappingScope scope,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, FindByLegacyCodeSql);
        command.Parameters.Add("legacy_code", OracleDbType.Varchar2, 10).Value = legacyCode;
        command.Parameters.Add("include_places", OracleDbType.Int32).Value =
            scope == OrganizationUnitMappingScope.SectionAndPlace ? 1 : 0;
        return await ReadAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationUnitMapping>> SearchAsync(
        string query, bool includeSections, bool includePlaces, bool activePlaceOnly, int limit,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, SearchSql);
        command.Parameters.Add("include_sections", OracleDbType.Int32).Value = includeSections ? 1 : 0;
        command.Parameters.Add("include_places", OracleDbType.Int32).Value = includePlaces ? 1 : 0;
        command.Parameters.Add("active_place_only", OracleDbType.Int32).Value = activePlaceOnly ? 1 : 0;
        command.Parameters.Add("query", OracleDbType.Varchar2, 120).Value = $"%{EscapeLike(query)}%";
        command.Parameters.Add("result_limit", OracleDbType.Int32).Value = limit;
        return await ReadAsync(command, cancellationToken);
    }

    internal static string EscapeLike(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal);

    private static OracleCommand CreateCommand(OracleConnection connection, string sql) => new(sql, connection)
    {
        BindByName = true,
        CommandTimeout = 60
    };

    private static async Task<IReadOnlyList<OrganizationUnitMapping>> ReadAsync(
        OracleCommand command, CancellationToken cancellationToken)
    {
        var mappings = new List<OrganizationUnitMapping>();
        await using OracleDataReader reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            mappings.Add(new(Enum.Parse<OrganizationUnitSource>(reader.GetString(0)),
                reader.GetString(1).Trim(), reader.IsDBNull(2) ? string.Empty : reader.GetString(2).Trim(),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim(), reader.GetInt32(4) == 1));
        return mappings;
    }
}
