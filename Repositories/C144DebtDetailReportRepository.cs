using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C144DebtDetailReportRepository(
    IConnectionStringProvider connectionStringProvider) : IC144DebtDetailReportRepository
{
    internal const int CommandTimeoutSeconds = 600;

    public int GetCount(C144Query query, CancellationToken cancellationToken = default)
    {
        using OracleConnection connection = OpenConnection();
        using OracleCommand command = CreateCommand(
            C144Sql.Count(Projection(query.Source)), connection, query);
        object? value = command.ExecuteScalarAsync(cancellationToken).GetAwaiter().GetResult();
        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    public List<C144DebtDetailReportViewModel> GetPage(
        C144Query query, int offset, int pageSize, CancellationToken cancellationToken = default)
    {
        using OracleConnection connection = OpenConnection();
        using OracleCommand command = CreateCommand(
            C144Sql.Page(Projection(query.Source)), connection, query);
        command.Parameters.Add(Input("RowOffset", OracleDbType.Int32, offset));
        command.Parameters.Add(Input("PageSize", OracleDbType.Int32, pageSize));
        return ReadRows(command, cancellationToken);
    }

    public List<C144DebtDetailReportViewModel> GetAll(
        C144Query query, CancellationToken cancellationToken = default)
    {
        using OracleConnection connection = OpenConnection();
        using OracleCommand command = CreateCommand(
            C144Sql.All(Projection(query.Source)), connection, query);
        return ReadRows(command, cancellationToken);
    }

    private OracleConnection OpenConnection()
    {
        var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        connection.Open();
        return connection;
    }

    internal static OracleCommand CreateCommand(
        string sql, OracleConnection connection, C144Query query)
    {
        var command = new OracleCommand(sql, connection)
        {
            BindByName = true,
            CommandTimeout = CommandTimeoutSeconds
        };
        command.Parameters.Add(Char("StartDate", query.StartDate));
        command.Parameters.Add(Char("EndDate", query.EndDate));
        return command;
    }

    private static string Projection(string source) => source switch
    {
        C144Sources.OpdEr => C144Sql.OutpatientEmergencyProjection,
        C144Sources.Inpatient => C144Sql.InpatientProjection,
        _ => throw new ArgumentException("C144 資料來源不正確。", nameof(source))
    };

    private static List<C144DebtDetailReportViewModel> ReadRows(
        OracleCommand command, CancellationToken cancellationToken)
    {
        using OracleDataReader reader = command.ExecuteReaderAsync(cancellationToken)
            .GetAwaiter().GetResult();
        var rows = new List<C144DebtDetailReportViewModel>();
        while (reader.Read()) rows.Add(ReadRow(reader));
        return rows;
    }

    internal static C144DebtDetailReportViewModel ReadRow(OracleDataReader reader) => new()
    {
        EncounterType = Text(reader, "EncounterType"),
        VisitDate = Text(reader, "VisitDate"),
        VisitTime = Text(reader, "VisitTime"),
        RoomNumber = Text(reader, "RoomNumber"),
        SequenceNumber = Number(reader, "SequenceNumber"),
        MedicalRecordNumber = Text(reader, "MedicalRecordNumber"),
        PatientName = Text(reader, "PatientName"),
        DischargeDate = Text(reader, "DischargeDate"),
        SectionCode = Text(reader, "SectionCode"),
        SectionName = Text(reader, "SectionName"),
        DoctorId = Text(reader, "DoctorId"),
        DoctorName = Text(reader, "DoctorName"),
        PatientIdentity = Text(reader, "PatientIdentity"),
        OutstandingAmount = Number(reader, "OutstandingAmount"),
        TotalSelfPayAmount = Number(reader, "TotalSelfPayAmount"),
        GeneralSelfPayAmount = Number(reader, "GeneralSelfPayAmount"),
        InsuredSelfPayAmount = Number(reader, "InsuredSelfPayAmount"),
        CopaymentAmount = Number(reader, "CopaymentAmount"),
        DrugCopaymentAmount = Number(reader, "DrugCopaymentAmount"),
        DiscountAmount = Number(reader, "DiscountAmount"),
        OnAccountAmount = Number(reader, "OnAccountAmount"),
        GeneralMaterialAmount = Number(reader, "GeneralMaterialAmount"),
        InsuredMaterialAmount = Number(reader, "InsuredMaterialAmount"),
        GeneralSurgeryAmount = Number(reader, "GeneralSurgeryAmount"),
        InsuredSurgeryAmount = Number(reader, "InsuredSurgeryAmount"),
        GeneralAnesthesiaAmount = Number(reader, "GeneralAnesthesiaAmount"),
        InsuredAnesthesiaAmount = Number(reader, "InsuredAnesthesiaAmount"),
        GeneralDrugAmount = Number(reader, "GeneralDrugAmount"),
        InsuredDrugAmount = Number(reader, "InsuredDrugAmount"),
        GeneralRegistrationAmount = Number(reader, "GeneralRegistrationAmount"),
        InsuredRegistrationAmount = Number(reader, "InsuredRegistrationAmount")
    };

    private static string? Text(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static decimal? Number(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static OracleParameter Char(string name, string value)
    {
        OracleParameter parameter = Input(name, OracleDbType.Char, value);
        parameter.Size = 7;
        return parameter;
    }

    private static OracleParameter Input(string name, OracleDbType type, object value) =>
        new(name, type) { Direction = ParameterDirection.Input, Value = value };
}
