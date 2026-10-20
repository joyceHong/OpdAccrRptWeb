using System.Globalization;
using System.Text;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class ReportPermissionRepository(IConnectionStringProvider connectionStringProvider)
    : IReportPermissionRepository
{
    internal const string ActiveGrantSourceSql = """
        FROM OpdAccrReportPermissionTbl grantRow
        LEFT JOIN (
            SELECT RTRIM(chUserID) AS TargetCode, MAX(RTRIM(chUserName)) AS TargetName
            FROM GenUserProfile1 GROUP BY RTRIM(chUserID)
        ) person ON grantRow.NPERMISSIONTYPE IN (0, 2)
            AND RTRIM(grantRow.VCHPERMISSIONTARGET) = person.TargetCode
        LEFT JOIN (
            SELECT NVL(p.CHPLANO, s.CHSECNO) AS TargetCode,
                   NVL(p.CHPLANAME, s.CHSECNAME) AS TargetName
            FROM (SELECT CHPLANO, CHPLANAME FROM genplacetbl WHERE CHPLAWORK = '1') p
            FULL OUTER JOIN (SELECT CHSECNO, CHSECNAME FROM gensectiontbl) s
                ON p.CHPLANO = s.CHSECNO
        ) department ON grantRow.NPERMISSIONTYPE = 1
            AND RTRIM(grantRow.VCHPERMISSIONTARGET) = RTRIM(department.TargetCode)
        WHERE grantRow.VCHSTAT IS NULL
        """;

    internal const string SearchPeopleSql = """
        SELECT Code, Name FROM (
            SELECT RTRIM(chUserID) AS Code, MAX(RTRIM(chUserName)) AS Name
            FROM GenUserProfile1
            WHERE vchStatus = 1
              AND (UPPER(RTRIM(chUserID)) LIKE :Search ESCAPE '\'
                OR UPPER(RTRIM(chUserName)) LIKE :Search ESCAPE '\')
            GROUP BY RTRIM(chUserID)
            HAVING COUNT(*) = 1
            ORDER BY Code
        ) WHERE ROWNUM <= 20
        """;

    internal const string SearchDepartmentsSql = """
        SELECT Code, Name FROM (
            SELECT RTRIM(NVL(p.CHPLANO, s.CHSECNO)) AS Code,
                   RTRIM(NVL(p.CHPLANAME, s.CHSECNAME)) AS Name
            FROM (SELECT CHPLANO, CHPLANAME FROM genplacetbl WHERE CHPLAWORK = '1') p
            FULL OUTER JOIN (SELECT CHSECNO, CHSECNAME FROM gensectiontbl) s
                ON p.CHPLANO = s.CHSECNO
            WHERE UPPER(RTRIM(NVL(p.CHPLANO, s.CHSECNO))) LIKE :Search ESCAPE '\'
               OR UPPER(RTRIM(NVL(p.CHPLANAME, s.CHSECNAME))) LIKE :Search ESCAPE '\'
            ORDER BY Code
        ) WHERE ROWNUM <= 20
        """;

    internal const string ValidateDepartmentSql = """
        SELECT COUNT(*) FROM (
            SELECT RTRIM(NVL(p.CHPLANO, s.CHSECNO)) AS Code
            FROM (SELECT CHPLANO FROM genplacetbl WHERE CHPLAWORK = '1') p
            FULL OUTER JOIN (SELECT CHSECNO FROM gensectiontbl) s
                ON p.CHPLANO = s.CHSECNO
        ) WHERE Code = :Target
        """;

    internal const string ValidatePersonSql = """
        SELECT COUNT(*) FROM GenUserProfile1
        WHERE vchStatus = 1 AND RTRIM(chUserID) = :Target
        """;

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

    public async Task<PermissionGrantRecordPage> FindActiveGrantPageAsync(
        IReadOnlyList<string> categoryCodes, IReadOnlyList<string> matchingNameCodes,
        string search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (categoryCodes.Count == 0) return new([], 0);

        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        string codesSql = string.Join(", ", categoryCodes.Select((_, index) => $":Code{index}"));
        string namesSql = string.Join(", ", matchingNameCodes.Select((_, index) => $":NameCode{index}"));
        var where = new StringBuilder(ActiveGrantSourceSql);
        where.Append($" AND grantRow.NMENUCODE IN ({codesSql})");
        if (search.Length > 0)
        {
            where.Append(" AND (UPPER(RTRIM(grantRow.NMENUCODE)) LIKE :Search ESCAPE '\\'");
            where.Append(" OR UPPER(RTRIM(grantRow.VCHPERMISSIONTARGET)) LIKE :Search ESCAPE '\\'");
            where.Append(" OR UPPER(NVL(person.TargetName, department.TargetName)) LIKE :Search ESCAPE '\\'");
            if (matchingNameCodes.Count > 0)
                where.Append($" OR grantRow.NMENUCODE IN ({namesSql})");
            where.Append(')');
        }

        string source = where.ToString();
        await using var countCommand = CreateListCommand(
            connection, $"SELECT COUNT(*) {source}", categoryCodes, matchingNameCodes, search);
        int totalCount = Convert.ToInt32(
            await countCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (totalCount == 0) return new([], 0);

        string rowsSql = $"""
            SELECT ReportCode, TargetType, TargetCode, ActionMask, TargetName, ModifiedBy, ModifiedAt
            FROM (
                SELECT baseRows.*, ROW_NUMBER() OVER (
                    ORDER BY ModifiedAt DESC, ReportCode, TargetType, TargetCode) AS RowNumber
                FROM (
                    SELECT grantRow.NMENUCODE AS ReportCode,
                           grantRow.NPERMISSIONTYPE AS TargetType,
                           RTRIM(grantRow.VCHPERMISSIONTARGET) AS TargetCode,
                           grantRow.NBTNPERMISSION AS ActionMask,
                           NVL(person.TargetName, department.TargetName) AS TargetName,
                           RTRIM(NVL(grantRow.VCHRECID, grantRow.VCHCUSRID)) AS ModifiedBy,
                           NVL(grantRow.DRECDATE, grantRow.DCDATE) AS ModifiedAt
                    {source}
                ) baseRows
            ) WHERE RowNumber BETWEEN :StartRow AND :EndRow
            """;
        await using var command = CreateListCommand(
            connection, rowsSql, categoryCodes, matchingNameCodes, search);
        command.Parameters.Add("StartRow", OracleDbType.Int32, (page - 1) * pageSize + 1,
            System.Data.ParameterDirection.Input);
        command.Parameters.Add("EndRow", OracleDbType.Int32, page * pageSize,
            System.Data.ParameterDirection.Input);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<PermissionGrantListRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new(
                Convert.ToString(reader["ReportCode"], CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToInt32(reader["TargetType"], CultureInfo.InvariantCulture),
                Convert.ToString(reader["TargetCode"], CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToInt32(reader["ActionMask"], CultureInfo.InvariantCulture),
                Convert.ToString(reader["TargetName"], CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToString(reader["ModifiedBy"], CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToDateTime(reader["ModifiedAt"], CultureInfo.InvariantCulture)));
        }
        return new(items, totalCount);
    }

    public async Task<PermissionTargetListPage> FindActiveTargetPageAsync(
        IReadOnlyList<string> categoryCodes, IReadOnlyList<string> matchingNameCodes,
        string search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        if (categoryCodes.Count == 0) return new([], page, pageSize, 0);

        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        string codesSql = string.Join(", ", categoryCodes.Select((_, index) => $":Code{index}"));
        string namesSql = string.Join(", ", matchingNameCodes.Select((_, index) => $":NameCode{index}"));
        var where = new StringBuilder(ActiveGrantSourceSql);
        where.Append($" AND grantRow.NMENUCODE IN ({codesSql})");
        if (search.Length > 0)
        {
            where.Append(" AND (UPPER(RTRIM(grantRow.NMENUCODE)) LIKE :Search ESCAPE '\\'");
            where.Append(" OR UPPER(RTRIM(grantRow.VCHPERMISSIONTARGET)) LIKE :Search ESCAPE '\\'");
            where.Append(" OR UPPER(NVL(person.TargetName, department.TargetName)) LIKE :Search ESCAPE '\\'");
            if (matchingNameCodes.Count > 0)
                where.Append($" OR grantRow.NMENUCODE IN ({namesSql})");
            where.Append(')');
        }

        string groupedSql = BuildTargetGroupSql(where.ToString());
        await using var countCommand = CreateListCommand(connection,
            $"SELECT COUNT(*) FROM ({groupedSql})", categoryCodes, matchingNameCodes, search);
        int totalCount = Convert.ToInt32(
            await countCommand.ExecuteScalarAsync(cancellationToken), CultureInfo.InvariantCulture);
        if (totalCount == 0) return new([], page, pageSize, 0);

        string pageSql = $"""
            SELECT TargetType, TargetCode, TargetName, ReportCount, ModifiedBy, ModifiedAt
            FROM (
                SELECT groups.*, ROW_NUMBER() OVER (
                    ORDER BY ModifiedAt DESC, TargetType, TargetCode) AS RowNumber
                FROM ({groupedSql}) groups
            ) WHERE RowNumber BETWEEN :StartRow AND :EndRow
            """;
        await using var command = CreateListCommand(
            connection, pageSql, categoryCodes, matchingNameCodes, search);
        command.Parameters.Add("StartRow", OracleDbType.Int32, (page - 1) * pageSize + 1,
            System.Data.ParameterDirection.Input);
        command.Parameters.Add("EndRow", OracleDbType.Int32, page * pageSize,
            System.Data.ParameterDirection.Input);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<PermissionTargetListRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new(
                Convert.ToInt32(reader["TargetType"], CultureInfo.InvariantCulture),
                Convert.ToString(reader["TargetCode"], CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToString(reader["TargetName"], CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToInt32(reader["ReportCount"], CultureInfo.InvariantCulture),
                Convert.ToString(reader["ModifiedBy"], CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToDateTime(reader["ModifiedAt"], CultureInfo.InvariantCulture)));
        }
        return new(items, page, pageSize, totalCount);
    }

    internal static string BuildTargetGroupSql(string source) => $"""
            SELECT grantRow.NPERMISSIONTYPE AS TargetType,
                   RTRIM(grantRow.VCHPERMISSIONTARGET) AS TargetCode,
                   MAX(NVL(person.TargetName, department.TargetName)) AS TargetName,
                   COUNT(DISTINCT grantRow.NMENUCODE) AS ReportCount,
                   MAX(RTRIM(NVL(grantRow.VCHRECID, grantRow.VCHCUSRID)))
                       KEEP (DENSE_RANK LAST ORDER BY NVL(grantRow.DRECDATE, grantRow.DCDATE)) AS ModifiedBy,
                   MAX(NVL(grantRow.DRECDATE, grantRow.DCDATE)) AS ModifiedAt
            {source}
            GROUP BY grantRow.NPERMISSIONTYPE, RTRIM(grantRow.VCHPERMISSIONTARGET)
            """;

    public async Task<IReadOnlyList<PermissionTargetOption>> SearchTargetsAsync(
        int targetType, string search, CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateSearchTargetsCommand(connection, targetType, search);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var items = new List<PermissionTargetOption>();
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new(
                Convert.ToString(reader["Code"], CultureInfo.InvariantCulture) ?? string.Empty,
                Convert.ToString(reader["Name"], CultureInfo.InvariantCulture) ?? string.Empty));
        }
        return items;
    }

    internal static OracleCommand CreateSearchTargetsCommand(
        OracleConnection connection, int targetType, string search)
    {
        var command = new OracleCommand(targetType == 1 ? SearchDepartmentsSql : SearchPeopleSql,
            connection) { BindByName = true };
        command.Parameters.Add("Search", OracleDbType.Varchar2, $"%{EscapeLike(search.ToUpperInvariant())}%",
            System.Data.ParameterDirection.Input);
        return command;
    }

    private static OracleCommand CreateListCommand(
        OracleConnection connection, string sql, IReadOnlyList<string> categoryCodes,
        IReadOnlyList<string> matchingNameCodes, string search)
    {
        var command = new OracleCommand(sql, connection) { BindByName = true };
        for (int index = 0; index < categoryCodes.Count; index++)
            command.Parameters.Add($"Code{index}", OracleDbType.Varchar2, categoryCodes[index],
                System.Data.ParameterDirection.Input);
        if (search.Length > 0)
        {
            command.Parameters.Add("Search", OracleDbType.Varchar2,
                $"%{EscapeLike(search.ToUpperInvariant())}%", System.Data.ParameterDirection.Input);
            for (int index = 0; index < matchingNameCodes.Count; index++)
                command.Parameters.Add($"NameCode{index}", OracleDbType.Varchar2,
                    matchingNameCodes[index], System.Data.ParameterDirection.Input);
        }
        return command;
    }

    private static string EscapeLike(string value) => value.Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

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
        string sql = targetType == 1 ? ValidateDepartmentSql : ValidatePersonSql;
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

    public async Task SaveGrantsIfUnchangedAsync(
        int targetType, string target, IReadOnlyList<PermissionChangeItem> grants,
        string actor, CancellationToken cancellationToken = default,
        string? sourceUserId = null)
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

        if (sourceUserId is not null)
        {
            foreach (PermissionChangeItem grant in grants)
            {
                await using var source = new OracleCommand("""
                    SELECT NBTNPERMISSION FROM OpdAccrReportPermissionTbl
                    WHERE NMENUCODE = :ReportCode AND NPERMISSIONTYPE = 0
                      AND RTRIM(VCHPERMISSIONTARGET) = :SourceUserId AND VCHSTAT IS NULL
                    """, connection) { BindByName = true, Transaction = transaction };
                source.Parameters.Add("ReportCode", OracleDbType.Varchar2, grant.ReportCode,
                    System.Data.ParameterDirection.Input);
                source.Parameters.Add("SourceUserId", OracleDbType.Varchar2, sourceUserId,
                    System.Data.ParameterDirection.Input);
                object? value = await source.ExecuteScalarAsync(cancellationToken);
                if (value is null or DBNull
                    || Convert.ToInt32(value, CultureInfo.InvariantCulture) != grant.ActionMask)
                    throw new PermissionConflictException();
            }
        }

        var currentMasks = new Dictionary<string, int?>(StringComparer.Ordinal);
        foreach (PermissionChangeItem grant in grants)
        {
            await using var current = new OracleCommand("""
                SELECT NBTNPERMISSION FROM OpdAccrReportPermissionTbl
                WHERE NMENUCODE = :ReportCode AND NPERMISSIONTYPE = :TargetType
                  AND RTRIM(VCHPERMISSIONTARGET) = :Target AND VCHSTAT IS NULL
                """, connection) { BindByName = true, Transaction = transaction };
            current.Parameters.Add("ReportCode", OracleDbType.Varchar2, grant.ReportCode,
                System.Data.ParameterDirection.Input);
            current.Parameters.Add("TargetType", OracleDbType.Int32, targetType,
                System.Data.ParameterDirection.Input);
            current.Parameters.Add("Target", OracleDbType.Varchar2, target,
                System.Data.ParameterDirection.Input);
            object? value = await current.ExecuteScalarAsync(cancellationToken);
            int? mask = value is null or DBNull ? null : Convert.ToInt32(value, CultureInfo.InvariantCulture);
            if (mask != grant.ExpectedMask) throw new PermissionConflictException();
            currentMasks.Add(grant.ReportCode, mask);
        }

        foreach (PermissionChangeItem grant in grants)
        {
            if (grant.Revoke)
            {
                await using var revoke = new OracleCommand("""
                    UPDATE OpdAccrReportPermissionTbl
                    SET VCHSTAT = 'DC', DDCDATE = SYSDATE, VCHDCUSERID = :Actor
                    WHERE NMENUCODE = :ReportCode AND NPERMISSIONTYPE = :TargetType
                      AND RTRIM(VCHPERMISSIONTARGET) = :Target AND VCHSTAT IS NULL
                      AND NBTNPERMISSION = :ExpectedMask
                    """, connection) { BindByName = true, Transaction = transaction };
                revoke.Parameters.Add("Actor", OracleDbType.Varchar2, actor,
                    System.Data.ParameterDirection.Input);
                revoke.Parameters.Add("ReportCode", OracleDbType.Varchar2, grant.ReportCode,
                    System.Data.ParameterDirection.Input);
                revoke.Parameters.Add("TargetType", OracleDbType.Int32, targetType,
                    System.Data.ParameterDirection.Input);
                revoke.Parameters.Add("Target", OracleDbType.Varchar2, target,
                    System.Data.ParameterDirection.Input);
                revoke.Parameters.Add("ExpectedMask", OracleDbType.Int32, grant.ExpectedMask!.Value,
                    System.Data.ParameterDirection.Input);
                if (await revoke.ExecuteNonQueryAsync(cancellationToken) != 1)
                    throw new PermissionConflictException();
                continue;
            }
            if (currentMasks[grant.ReportCode] == grant.ActionMask) continue;
            await using var update = CreateWriteCommand(UpdateGrantSql, connection, transaction,
                grant.ReportCode, targetType, target, grant.ActionMask, actor);
            int updated = await update.ExecuteNonQueryAsync(cancellationToken);
            if (updated > 1) throw new InvalidOperationException("Duplicate permission rows found.");
            if (updated == 1) continue;
            await using var insert = CreateWriteCommand(InsertGrantSql, connection, transaction,
                grant.ReportCode, targetType, target, grant.ActionMask, actor);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
        transaction.Commit();
    }

    public async Task RevokeTargetIfUnchangedAsync(
        PermissionDeleteTargetRequest request, string actor,
        CancellationToken cancellationToken = default)
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

        await using (var current = new OracleCommand("""
            SELECT NMENUCODE, NBTNPERMISSION FROM OpdAccrReportPermissionTbl
            WHERE NPERMISSIONTYPE = :TargetType
              AND RTRIM(VCHPERMISSIONTARGET) = :Target AND VCHSTAT IS NULL
            """, connection) { BindByName = true, Transaction = transaction })
        {
            current.Parameters.Add("TargetType", OracleDbType.Int32, request.TargetType,
                System.Data.ParameterDirection.Input);
            current.Parameters.Add("Target", OracleDbType.Varchar2, request.Target,
                System.Data.ParameterDirection.Input);
            await using var reader = await current.ExecuteReaderAsync(cancellationToken);
            var actual = new Dictionary<string, int>(StringComparer.Ordinal);
            while (await reader.ReadAsync(cancellationToken))
            {
                string code = Convert.ToString(reader["NMENUCODE"], CultureInfo.InvariantCulture) ?? string.Empty;
                int mask = Convert.ToInt32(reader["NBTNPERMISSION"], CultureInfo.InvariantCulture);
                if (!actual.TryAdd(code, mask)) throw new PermissionConflictException();
            }
            if (actual.Count != request.ExpectedMasks.Count || actual.Any(pair =>
                !request.ExpectedMasks.TryGetValue(pair.Key, out int expected) || expected != pair.Value))
                throw new PermissionConflictException();
        }

        await using var revoke = new OracleCommand("""
            UPDATE OpdAccrReportPermissionTbl
            SET VCHSTAT = 'DC', DDCDATE = SYSDATE, VCHDCUSERID = :Actor
            WHERE NPERMISSIONTYPE = :TargetType
              AND RTRIM(VCHPERMISSIONTARGET) = :Target AND VCHSTAT IS NULL
            """, connection) { BindByName = true, Transaction = transaction };
        revoke.Parameters.Add("Actor", OracleDbType.Varchar2, actor,
            System.Data.ParameterDirection.Input);
        revoke.Parameters.Add("TargetType", OracleDbType.Int32, request.TargetType,
            System.Data.ParameterDirection.Input);
        revoke.Parameters.Add("Target", OracleDbType.Varchar2, request.Target,
            System.Data.ParameterDirection.Input);
        if (await revoke.ExecuteNonQueryAsync(cancellationToken) != request.ExpectedMasks.Count)
            throw new PermissionConflictException();
        transaction.Commit();
    }

    public async Task RevokeGrantIfUnchangedAsync(
        PermissionRevokeRequest request, string actor,
        CancellationToken cancellationToken = default)
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

        await using var command = new OracleCommand("""
            UPDATE OpdAccrReportPermissionTbl
            SET VCHSTAT = 'DC', DDCDATE = SYSDATE, VCHDCUSERID = :Actor
            WHERE NMENUCODE = :ReportCode AND NPERMISSIONTYPE = :TargetType
              AND RTRIM(VCHPERMISSIONTARGET) = :Target
              AND VCHSTAT IS NULL AND NBTNPERMISSION = :ExpectedMask
            """, connection) { BindByName = true, Transaction = transaction };
        command.Parameters.Add("Actor", OracleDbType.Varchar2, actor,
            System.Data.ParameterDirection.Input);
        command.Parameters.Add("ReportCode", OracleDbType.Varchar2, request.ReportCode,
            System.Data.ParameterDirection.Input);
        command.Parameters.Add("TargetType", OracleDbType.Int32, request.TargetType,
            System.Data.ParameterDirection.Input);
        command.Parameters.Add("Target", OracleDbType.Varchar2, request.Target,
            System.Data.ParameterDirection.Input);
        command.Parameters.Add("ExpectedMask", OracleDbType.Int32, request.ExpectedMask,
            System.Data.ParameterDirection.Input);
        if (await command.ExecuteNonQueryAsync(cancellationToken) != 1)
            throw new PermissionConflictException();
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
