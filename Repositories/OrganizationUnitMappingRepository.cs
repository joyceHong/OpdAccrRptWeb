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

        var mappings = new List<OrganizationUnitMapping>();
        await using OracleDataReader reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess,
            cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            mappings.Add(new OrganizationUnitMapping(
                Enum.Parse<OrganizationUnitSource>(reader.GetString(0)),
                reader.GetString(1).Trim(),
                reader.GetString(2).Trim(),
                reader.IsDBNull(3) ? string.Empty : reader.GetString(3).Trim(),
                reader.GetInt32(4) == 1));
        }
        return mappings;
    }
}
