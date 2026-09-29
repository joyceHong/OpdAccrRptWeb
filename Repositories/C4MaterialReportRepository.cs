using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Models;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C4MaterialReportRepository(IConnectionStringProvider connectionStringProvider)
    : IC4MaterialReportRepository
{
    internal const string DailySql = """
        SELECT
            DECODE(RTRIM(O.chOp1Room), '0000', 'E', 'R') AS RoomType,
            G.chOrdDetal,
            G.chOrdInv,
            G.chOrdHisACode,
            G.chOrdCName,
            G.chOrdUnit,
            O.chOp4OrdNo,
            O.chOp4PSec,
            SUM(O.rlOp4OrdTot) AS Tot
        FROM GenOrdBasicTbl G,
             OpdOrdTbl O
        WHERE O.chOp4IDate BETWEEN :run_date || '0000' AND :run_date || '9999'
          AND O.chOp4Stat <> 'DC'
          AND O.chOp4OrdNo = G.chOrdNo
          AND RTRIM(G.chOrdDetal) IS NOT NULL
          AND O.chOp4HinCls IN ('50', '99')
          AND (:section_prefix IS NULL OR O.chOp4PSec LIKE :section_prefix || '%')
        GROUP BY
            DECODE(RTRIM(O.chOp1Room), '0000', 'E', 'R'),
            G.chOrdDetal,
            G.chOrdInv,
            G.chOrdHisACode,
            G.chOrdCName,
            G.chOrdUnit,
            O.chOp4OrdNo,
            O.chOp4PSec
        ORDER BY
            O.chOp4PSec,
            G.chOrdInv
        """;

    public async Task<IReadOnlyList<C4MaterialSourceRow>> QueryDayAsync(string runDate,
        string? sectionPrefix, CancellationToken cancellationToken = default)
    {
        await using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        await connection.OpenAsync(cancellationToken);
        await using var command = new OracleCommand(DailySql, connection)
        {
            BindByName = true,
            CommandTimeout = 60
        };
        command.Parameters.Add("run_date", OracleDbType.Char, 7).Value = runDate;
        command.Parameters.Add("section_prefix", OracleDbType.Varchar2, 10).Value =
            sectionPrefix is null ? DBNull.Value : sectionPrefix;
        var rows = new List<C4MaterialSourceRow>();
        await using OracleDataReader reader = await command.ExecuteReaderAsync(
            CommandBehavior.SequentialAccess, cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            rows.Add(new(Text(reader, 0) ?? "R", Text(reader, 1), Text(reader, 2), Text(reader, 3),
                Text(reader, 4), Text(reader, 5), Text(reader, 6), Text(reader, 7),
                Convert.ToDecimal(reader.GetValue(8), CultureInfo.InvariantCulture)));
        return rows;
    }

    private static string? Text(OracleDataReader reader, int ordinal) => reader.IsDBNull(ordinal)
        ? null : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.Trim();
}
