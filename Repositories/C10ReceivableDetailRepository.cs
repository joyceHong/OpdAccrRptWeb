using System.Data;
using Dapper;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C10ReceivableDetailRepository : IC10ReceivableDetailRepository
{
    internal const int DetailBatchSize = 50;
    private readonly IConnectionStringProvider _connectionStringProvider;

    public C10ReceivableDetailRepository(IConnectionStringProvider connectionStringProvider) =>
        _connectionStringProvider = connectionStringProvider;

    public C10RepositoryResult Load(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default)
    {
        using var connection = new OracleConnection(_connectionStringProvider.GetConnectionString());
        connection.Open();
        using var debtCommand = new OracleCommand(
            condition.Source == C10Sources.Inpatient ? C10Sql.InpatientDebt : C10Sql.OutpatientDebt,
            connection)
        {
            BindByName = true
        };
        AddDebtParameters(debtCommand, condition);
        List<C10DebtVisit> visits = ReadVisits(debtCommand, cancellationToken);
        var charges = new List<C10ChargeAggregate>();
        foreach (C10DebtVisit[] batch in visits.Chunk(DetailBatchSize))
        {
            using var detailCommand = new OracleCommand(
                C10Sql.BuildDetail(condition.Source!, batch.Length),
                connection)
            {
                BindByName = true
            };
            AddDetailParameters(detailCommand, batch);
            charges.AddRange(ReadCharges(detailCommand, cancellationToken));
        }
        return new C10RepositoryResult { Visits = visits, Charges = charges };
    }

    internal static DynamicParameters CreateDebtParameters(SearchReportCondition condition)
    {
        var parameters = new DynamicParameters();
        parameters.Add("StartDate", ToRocDate(condition.StartDate), DbType.String);
        parameters.Add("EndDate", ToRocDate(condition.EndDate), DbType.String);
        parameters.Add("MedicalRecordNumber", condition.MedicalRecordNo, DbType.String);
        if (condition.Source == C10Sources.OpdEr)
        {
            int scope = condition.RoomScope switch
            {
                C10RoomScopes.Emergency => 1,
                C10RoomScopes.Outpatient => 2,
                _ => 0
            };
            parameters.Add("RoomScope", scope, DbType.Int32);
        }
        return parameters;
    }

    internal static DynamicParameters CreateDetailParameters(IReadOnlyList<C10DebtVisit> visits)
    {
        var parameters = new DynamicParameters();
        for (var index = 0; index < visits.Count; index++)
        {
            C10VisitKey key = visits[index].VisitKey;
            parameters.Add($"VisitDate{index}", key.VisitDate, DbType.String);
            parameters.Add($"VisitTime{index}", key.VisitTime, DbType.String);
            parameters.Add($"VisitRoom{index}", key.VisitRoom, DbType.String);
            parameters.Add($"VisitNumber{index}", key.VisitNumber, DbType.Int32);
        }
        return parameters;
    }

    private static void AddDebtParameters(OracleCommand command, SearchReportCondition condition)
    {
        DynamicParameters values = CreateDebtParameters(condition);
        command.Parameters.Add("StartDate", OracleDbType.Char, values.Get<string>("StartDate"),
            ParameterDirection.Input);
        command.Parameters.Add("EndDate", OracleDbType.Char, values.Get<string>("EndDate"),
            ParameterDirection.Input);
        command.Parameters.Add("MedicalRecordNumber", OracleDbType.Varchar2,
            (object?)condition.MedicalRecordNo ?? DBNull.Value, ParameterDirection.Input);
        if (condition.Source == C10Sources.OpdEr)
            command.Parameters.Add("RoomScope", OracleDbType.Int32, values.Get<int>("RoomScope"),
                ParameterDirection.Input);
    }

    private static void AddDetailParameters(OracleCommand command, IReadOnlyList<C10DebtVisit> visits)
    {
        for (var index = 0; index < visits.Count; index++)
        {
            C10VisitKey key = visits[index].VisitKey;
            command.Parameters.Add($"VisitDate{index}", OracleDbType.Char, key.VisitDate,
                ParameterDirection.Input);
            command.Parameters.Add($"VisitTime{index}", OracleDbType.Char, key.VisitTime,
                ParameterDirection.Input);
            command.Parameters.Add($"VisitRoom{index}", OracleDbType.Char, key.VisitRoom,
                ParameterDirection.Input);
            command.Parameters.Add($"VisitNumber{index}", OracleDbType.Int32, key.VisitNumber,
                ParameterDirection.Input);
        }
    }

    private static List<C10DebtVisit> ReadVisits(
        OracleCommand command,
        CancellationToken cancellationToken)
    {
        using OracleDataReader reader = command.ExecuteReaderAsync(cancellationToken)
            .GetAwaiter().GetResult();
        var rows = new List<C10DebtVisit>();
        while (reader.Read())
        {
            rows.Add(MapVisit(new DebtRow
            {
                VisitDate = RequiredString(reader, "VisitDate"),
                VisitTime = RequiredString(reader, "VisitTime"),
                VisitRoom = RequiredString(reader, "VisitRoom"),
                VisitNumber = Convert.ToInt32(reader["VisitNumber"]),
                AdmissionSequence = RequiredString(reader, "AdmissionSequence"),
                RoomType = RequiredString(reader, "RoomType"),
                MedicalRecordNumber = RequiredString(reader, "MedicalRecordNumber"),
                PatientName = RequiredString(reader, "PatientName"),
                DoctorName = RequiredString(reader, "DoctorName"),
                DepartmentName = RequiredString(reader, "DepartmentName"),
                DischargeDate = OptionalString(reader, "DischargeDate"),
                HomePhone = OptionalString(reader, "HomePhone"),
                Address1 = OptionalString(reader, "Address1"),
                Address2 = OptionalString(reader, "Address2"),
                ContactName = OptionalString(reader, "ContactName"),
                ContactRelation = OptionalString(reader, "ContactRelation"),
                ContactPhone = OptionalString(reader, "ContactPhone"),
                DebtAmount = Convert.ToDecimal(reader["DebtAmount"])
            }));
        }
        return rows;
    }

    private static List<C10ChargeAggregate> ReadCharges(
        OracleCommand command,
        CancellationToken cancellationToken)
    {
        using OracleDataReader reader = command.ExecuteReaderAsync(cancellationToken)
            .GetAwaiter().GetResult();
        var rows = new List<C10ChargeAggregate>();
        while (reader.Read())
        {
            rows.Add(MapCharge(new ChargeRow
            {
                VisitDate = RequiredString(reader, "VisitDate"),
                VisitTime = RequiredString(reader, "VisitTime"),
                VisitRoom = RequiredString(reader, "VisitRoom"),
                VisitNumber = Convert.ToInt32(reader["VisitNumber"]),
                Source = RequiredString(reader, "Source"),
                ChargeItemName = RequiredString(reader, "ChargeItemName"),
                Sub6 = OptionalDecimal(reader, "Sub6"),
                Sub3 = OptionalDecimal(reader, "Sub3"),
                Sub1 = OptionalDecimal(reader, "Sub1"),
                Sub25 = OptionalDecimal(reader, "Sub25")
            }));
        }
        return rows;
    }

    private static string RequiredString(OracleDataReader reader, string name) =>
        OptionalString(reader, name) ?? string.Empty;

    private static string? OptionalString(OracleDataReader reader, string name) =>
        reader[name] is DBNull ? null : Convert.ToString(reader[name]);

    private static decimal? OptionalDecimal(OracleDataReader reader, string name) =>
        reader[name] is DBNull ? null : Convert.ToDecimal(reader[name]);

    private static string ToRocDate(string? value)
    {
        if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", out DateOnly date))
            throw new ArgumentException("C10 日期格式不正確。", nameof(value));
        return DateTimeExtensions.ToRocDateString(date.ToDateTime(TimeOnly.MinValue));
    }

    private static C10DebtVisit MapVisit(DebtRow row) => new(
        new C10VisitKey(row.VisitDate, row.VisitTime, row.VisitRoom, row.VisitNumber),
        row.AdmissionSequence,
        row.RoomType,
        row.MedicalRecordNumber,
        row.PatientName,
        row.DoctorName,
        row.DepartmentName,
        row.DischargeDate is { Length: > 7 } ? row.DischargeDate[..7] : row.DischargeDate,
        row.HomePhone,
        row.Address1,
        row.Address2,
        row.ContactName,
        row.ContactRelation,
        row.ContactPhone,
        row.DebtAmount);

    private static C10ChargeAggregate MapCharge(ChargeRow row) => new(
        new C10VisitKey(row.VisitDate, row.VisitTime, row.VisitRoom, row.VisitNumber),
        row.Source == "Drug" ? C10ChargeSource.Drug : C10ChargeSource.Order,
        row.ChargeItemName,
        row.Sub6,
        row.Sub3,
        row.Sub1,
        row.Sub25);

    private sealed class DebtRow
    {
        public string VisitDate { get; init; } = string.Empty;
        public string VisitTime { get; init; } = string.Empty;
        public string VisitRoom { get; init; } = string.Empty;
        public int VisitNumber { get; init; }
        public string AdmissionSequence { get; init; } = string.Empty;
        public string RoomType { get; init; } = string.Empty;
        public string MedicalRecordNumber { get; init; } = string.Empty;
        public string PatientName { get; init; } = string.Empty;
        public string DoctorName { get; init; } = string.Empty;
        public string DepartmentName { get; init; } = string.Empty;
        public string? DischargeDate { get; init; }
        public string? HomePhone { get; init; }
        public string? Address1 { get; init; }
        public string? Address2 { get; init; }
        public string? ContactName { get; init; }
        public string? ContactRelation { get; init; }
        public string? ContactPhone { get; init; }
        public decimal DebtAmount { get; init; }
    }

    private sealed class ChargeRow
    {
        public string VisitDate { get; init; } = string.Empty;
        public string VisitTime { get; init; } = string.Empty;
        public string VisitRoom { get; init; } = string.Empty;
        public int VisitNumber { get; init; }
        public string Source { get; init; } = string.Empty;
        public string ChargeItemName { get; init; } = string.Empty;
        public decimal? Sub6 { get; init; }
        public decimal? Sub3 { get; init; }
        public decimal? Sub1 { get; init; }
        public decimal? Sub25 { get; init; }
    }
}
