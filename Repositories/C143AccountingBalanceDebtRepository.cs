using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C143AccountingBalanceDebtRepository(
    IConnectionStringProvider connectionStringProvider) : IC143AccountingBalanceDebtRepository
{
    internal const int CommandTimeoutSeconds = 600;

    public int GetOutpatientEmergencyCount(C143Query query, CancellationToken cancellationToken = default) =>
        ExecuteCount(C143Sql.OutpatientProjection, query, null, cancellationToken);

    public List<C143AccountingBalanceDebtReportViewModel> GetOutpatientEmergencyPage(
        C143Query query, int offset, int pageSize, CancellationToken cancellationToken = default) =>
        ExecutePage(C143Sql.OutpatientProjection, C143Sql.OutpatientOrderBy, query, null,
            offset, pageSize, C143ResultGroups.OutpatientEmergency, cancellationToken);

    public int GetInpatientCount(C143Query query, int dischargeGroup, CancellationToken cancellationToken = default) =>
        ExecuteCount(C143Sql.InpatientProjection, query, dischargeGroup, cancellationToken);

    public List<C143AccountingBalanceDebtReportViewModel> GetInpatientPage(
        C143Query query, int dischargeGroup, int offset, int pageSize,
        CancellationToken cancellationToken = default) =>
        ExecutePage(C143Sql.InpatientProjection, C143Sql.InpatientOrderBy, query, dischargeGroup,
            offset, pageSize, dischargeGroup == 1 ? C143ResultGroups.Discharged : C143ResultGroups.InHospital,
            cancellationToken);

    private int ExecuteCount(string projection, C143Query query, int? group, CancellationToken token)
    {
        using OracleConnection connection = OpenConnection();
        using OracleCommand command = CreateCommand(C143Sql.Count(projection), connection, query, group);
        return Convert.ToInt32(command.ExecuteScalarAsync(token).GetAwaiter().GetResult(), CultureInfo.InvariantCulture);
    }

    private List<C143AccountingBalanceDebtReportViewModel> ExecutePage(
        string projection, string orderBy, C143Query query, int? group,
        int offset, int pageSize, string resultGroup, CancellationToken token)
    {
        using OracleConnection connection = OpenConnection();
        using OracleCommand command = CreateCommand(C143Sql.Page(projection, orderBy), connection, query, group);
        command.Parameters.Add(Input("RowOffset", OracleDbType.Int32, offset));
        command.Parameters.Add(Input("PageSize", OracleDbType.Int32, pageSize));
        using OracleDataReader reader = command.ExecuteReaderAsync(token).GetAwaiter().GetResult();
        var rows = new List<C143AccountingBalanceDebtReportViewModel>();
        while (reader.Read()) rows.Add(ReadRow(reader, resultGroup));
        return rows;
    }

    private OracleConnection OpenConnection()
    {
        var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        connection.Open();
        return connection;
    }

    internal static OracleCommand CreateCommand(
        string sql, OracleConnection connection, C143Query query, int? dischargeGroup)
    {
        var command = new OracleCommand(sql, connection)
        {
            BindByName = true,
            CommandTimeout = CommandTimeoutSeconds
        };
        command.Parameters.Add(Char("StartDate", query.StartDate));
        command.Parameters.Add(Char("EndDate", query.EndDate));
        command.Parameters.Add(Char("ReportType", C143ReportTypes.ToLegacyValue(query.ReportType), 1));
        if (dischargeGroup.HasValue)
            command.Parameters.Add(Input("DischargeGroup", OracleDbType.Int32, dischargeGroup.Value));
        return command;
    }

    private static C143AccountingBalanceDebtReportViewModel ReadRow(OracleDataReader reader, string group) => new()
    {
        EncounterType = Text(reader, "EncounterType"),
        MedicalRecordNumber = Text(reader, "MedicalRecordNumber"),
        VisitDate = Text(reader, "VisitDate"),
        SequenceNumber = Number(reader, "SequenceNumber"),
        EmergencyDepartureDate = Text(reader, "EmergencyDepartureDate"),
        DischargeDate = Text(reader, "DischargeDate"),
        AccountingGeneralSelfPay = Number(reader, "AccountingGeneralSelfPay"),
        AccountingInsuranceSelfPay = Number(reader, "AccountingInsuranceSelfPay"),
        AccountingCopayment = Number(reader, "AccountingCopayment"),
        AccountingOutstanding = Number(reader, "AccountingOutstanding"),
        AccountingMergedOutstanding = Number(reader, "AccountingMergedOutstanding"),
        BillingDebt = Number(reader, "BillingDebt"),
        BillingOutstanding = Number(reader, "BillingOutstanding"),
        Difference = Number(reader, "Difference"), ResultGroup = group
    };

    private static string? Text(OracleDataReader reader, string name)
    {
        int ordinal;
        try { ordinal = reader.GetOrdinal(name); } catch (IndexOutOfRangeException) { return null; }
        return reader.IsDBNull(ordinal) ? null
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.Trim();
    }

    private static decimal? Number(OracleDataReader reader, string name)
    {
        int ordinal;
        try { ordinal = reader.GetOrdinal(name); } catch (IndexOutOfRangeException) { return null; }
        return reader.IsDBNull(ordinal) ? null
            : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }

    private static OracleParameter Char(string name, string value, int size = 7)
    {
        OracleParameter parameter = Input(name, OracleDbType.Char, value);
        parameter.Size = size;
        return parameter;
    }

    private static OracleParameter Input(string name, OracleDbType type, object value) => new(name, type)
    { Direction = ParameterDirection.Input, Value = value };
}
