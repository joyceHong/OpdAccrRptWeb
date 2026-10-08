using System.Data;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class SapInterfaceRepository(IConnectionStringProvider connectionStringProvider) : ISapInterfaceRepository
{
    private static readonly string[] EventCodes = ["SAPCASH", "SAPCONS", "SAPACC", "SAPREV2"];

    public async Task<string?> GetDefaultRocDateAsync(CancellationToken cancellationToken)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = CreateCommand(connection, null, SapInterfaceSqlQueries.DefaultDate);
        object? value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? null : Convert.ToString(value);
    }

    public async Task<IReadOnlyDictionary<string, bool>> GetCompletedAsync(string rocDate, CancellationToken cancellationToken)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        var completed = new Dictionary<string, bool>(StringComparer.Ordinal);
        foreach (string eventCode in EventCodes)
        {
            await using var command = CreateCommand(connection, null, SapInterfaceSqlQueries.CheckLog, rocDate, rocDate, eventCode);
            completed[eventCode] = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) > 0;
        }
        return completed;
    }

    public async Task<SapInterfaceRepositoryRunResult> RunEventAsync(string eventCode, string rocDate,
        string gregorianDate,
        bool confirmRerun, CancellationToken cancellationToken)
    {
        EventStatements statements = GetEventStatements(eventCode);

        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            // Lock before checking logday, including dates with no row yet. This serializes
            // concurrent SAP runs across processes until this event commits.
            _ = await ExecuteAsync(connection, transaction, "LOCK TABLE logday IN EXCLUSIVE MODE WAIT 30",
                rocDate, gregorianDate, eventCode, cancellationToken);
            await using var check = CreateCommand(connection, transaction, SapInterfaceSqlQueries.CheckLog,
                rocDate, gregorianDate, eventCode);
            bool completed = Convert.ToInt32(await check.ExecuteScalarAsync(cancellationToken)) > 0;
            if (completed && !confirmRerun)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new(SapInterfaceRepositoryRunStatus.ConfirmationRequired, 0);
            }

            if (completed)
                _ = await ExecuteAsync(connection, transaction, SapInterfaceSqlQueries.DeleteLog,
                    rocDate, gregorianDate, eventCode, cancellationToken);
            _ = await ExecuteAsync(connection, transaction, statements.DeleteSql,
                rocDate, gregorianDate, eventCode, cancellationToken);
            var inserts = statements.InsertSql.Select(statement =>
                (Func<CancellationToken, Task<int>>)(token => ExecuteAsync(connection, transaction, statement,
                    rocDate, gregorianDate, eventCode, token))).ToArray();
            return await SapInterfaceEventTransaction.ExecuteAsync(inserts,
                async token =>
                {
                    _ = await ExecuteAsync(connection, transaction, SapInterfaceSqlQueries.InsertLog,
                        rocDate, gregorianDate, eventCode, token);
                },
                token => transaction.CommitAsync(token),
                token => transaction.RollbackAsync(token), cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    internal static int SumInsertedRowCounts(IEnumerable<int> rowCounts)
    {
        int total = 0;
        foreach (int count in rowCounts)
        {
            if (count < 0)
                throw new InvalidOperationException("Oracle did not return an affected-row count for a SAP INSERT.");
            total = checked(total + count);
        }
        return total;
    }

    internal static EventStatements GetEventStatements(string eventCode) => eventCode switch
    {
        "SAPCASH" => new(SapInterfaceSqlQueries.DeleteCash, [SapInterfaceSqlQueries.InsertCash]),
        "SAPCONS" => new(SapInterfaceSqlQueries.DeleteContract, [SapInterfaceSqlQueries.InsertContract]),
        "SAPACC" => new(SapInterfaceSqlQueries.DeleteAcc, [SapInterfaceSqlQueries.InsertAccOutpatient,
            SapInterfaceSqlQueries.InsertAccInpatient]),
        "SAPREV2" => new(SapInterfaceSqlQueries.DeleteRev, [SapInterfaceSqlQueries.InsertRevCharges,
            SapInterfaceSqlQueries.InsertRevInpatientDiscount, SapInterfaceSqlQueries.InsertRevOutpatientDiscount]),
        _ => throw new ArgumentOutOfRangeException(nameof(eventCode))
    };

    private static async Task<int> ExecuteAsync(OracleConnection connection, OracleTransaction transaction,
        string sql, string rocDate, string gregorianDate, string eventCode, CancellationToken cancellationToken)
    {
        await using var command = CreateCommand(connection, transaction, sql, rocDate, gregorianDate, eventCode);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static OracleCommand CreateCommand(OracleConnection connection, OracleTransaction? transaction,
        string sql, string? rocDate = null, string? gregorianDate = null, string? eventCode = null)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.BindByName = true;
        command.CommandTimeout = 1000;
        if (sql.Contains(":start_roc", StringComparison.Ordinal))
            command.Parameters.Add("start_roc", OracleDbType.Varchar2).Value = rocDate;
        if (sql.Contains(":end_roc", StringComparison.Ordinal))
            command.Parameters.Add("end_roc", OracleDbType.Varchar2).Value = rocDate;
        if (sql.Contains(":work_roc", StringComparison.Ordinal))
            command.Parameters.Add("work_roc", OracleDbType.Varchar2).Value = rocDate;
        if (sql.Contains(":start_gregorian", StringComparison.Ordinal))
            command.Parameters.Add("start_gregorian", OracleDbType.Varchar2).Value = gregorianDate;
        if (sql.Contains(":end_gregorian", StringComparison.Ordinal))
            command.Parameters.Add("end_gregorian", OracleDbType.Varchar2).Value = gregorianDate;
        if (sql.Contains(":event", StringComparison.Ordinal))
            command.Parameters.Add("event", OracleDbType.Varchar2).Value = eventCode;
        return command;
    }

    internal sealed record EventStatements(string DeleteSql, string[] InsertSql);
}

internal static class SapInterfaceEventTransaction
{
    internal static async Task<SapInterfaceRepositoryRunResult> ExecuteAsync(
        IReadOnlyList<Func<CancellationToken, Task<int>>> insertStatements,
        Func<CancellationToken, Task> insertLog,
        Func<CancellationToken, Task> commit,
        Func<CancellationToken, Task> rollback,
        CancellationToken cancellationToken)
    {
        var rowCounts = new int[insertStatements.Count];
        for (int index = 0; index < insertStatements.Count; index++)
            rowCounts[index] = await insertStatements[index](cancellationToken);

        int insertedRows = SapInterfaceRepository.SumInsertedRowCounts(rowCounts);
        if (insertedRows == 0)
        {
            await rollback(CancellationToken.None);
            return new(SapInterfaceRepositoryRunStatus.NoData, insertedRows);
        }

        await insertLog(cancellationToken);
        await commit(cancellationToken);
        return new(SapInterfaceRepositoryRunStatus.Completed, insertedRows);
    }
}
