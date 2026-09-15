using System.Data;
using System.Globalization;
using System.Text;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C13HighRiskEmergencyRepository(
    IConnectionStringProvider connectionStringProvider,
    IC13LegacyPhoneMasker phoneMasker) : IC13HighRiskEmergencyRepository
{
    private const int CommandTimeoutSeconds = 60;

    internal const string RangeProjection = """
        SELECT DISTINCT
               r.chOp0Date AS VisitDate,
               r.chOp0PMrNo AS MedicalRecordNumber,
               r.chOp0PName AS PatientName,
               f.chFin1Name AS IdentityName,
               p.chPPayName AS PartialPaymentName,
               r.chOp0BirDate AS BirthDate,
               r.chOp0PtID AS NationalId,
               m.chAdd1 AS Address,
               m.chTelH AS Telephone,
               s.chNowBedNo AS EmergencyBedNumber
        FROM OpdRegPtnTbl r
        JOIN OpdMRBasicTbl m ON m.chMrNo = RTRIM(r.chOp0PMrNo)
        JOIN GenFin1Tbl f ON f.chFin1No = RTRIM(r.chOp0Fin1)
        JOIN OpdPPayTbl p ON p.chPPayNo = r.chOp0PPay
        JOIN GenDebtTbl d
          ON d.chOp1Date = r.chOp0Date
         AND d.chOp1Time = r.chOp0Time
         AND d.chOp1Room = r.chOp0Room
         AND d.intOp1No = r.intOp0No
        LEFT JOIN OpdEmgSoapDispTbl s
          ON s.chRegDate = r.chOp0Date
         AND s.chRegTime = r.chOp0Time
         AND s.chRegRoom = r.chOp0Room
         AND s.intRegNo = r.intOp0No
        WHERE r.chOp0Date BETWEEN :StartDate AND :EndDate
          AND r.chOp0Room = '0000'
          AND r.chOp0PMrNo NOT IN ('C36979', '1000000')
          AND r.chOp0DC <> '1'
          AND (r.chOp0Fin1 IN ('01', '35')
               OR r.chOp0PPay = '004'
               OR r.chOp0PName LIKE '%無名氏%')
          AND RTRIM(d.chBackFlg) IS NULL
          AND d.rlDebtAmt > 0
        """;

    private const string SelectColumns = """
        VisitDate, MedicalRecordNumber, PatientName, IdentityName,
        PartialPaymentName, BirthDate, NationalId, Address, Telephone,
        EmergencyBedNumber
        """;

    private const string OrderBy =
        " ORDER BY VisitDate, IdentityName, PartialPaymentName, MedicalRecordNumber";

    public int GetCount(SearchReportCondition condition, CancellationToken cancellationToken = default)
    {
        QueryDefinition definition = BuildDefinition(condition);
        using var connection = OpenConnection();
        using OracleCommand command = CreateCommand(
            $"SELECT COUNT(*) FROM ({definition.ProjectionSql})", connection, definition);
        return Convert.ToInt32(command.ExecuteScalarAsync(cancellationToken).GetAwaiter().GetResult(),
            CultureInfo.InvariantCulture);
    }

    public List<C13HighRiskEmergencyReportViewModel> GetPage(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default)
    {
        QueryDefinition definition = BuildDefinition(condition);
        int pageNumber = condition.PageNumber ?? 1;
        int pageSize = condition.PageSize ?? 10;
        using var connection = OpenConnection();
        using OracleCommand command = CreateCommand(
            $"SELECT {SelectColumns} FROM ({definition.ProjectionSql}){OrderBy} OFFSET :RowOffset ROWS FETCH NEXT :PageSize ROWS ONLY",
            connection, definition);
        command.Parameters.Add(Input("RowOffset", OracleDbType.Int32, (pageNumber - 1) * pageSize));
        command.Parameters.Add(Input("PageSize", OracleDbType.Int32, pageSize));
        return ReadRows(command, cancellationToken);
    }

    public List<C13HighRiskEmergencyReportViewModel> GetAllForPreview(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default)
    {
        QueryDefinition definition = BuildDefinition(condition);
        using var connection = OpenConnection();
        using OracleCommand command = CreateCommand(
            $"SELECT {SelectColumns} FROM ({definition.ProjectionSql}){OrderBy}", connection, definition);
        return ReadRows(command, cancellationToken);
    }

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<C13HighRiskEmergencyReportViewModel>();

    internal static QueryDefinition BuildDefinition(SearchReportCondition condition)
    {
        if (!DateOnly.TryParseExact(condition.StartDate, "yyyy-MM-dd", out DateOnly start)
            || !DateOnly.TryParseExact(condition.EndDate, "yyyy-MM-dd", out DateOnly end)
            || start > end || start.Year < 1912)
            throw new ArgumentException("C13 日期格式或範圍不正確。");

        return new QueryDefinition(
            RangeProjection,
            $"{start.Year - 1911:000}{start:MMdd}",
            $"{end.Year - 1911:000}{end:MMdd}");
    }

    private OracleConnection OpenConnection()
    {
        var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        connection.Open();
        return connection;
    }

    private static OracleCommand CreateCommand(
        string sql,
        OracleConnection connection,
        QueryDefinition definition)
    {
        var command = new OracleCommand(sql, connection)
        {
            BindByName = true,
            CommandTimeout = CommandTimeoutSeconds
        };
        AddRangeParameters(command, definition);
        return command;
    }

    internal static void AddRangeParameters(OracleCommand command, QueryDefinition definition)
    {
        var startDate = Input("StartDate", OracleDbType.Char, definition.StartDate);
        startDate.Size = 7;
        command.Parameters.Add(startDate);
        var endDate = Input("EndDate", OracleDbType.Char, definition.EndDate);
        endDate.Size = 7;
        command.Parameters.Add(endDate);
    }

    private List<C13HighRiskEmergencyReportViewModel> ReadRows(
        OracleCommand command,
        CancellationToken cancellationToken)
    {
        using OracleDataReader reader = command.ExecuteReaderAsync(cancellationToken).GetAwaiter().GetResult();
        var rows = new List<C13HighRiskEmergencyReportViewModel>();
        while (reader.Read())
        {
            rows.Add(new C13HighRiskEmergencyReportViewModel
            {
                VisitDate = ReadText(reader, "VisitDate"),
                MedicalRecordNumber = ReadText(reader, "MedicalRecordNumber"),
                PatientName = ReadText(reader, "PatientName"),
                IdentityName = ReadText(reader, "IdentityName"),
                PartialPaymentName = ReadText(reader, "PartialPaymentName"),
                BirthDate = ReadText(reader, "BirthDate"),
                NationalId = ReadText(reader, "NationalId"),
                Address = ReadText(reader, "Address"),
                Telephone = phoneMasker.Mask(ReadText(reader, "Telephone")),
                EmergencyBedNumber = ReadText(reader, "EmergencyBedNumber")
            });
        }
        return rows;
    }

    private static string ReadText(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal)
            ? string.Empty
            : (Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture) ?? string.Empty).Trim();
    }

    private static OracleParameter Input(string name, OracleDbType type, object value) => new(name, type)
    {
        Direction = ParameterDirection.Input,
        Value = value
    };

    internal sealed record QueryDefinition(string ProjectionSql, string StartDate, string EndDate);
}
