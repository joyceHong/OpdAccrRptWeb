using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class M2DoctorMonthlyReportRepository(IConnectionStringProvider connectionStrings)
    : IM2DoctorMonthlyReportRepository
{
    internal const int CommandTimeoutSeconds = 120;

    public Task<IReadOnlyList<M2DoctorMonthlySourceRow>> QueryStatisticsAsync(string rocMonth,
        M2VisitScope visitScope, M2TimeSlot timeSlot, CancellationToken cancellationToken = default) =>
        QueryAsync(M2DoctorMonthlySql.Statistics, rocMonth, null, visitScope, timeSlot, cancellationToken);

    public Task<IReadOnlyList<M2DoctorMonthlySourceRow>> QueryActualVisitDayAsync(string rocMonth,
        int day, M2VisitScope visitScope, M2TimeSlot timeSlot,
        CancellationToken cancellationToken = default) =>
        QueryAsync(M2DoctorMonthlySql.SelectActualVisit(rocMonth), rocMonth, day,
            visitScope, timeSlot, cancellationToken);

    private async Task<IReadOnlyList<M2DoctorMonthlySourceRow>> QueryAsync(string sql,
        string rocMonth, int? day, M2VisitScope visitScope, M2TimeSlot timeSlot,
        CancellationToken cancellationToken)
    {
        Validate(rocMonth, day, visitScope, timeSlot);
        await using var connection = new OracleConnection(connectionStrings.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new OracleCommand(sql, connection)
        {
            BindByName = true,
            CommandTimeout = CommandTimeoutSeconds
        };
        command.Parameters.Add("report_month", OracleDbType.Char, 5).Value = rocMonth;
        if (day.HasValue) command.Parameters.Add("day", OracleDbType.Char, 2).Value = day.Value.ToString("00");
        command.Parameters.Add("visit_scope", OracleDbType.Varchar2, 12).Value = Bind(visitScope);
        command.Parameters.Add("time_slot", OracleDbType.Varchar2, 12).Value = Bind(timeSlot);

        var rows = new List<M2DoctorMonthlySourceRow>();
        await using OracleDataReader reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new(Text(reader, "chSecNo"), Text(reader, "chSecName"),
                Text(reader, "chODrId"), Text(reader, "chDocName"),
                Number(reader, "report_day"), Number(reader, "DayTotalNumber")));
        return rows;
    }

    internal static string Bind(M2VisitScope value) => value switch
    {
        M2VisitScope.All => "ALL", M2VisitScope.Outpatient => "OUTPATIENT",
        M2VisitScope.Emergency => "EMERGENCY", _ => throw new ArgumentOutOfRangeException(nameof(value))
    };
    internal static string Bind(M2TimeSlot value) => value switch
    {
        M2TimeSlot.All => "ALL", M2TimeSlot.Morning => "MORNING",
        M2TimeSlot.Afternoon => "AFTERNOON", M2TimeSlot.Night => "NIGHT",
        _ => throw new ArgumentOutOfRangeException(nameof(value))
    };

    private static void Validate(string rocMonth, int? day, M2VisitScope visitScope, M2TimeSlot timeSlot)
    {
        if (rocMonth.Length != 5 || !rocMonth.All(char.IsAsciiDigit))
            throw new ArgumentException("M2 repository 月份必須為民國五碼。", nameof(rocMonth));
        if (day is < 1 or > 31) throw new ArgumentOutOfRangeException(nameof(day));
        _ = Bind(visitScope); _ = Bind(timeSlot);
    }
    private static string Text(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal) ? string.Empty :
            Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.Trim() ?? string.Empty;
    }
    private static int Number(OracleDataReader reader, string name) => checked(Convert.ToInt32(
        reader.GetValue(reader.GetOrdinal(name)), CultureInfo.InvariantCulture));
}
