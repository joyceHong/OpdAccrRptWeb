using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C7ReportRepository(IConnectionStringProvider connectionStrings) : IC7ReportRepository
{
    internal const int CommandTimeoutSeconds = 60;
    public async Task<IReadOnlyList<C7InputUser>> GetEligibleUsersAsync(string rocEndDate, CancellationToken token = default)
    {
        await using var connection = new OracleConnection(connectionStrings.GetConnectionString());
        await connection.OpenAsync(token);
        await using var command = new OracleCommand(C7Sql.UserLookup, connection) { BindByName = true, CommandTimeout = CommandTimeoutSeconds };
        command.Parameters.Add(new OracleParameter("end_date", OracleDbType.Char, 7, rocEndDate, ParameterDirection.Input));
        var rows = new List<C7InputUser>();
        await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, token);
        while (await reader.ReadAsync(token)) rows.Add(new(Text(reader, "chUserID"), Text(reader, "chUserName")));
        return rows.OrderBy(x => x.UserId, StringComparer.Ordinal).ToList();
    }
    public async Task<IC7ReportQuerySession> OpenSessionAsync(CancellationToken token = default)
    {
        var connection = new OracleConnection(connectionStrings.GetConnectionString());
        await connection.OpenAsync(token);
        return new Session(connection);
    }
    private sealed class Session(OracleConnection connection) : IC7ReportQuerySession
    {
        public async Task<IReadOnlyList<C7SourceRow>> QueryDayAsync(C7ValidatedRequest request, string runDate, CancellationToken token = default)
        {
            await using var command = new OracleCommand(request.ChargeKind == C7ChargeKind.Drug ? C7Sql.DrugDetail : C7Sql.OrderDetail, connection) { BindByName = true, CommandTimeout = CommandTimeoutSeconds };
            Add(command, "run_date", OracleDbType.Char, 7, runDate); Add(command, "start_time", OracleDbType.Char, 4, request.StartTime);
            Add(command, "end_time", OracleDbType.Char, 4, request.EndTime); Add(command, "input_user_id", OracleDbType.Char, 10, (object?)request.InputUserId ?? DBNull.Value);
            var rows = new List<C7SourceRow>();
            await using var reader = await command.ExecuteReaderAsync(CommandBehavior.SequentialAccess, token);
            while (await reader.ReadAsync(token)) rows.Add(new(T(reader,"chOp4PFin1"),T(reader,"chOp4PSec"),T(reader,"chOp4OrdNo"),T(reader,"chOp4OrdName"),T(reader,"chOp4SPay"),T(reader,"chOp4Stat"),N(reader,"rlOp4OrdTot"),T(reader,"chOp4CUser"),N(reader,"rlOp4Pric1"),N(reader,"rlOp4Pric2"),N(reader,"rlOp4AMT1"),N(reader,"rlOp4AMT2"),T(reader,"chUserName"),T(reader,"chOp1Date"),T(reader,"chOp1Time"),T(reader,"chOp1MrNo"),T(reader,"chOp1RoomType")));
            return rows;
        }
        public ValueTask DisposeAsync() => connection.DisposeAsync();
    }
    private static void Add(OracleCommand c,string n,OracleDbType t,int s,object v)=>c.Parameters.Add(new OracleParameter(n,t,s,v,ParameterDirection.Input));
    private static string Text(OracleDataReader r,string n)=>T(r,n)??string.Empty;
    private static string? T(OracleDataReader r,string n){int i=r.GetOrdinal(n);return r.IsDBNull(i)?null:Convert.ToString(r.GetValue(i),CultureInfo.InvariantCulture)?.Trim();}
    private static decimal? N(OracleDataReader r,string n){int i=r.GetOrdinal(n);return r.IsDBNull(i)?null:Convert.ToDecimal(r.GetValue(i),CultureInfo.InvariantCulture);}
}
