using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class ReportPermissionRepository(IConnectionStringProvider connectionStringProvider)
    : IReportPermissionRepository
{
    internal const string FindUsersSql = """
        SELECT RTRIM(chUserID) AS UserId, RTRIM(chUserSector) AS Sector
        FROM GenUserProfile1
        WHERE RTRIM(chUserID) = :LoginName
        """;

    internal const string FindGrantsSql = """
        SELECT NMENUCODE, NPERMISSIONTYPE, RTRIM(VCHPERMISSIONTARGET) AS VCHPERMISSIONTARGET,
               NBTNPERMISSION
        FROM OpdAccrReportPermissionTbl
        WHERE VCHSTAT IS NULL
          AND ((NPERMISSIONTYPE IN (0, 2) AND RTRIM(VCHPERMISSIONTARGET) = :UserId)
            OR (NPERMISSIONTYPE = 1 AND RTRIM(VCHPERMISSIONTARGET) = :Sector))
        """;

    internal const string FindTargetGrantsSql = """
        SELECT NMENUCODE, NPERMISSIONTYPE, RTRIM(VCHPERMISSIONTARGET) AS VCHPERMISSIONTARGET,
               NBTNPERMISSION
        FROM OpdAccrReportPermissionTbl
        WHERE VCHSTAT IS NULL
          AND NPERMISSIONTYPE = :TargetType
          AND RTRIM(VCHPERMISSIONTARGET) = :Target
        ORDER BY NMENUCODE
        """;

    private const string UpdateGrantSql = """
        UPDATE OpdAccrReportPermissionTbl
        SET NBTNPERMISSION = :ActionMask,
            VCHSTAT = NULL,
            DRECDATE = SYSDATE,
            VCHRECID = :Actor,
            DDCDATE = NULL,
            VCHDCUSERID = NULL
        WHERE NMENUCODE = :ReportCode
          AND NPERMISSIONTYPE = :TargetType
          AND RTRIM(VCHPERMISSIONTARGET) = :Target
        """;

    private const string InsertGrantSql = """
        INSERT INTO OpdAccrReportPermissionTbl
            (NPERMISSIONID, NMENUCODE, NPERMISSIONTYPE, VCHPERMISSIONTARGET, NBTNPERMISSION,
             DCDATE, VCHCUSRID)
        SELECT NVL(MAX(NPERMISSIONID), 0) + 1,
               :ReportCode, :TargetType, :Target, :ActionMask, SYSDATE, :Actor
        FROM OpdAccrReportPermissionTbl
        """;

    public async Task<IReadOnlyList<ReportPermissionUser>> FindUsersAsync(
        string loginName, CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateFindUsersCommand(connection, loginName);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var users = new List<ReportPermissionUser>();
        while (await reader.ReadAsync(cancellationToken))
        {
            users.Add(new(
                (Convert.ToString(reader["UserId"], CultureInfo.InvariantCulture) ?? string.Empty).TrimEnd(),
                reader.IsDBNull(reader.GetOrdinal("Sector"))
                    ? string.Empty
                    : (Convert.ToString(reader["Sector"], CultureInfo.InvariantCulture) ?? string.Empty).TrimEnd()));
        }

        return users;
    }

    public async Task<IReadOnlyList<ReportPermissionGrant>> FindGrantsAsync(
        string userId, string sector, CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateFindGrantsCommand(connection, userId, sector);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var grants = new List<ReportPermissionGrant>();
        while (await reader.ReadAsync(cancellationToken))
        {
            grants.Add(new(
                Convert.ToString(reader["NMENUCODE"], CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToInt32(reader["NPERMISSIONTYPE"], CultureInfo.InvariantCulture),
                Convert.ToString(reader["VCHPERMISSIONTARGET"], CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToInt32(reader["NBTNPERMISSION"], CultureInfo.InvariantCulture)));
        }

        return grants;
    }

    public async Task<bool> TargetExistsAsync(
        int targetType, string target, CancellationToken cancellationToken = default)
    {
        string sql = targetType == 1
            ? "SELECT COUNT(*) FROM GenUserProfile1 WHERE RTRIM(chUserSector) = :Target"
            : "SELECT COUNT(*) FROM GenUserProfile1 WHERE RTRIM(chUserID) = :Target";
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new OracleCommand(sql, connection) { BindByName = true };
        command.Parameters.Add("Target", OracleDbType.Varchar2, target, System.Data.ParameterDirection.Input);
        object? result = await command.ExecuteScalarAsync(cancellationToken);
        int count = Convert.ToInt32(result, CultureInfo.InvariantCulture);
        return targetType == 1 ? count > 0 : count == 1;
    }

    public async Task<IReadOnlyList<ReportPermissionGrant>> FindTargetGrantsAsync(
        int targetType, string target, CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new OracleCommand(FindTargetGrantsSql, connection) { BindByName = true };
        command.Parameters.Add("TargetType", OracleDbType.Int32, targetType, System.Data.ParameterDirection.Input);
        command.Parameters.Add("Target", OracleDbType.Varchar2, target, System.Data.ParameterDirection.Input);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var grants = new List<ReportPermissionGrant>();
        while (await reader.ReadAsync(cancellationToken))
        {
            grants.Add(new(
                Convert.ToString(reader["NMENUCODE"], CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToInt32(reader["NPERMISSIONTYPE"], CultureInfo.InvariantCulture),
                Convert.ToString(reader["VCHPERMISSIONTARGET"], CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToInt32(reader["NBTNPERMISSION"], CultureInfo.InvariantCulture)));
        }

        return grants;
    }

    public async Task SaveGrantsAsync(
        int targetType, string target, IReadOnlyDictionary<string, int> reportMasks,
        string actor, CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        using OracleTransaction transaction = connection.BeginTransaction();
        await using (var tableLock = new OracleCommand(
            "LOCK TABLE OpdAccrReportPermissionTbl IN EXCLUSIVE MODE WAIT 5", connection)
        { Transaction = transaction })
        {
            await tableLock.ExecuteNonQueryAsync(cancellationToken);
        }
        foreach ((string reportCode, int actionMask) in reportMasks)
        {
            await using var update = CreateWriteCommand(
                UpdateGrantSql, connection, transaction, reportCode, targetType, target, actionMask, actor);
            int updated = await update.ExecuteNonQueryAsync(cancellationToken);
            if (updated > 1) throw new InvalidOperationException("Duplicate permission rows found.");
            if (updated == 1) continue;
            await using var insert = CreateWriteCommand(
                InsertGrantSql, connection, transaction, reportCode, targetType, target, actionMask, actor);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }

        transaction.Commit();
    }

    private static OracleCommand CreateWriteCommand(
        string sql, OracleConnection connection, OracleTransaction transaction,
        string reportCode, int targetType, string target, int actionMask, string actor)
    {
        var command = new OracleCommand(sql, connection) { BindByName = true, Transaction = transaction };
        command.Parameters.Add("ReportCode", OracleDbType.Varchar2, reportCode, System.Data.ParameterDirection.Input);
        command.Parameters.Add("TargetType", OracleDbType.Int32, targetType, System.Data.ParameterDirection.Input);
        command.Parameters.Add("Target", OracleDbType.Varchar2, target, System.Data.ParameterDirection.Input);
        command.Parameters.Add("ActionMask", OracleDbType.Int32, actionMask, System.Data.ParameterDirection.Input);
        command.Parameters.Add("Actor", OracleDbType.Varchar2, actor, System.Data.ParameterDirection.Input);
        return command;
    }

    internal static OracleCommand CreateFindUsersCommand(OracleConnection connection, string loginName)
    {
        var command = new OracleCommand(FindUsersSql, connection) { BindByName = true };
        command.Parameters.Add("LoginName", OracleDbType.Varchar2, loginName, System.Data.ParameterDirection.Input);
        return command;
    }

    internal static OracleCommand CreateFindGrantsCommand(
        OracleConnection connection, string userId, string sector)
    {
        var command = new OracleCommand(FindGrantsSql, connection) { BindByName = true };
        command.Parameters.Add("UserId", OracleDbType.Varchar2, userId, System.Data.ParameterDirection.Input);
        command.Parameters.Add("Sector", OracleDbType.Varchar2, sector, System.Data.ParameterDirection.Input);
        return command;
    }
}
