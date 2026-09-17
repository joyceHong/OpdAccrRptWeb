using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C3ReportRepository(IConnectionStringProvider connectionStringProvider) : IC3ReportRepository
{
    public async Task<IReadOnlyList<C3MovementRow>> QueryDayAsync(C3ValidatedRequest request,
        string runDate, DepartmentFilterMode departmentMode, CancellationToken token = default)
    {
        string sql = C3Sql.Get(request.Source, departmentMode);
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(token);
        await using var command = new OracleCommand(sql, connection) { BindByName = true, CommandTimeout = 60 };
        AddParameters(command, request, runDate, departmentMode);
        var rows = new List<C3MovementRow>();
        await using OracleDataReader reader = await command.ExecuteReaderAsync(token);
        while (await reader.ReadAsync(token)) rows.Add(Read(reader, runDate));
        return rows;
    }

    internal static void AddParameters(OracleCommand command, C3ValidatedRequest request,
        string runDate, DepartmentFilterMode departmentMode)
    {
        Add(command, "run_date", OracleDbType.Char, 7, runDate);
        Add(command, "day_begin", OracleDbType.Char, 11, runDate + "0000");
        Add(command, "day_end", OracleDbType.Char, 11, runDate + "9999");
        command.Parameters.Add(new OracleParameter("logistics_type", OracleDbType.Int32,
            (int)request.LogisticsType, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("detail_type", OracleDbType.Int32,
            (int)request.DetailType, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("has_room_filter", OracleDbType.Int32,
            request.RoomCodes.Count == 0 ? 0 : 1, ParameterDirection.Input));
        command.Parameters.Add(new OracleParameter("has_charge_filter", OracleDbType.Int32,
            request.ChargeCodes.Count == 0 ? 0 : 1, ParameterDirection.Input));
        if (request.Source == CareSource.I || departmentMode is DepartmentFilterMode.Station
            or DepartmentFilterMode.GeneralLocation)
        {
            AddNullable(command, "department_code", 10, request.DepartmentCode);
        }
        for (int i = 0; i < 50; i++)
        {
            AddNullable(command, $"room_{i + 1:00}", 20, i < request.RoomCodes.Count ? request.RoomCodes[i] : null);
            AddNullable(command, $"charge_{i + 1:00}", 30, i < request.ChargeCodes.Count ? request.ChargeCodes[i] : null);
        }
    }

    private static void Add(OracleCommand command, string name, OracleDbType type, int size, object value) =>
        command.Parameters.Add(new OracleParameter(name, type, size, value, ParameterDirection.Input));
    private static void AddNullable(OracleCommand command, string name, int size, string? value) =>
        Add(command, name, OracleDbType.Varchar2, size, value is null ? DBNull.Value : value);

    private static C3MovementRow Read(OracleDataReader r, string runDate) => new(
        Convert.ToInt32(r["movement_type"], CultureInfo.InvariantCulture), runDate,
        Text(r, "chOp1Room"), Text(r, "chStation"), Text(r, "chOp4OrdNo"), Text(r, "chOp1Sec"),
        Text(r, "chOp4Sys"), NullableText(r, "chOp4Dct"), NullableText(r, "chOp1MrNo"),
        NullableText(r, "chOp1PName"), NullableText(r, "chOp1DrName"), NullableText(r, "chOp4SPay"),
        Text(r, "chOrdCName"), Text(r, "chOrdInv"), Text(r, "chOrdInvAdd"),
        NullableText(r, "chOp4Proj"), NullableText(r, "chOp1Clamc"),
        Convert.ToDecimal(r["rlOp4OrdTot"], CultureInfo.InvariantCulture));

    private static string Text(OracleDataReader r, string name) => NullableText(r, name) ?? string.Empty;
    private static string? NullableText(OracleDataReader r, string name)
    {
        int index = r.GetOrdinal(name);
        return r.IsDBNull(index) ? null : Convert.ToString(r.GetValue(index), CultureInfo.InvariantCulture)?.Trim();
    }
}
