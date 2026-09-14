using System.Data;
using Oracle.ManagedDataAccess.Client;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public sealed class C24DebtPaymentRepository(IConnectionStringProvider connectionStringProvider)
    : IC24DebtPaymentRepository
{
    public bool HasLegacyResult(string source, DateOnly accountingDate,
        CancellationToken cancellationToken = default)
    {
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        connection.Open();
        using var command = new OracleCommand(LegacyReadSql(source).Exists, connection) { BindByName = true };
        command.Parameters.Add(Parameter("accounting_date", OracleDbType.Varchar2, ToRocDate(accountingDate), 7));
        cancellationToken.ThrowIfCancellationRequested();
        return Convert.ToDecimal(command.ExecuteScalar()) > 0m;
    }

    public C24LegacyResult LoadLegacy(string source, DateOnly startDate, DateOnly endDate,
        CancellationToken cancellationToken = default)
    {
        var sql = LegacyReadSql(source);
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var details = ReadLegacyDetails(connection, transaction, sql.Details, startDate, endDate,
                cancellationToken).ToList();
            var summaries = ReadLegacySummaries(connection, transaction, sql.Summaries, startDate, endDate,
                cancellationToken).ToList();
            transaction.Commit();
            return new C24LegacyResult(details, summaries);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public C24RepositoryResult Load(SearchReportCondition condition, CancellationToken cancellationToken = default)
    {
        var start = DateOnly.ParseExact(condition.StartDate!, "yyyy-MM-dd");
        var end = DateOnly.ParseExact(condition.EndDate!, "yyyy-MM-dd");
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            var candidates = new List<C24Candidate>();
            var billingRows = new List<C24BillingRow>();
            if (condition.Mode == C24Modes.Accounting)
            {
                for (var day = start; day <= end; day = day.AddDays(1))
                {
                    foreach (var query in C24Sql.Accounting(condition.Source!))
                        candidates.AddRange(ReadCandidates(connection, transaction, query, day, cancellationToken));
                }
            }
            else
            {
                var query = condition.Source == C24Sources.OpdEr ? ("B01", C24Sql.B01) : ("B02", C24Sql.B02);
                billingRows.AddRange(ReadBilling(connection, transaction, query, start, end,
                    condition.RoomScope!, condition.MedicalRecordNo, cancellationToken));
            }

            var patients = new List<C24PatientEnrichment>();
            foreach (var visitKey in candidates.Select(x => x.VisitKey).Distinct(StringComparer.Ordinal))
                patients.AddRange(ReadPatients(connection, transaction, condition.Source!, visitKey, cancellationToken));
            var chargeItems = new List<C24ChargeItemEnrichment>();
            foreach (var code in candidates.Select(x => x.ChargeItemCode).Where(x => x is not null)
                         .Distinct(StringComparer.Ordinal))
                chargeItems.AddRange(ReadChargeItems(connection, transaction, code!, cancellationToken));

            transaction.Commit();
            return new C24RepositoryResult
            {
                Candidates = candidates, Patients = patients, ChargeItems = chargeItems, BillingRows = billingRows
            };
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void PublishLegacy(string source, DateOnly accountingDate,
        IReadOnlyList<C24LegacyDetailRow> details, IReadOnlyList<C24LegacySummaryRow> summaries,
        CancellationToken cancellationToken = default)
    {
        var sql = LegacySql(source);
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            Execute(connection, transaction, sql.Lock, cancellationToken);
            Execute(connection, transaction, sql.DeleteDetails, cancellationToken,
                Parameter("accounting_date", OracleDbType.Varchar2, ToRocDate(accountingDate), 7));
            Execute(connection, transaction, sql.DeleteSummaries, cancellationToken,
                Parameter("accounting_date", OracleDbType.Varchar2, ToRocDate(accountingDate), 7));
            foreach (var detail in details)
            {
                cancellationToken.ThrowIfCancellationRequested();
                InsertDetail(connection, transaction, sql.InsertDetail, detail, source == C24Sources.OpdEr);
            }
            foreach (var summary in summaries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                InsertSummary(connection, transaction, sql.InsertSummary, summary);
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    internal static OracleParameter Parameter(string name, OracleDbType type, object? value, int size = 0)
    {
        var parameter = new OracleParameter(name, type)
        {
            Direction = ParameterDirection.Input,
            Value = value ?? DBNull.Value
        };
        if (size > 0) parameter.Size = size;
        return parameter;
    }

    private static void InsertDetail(OracleConnection connection, OracleTransaction transaction,
        string sql, C24LegacyDetailRow row, bool includeDischargeFlag)
    {
        using var command = Command(connection, transaction, sql);
        command.Parameters.Add(Parameter("accounting_date", OracleDbType.Varchar2, ToRocDate(row.AccountingDate), 7));
        command.Parameters.Add(Parameter("visit_date", OracleDbType.Varchar2, ToRocDate(row.VisitDate), 7));
        command.Parameters.Add(Parameter("room_type", OracleDbType.Varchar2, row.RoomType, 1));
        command.Parameters.Add(Parameter("room_name", OracleDbType.Varchar2, row.RoomTypeName, 20));
        command.Parameters.Add(Parameter("mr_no", OracleDbType.Varchar2, row.MedicalRecordNo, 10));
        command.Parameters.Add(Parameter("mr_no2", OracleDbType.Varchar2, row.AlternateMedicalRecordNo, 10));
        command.Parameters.Add(Parameter("patient_name", OracleDbType.Varchar2, row.PatientName, 20));
        command.Parameters.Add(Parameter("phone", OracleDbType.Varchar2, row.Phone, 15));
        command.Parameters.Add(Parameter("section_code", OracleDbType.Varchar2, row.DepartmentCode, 7));
        command.Parameters.Add(Parameter("fin1", OracleDbType.Varchar2, row.PayerClassCode, 2));
        command.Parameters.Add(Parameter("in_seq", OracleDbType.Varchar2, row.CardSequenceNo, 3));
        command.Parameters.Add(Parameter("dct_code", OracleDbType.Varchar2, row.ChargeItemCode, 4));
        command.Parameters.Add(Parameter("dct_name", OracleDbType.Varchar2, row.ChargeItemName, 40));
        command.Parameters.Add(Parameter("signed_amount", OracleDbType.Decimal, row.SignedAmount));
        command.Parameters.Add(Parameter("signed_discount", OracleDbType.Decimal, row.SignedDiscount));
        command.Parameters.Add(Parameter("amount_due", OracleDbType.Decimal, row.AmountDue));
        command.Parameters.Add(Parameter("other_date", OracleDbType.Varchar2, row.OtherDate, 7));
        command.Parameters.Add(Parameter("created_by", OracleDbType.Varchar2, row.CreatedBy, 10));
        command.Parameters.Add(Parameter("deleted_by", OracleDbType.Varchar2, row.DeletedBy, 10));
        command.Parameters.Add(Parameter("flag", OracleDbType.Int32, row.Flag));
        command.Parameters.Add(Parameter("date_flag", OracleDbType.Varchar2, row.DateFlag, 15));
        if (includeDischargeFlag)
            command.Parameters.Add(Parameter("discharge_flag", OracleDbType.Varchar2, row.DischargeFlag, 2));
        command.ExecuteNonQuery();
    }

    private static void InsertSummary(OracleConnection connection, OracleTransaction transaction,
        string sql, C24LegacySummaryRow row)
    {
        using var command = Command(connection, transaction, sql);
        command.Parameters.Add(Parameter("accounting_date", OracleDbType.Varchar2, ToRocDate(row.AccountingDate), 7));
        command.Parameters.Add(Parameter("room_type", OracleDbType.Varchar2, row.RoomType, 1));
        command.Parameters.Add(Parameter("room_name", OracleDbType.Varchar2, row.RoomTypeName, 20));
        command.Parameters.Add(Parameter("debt_amount", OracleDbType.Decimal, row.DebtAmount));
        command.Parameters.Add(Parameter("debt_count", OracleDbType.Decimal, row.DebtCount));
        command.Parameters.Add(Parameter("payment_amount", OracleDbType.Decimal, row.PaymentAmount));
        command.Parameters.Add(Parameter("payment_count", OracleDbType.Decimal, row.PaymentCount));
        command.Parameters.Add(Parameter("outstanding_amount", OracleDbType.Decimal, row.OutstandingAmount));
        command.Parameters.Add(Parameter("outstanding_count", OracleDbType.Decimal, row.OutstandingCount));
        command.Parameters.Add(Parameter("date_flag", OracleDbType.Varchar2, row.DateFlag, 15));
        command.ExecuteNonQuery();
    }

    private static void Execute(OracleConnection connection, OracleTransaction transaction, string sql,
        CancellationToken cancellationToken, params OracleParameter[] parameters)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var command = Command(connection, transaction, sql);
        command.Parameters.AddRange(parameters);
        command.ExecuteNonQuery();
    }

    internal static (string Lock, string DeleteDetails, string DeleteSummaries,
        string InsertDetail, string InsertSummary) LegacySql(string source) => source switch
    {
        C24Sources.OpdEr => (C24Sql.LockOpdLegacy, C24Sql.DeleteOpdLegacyDetails,
            C24Sql.DeleteOpdLegacySummaries, C24Sql.InsertOpdLegacyDetail, C24Sql.InsertOpdLegacySummary),
        C24Sources.Inpatient => (C24Sql.LockIpdLegacy, C24Sql.DeleteIpdLegacyDetails,
            C24Sql.DeleteIpdLegacySummaries, C24Sql.InsertIpdLegacyDetail, C24Sql.InsertIpdLegacySummary),
        _ => throw new ArgumentException("C24 來源不正確。", nameof(source))
    };

    internal static (string Exists, string Details, string Summaries) LegacyReadSql(string source) => source switch
    {
        C24Sources.OpdEr => (C24Sql.HasOpdLegacyResult, C24Sql.ReadOpdLegacyDetails,
            C24Sql.ReadOpdLegacySummaries),
        C24Sources.Inpatient => (C24Sql.HasIpdLegacyResult, C24Sql.ReadIpdLegacyDetails,
            C24Sql.ReadIpdLegacySummaries),
        _ => throw new ArgumentException("C24 來源不正確。", nameof(source))
    };

    private static IEnumerable<C24LegacyDetailRow> ReadLegacyDetails(OracleConnection connection,
        OracleTransaction transaction, string sql, DateOnly start, DateOnly end,
        CancellationToken cancellationToken)
    {
        using var command = LegacyReadCommand(connection, transaction, sql, start, end);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new C24LegacyDetailRow(
                ReadDate(reader, "AccountingDate", start), ReadDate(reader, "VisitDate", start),
                Text(reader, "RoomType") ?? "", Text(reader, "RoomTypeName") ?? "",
                Text(reader, "MedicalRecordNo") ?? "", Text(reader, "AlternateMedicalRecordNo"),
                Text(reader, "PatientName") ?? "", Text(reader, "Phone"), Text(reader, "DepartmentCode"),
                Text(reader, "PayerClassCode"), Text(reader, "CardSequenceNo"), Text(reader, "ChargeItemCode"),
                Text(reader, "ChargeItemName"), Decimal(reader, "SignedAmount"),
                Decimal(reader, "SignedDiscount"), Decimal(reader, "AmountDue"), Text(reader, "OtherDate"),
                Text(reader, "CreatedBy"), Text(reader, "DeletedBy"), NullableInt(reader, "Flag"),
                Text(reader, "DateFlag"), Text(reader, "DischargeFlag"));
        }
    }

    private static IEnumerable<C24LegacySummaryRow> ReadLegacySummaries(OracleConnection connection,
        OracleTransaction transaction, string sql, DateOnly start, DateOnly end,
        CancellationToken cancellationToken)
    {
        using var command = LegacyReadCommand(connection, transaction, sql, start, end);
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new C24LegacySummaryRow(ReadDate(reader, "AccountingDate", start),
                Text(reader, "RoomType") ?? "", Text(reader, "RoomTypeName") ?? "",
                Decimal(reader, "DebtAmount"), Decimal(reader, "DebtCount"),
                Decimal(reader, "PaymentAmount"), Decimal(reader, "PaymentCount"),
                Decimal(reader, "OutstandingAmount"), Decimal(reader, "OutstandingCount"),
                Text(reader, "DateFlag"));
        }
    }

    private static OracleCommand LegacyReadCommand(OracleConnection connection, OracleTransaction transaction,
        string sql, DateOnly start, DateOnly end)
    {
        var command = Command(connection, transaction, sql);
        command.Parameters.Add(Parameter("start_date", OracleDbType.Varchar2, ToRocDate(start), 7));
        command.Parameters.Add(Parameter("end_date", OracleDbType.Varchar2, ToRocDate(end), 7));
        return command;
    }

    private static IEnumerable<C24Candidate> ReadCandidates(OracleConnection connection,
        OracleTransaction transaction, (string Code, string Sql) query, DateOnly day,
        CancellationToken cancellationToken)
    {
        using var command = Command(connection, transaction, query.Sql);
        var rocDate = ToRocDate(day);
        command.Parameters.Add(Parameter("accounting_date", OracleDbType.Varchar2, rocDate, 7));
        command.Parameters.Add(Parameter("day_begin", OracleDbType.Varchar2, rocDate + "0000", 11));
        command.Parameters.Add(Parameter("day_end", OracleDbType.Varchar2, rocDate + "2359", 11));
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var kind = query.Code[^1] switch { '1' or '4' => C24SourceKind.Drg, '2' or '5' => C24SourceKind.Ord, _ => C24SourceKind.Acc69 };
            var visitKey = JoinVisitKey(reader);
            var businessKey = string.Join('|', visitKey, Text(reader, "IDate"), Text(reader, "DCDate"),
                Text(reader, "ChargeItemCode"), Text(reader, "CreatedBy"));
            yield return new C24Candidate(day, ReadDate(reader, "VisitDate", day), visitKey,
                businessKey, null, null, Text(reader, "PayerClassCode"),
                Text(reader, "ChargeItemCode"), Text(reader, "IDate"), Text(reader, "DCDate"),
                Decimal(reader, "QuerySub6"), Decimal(reader, "QuerySub3"), Decimal(reader, "SourceSub1"), kind,
                Text(reader, "CreatedBy"));
        }
    }

    private static IEnumerable<C24BillingRow> ReadBilling(OracleConnection connection,
        OracleTransaction transaction, (string Code, string Sql) query, DateOnly start, DateOnly end,
        string roomScope, string? mrn, CancellationToken cancellationToken)
    {
        using var command = Command(connection, transaction, query.Sql);
        command.Parameters.Add(Parameter("start_date", OracleDbType.Varchar2, ToRocDate(start), 7));
        command.Parameters.Add(Parameter("end_date", OracleDbType.Varchar2, ToRocDate(end), 7));
        if (query.Code == "B01")
            command.Parameters.Add(Parameter("room_scope", OracleDbType.Int32, RoomScopeValue(roomScope), 0));
        command.Parameters.Add(Parameter("mr_no", OracleDbType.Varchar2, mrn, 10));
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var medicalRecordNo = Text(reader, "MedicalRecordNo")!;
            yield return new C24BillingRow(ReadDate(reader, "BillDate", start), medicalRecordNo,
                medicalRecordNo, Text(reader, "PatientName"), Text(reader, "Phone"),
                query.Code == "B02" ? "I" : Text(reader, "RoomType"), Text(reader, "DepartmentCode"),
                Text(reader, "PayerClassCode"), Text(reader, "CardSequenceNo"), Decimal(reader, "Amount"),
                null, string.Join('|', medicalRecordNo, Text(reader, "BillDate"), Text(reader, "CreatedBy")));
        }
    }

    private static IEnumerable<C24PatientEnrichment> ReadPatients(OracleConnection connection,
        OracleTransaction transaction, string source, string visitKey, CancellationToken cancellationToken)
    {
        var key = visitKey.Split('|');
        if (key.Length != 4) throw new InvalidOperationException("C24 就診鍵格式不正確。");
        var sql = source == C24Sources.Inpatient ? C24Sql.E01
            : key[2] is "EEEE" or "HHHH" ? C24Sql.E03 : C24Sql.E02;
        using var command = Command(connection, transaction, sql);
        command.Parameters.Add(Parameter("visit_date", OracleDbType.Varchar2, key[0], 7));
        command.Parameters.Add(Parameter("visit_time", OracleDbType.Varchar2, key[1], 4));
        command.Parameters.Add(Parameter("visit_room", OracleDbType.Varchar2, key[2], 4));
        command.Parameters.Add(Parameter("visit_no", OracleDbType.Int32, int.Parse(key[3]), 0));
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new C24PatientEnrichment(visitKey, Text(reader, "MedicalRecordNo")!,
                Text(reader, "PatientName")!, Text(reader, "Phone"), Text(reader, "RoomType"),
                Text(reader, "DepartmentCode"), Text(reader, "PayerClassCode"), Text(reader, "CardSequenceNo"));
        }
    }

    private static IEnumerable<C24ChargeItemEnrichment> ReadChargeItems(OracleConnection connection,
        OracleTransaction transaction, string code, CancellationToken cancellationToken)
    {
        using var command = Command(connection, transaction, C24Sql.E04);
        command.Parameters.Add(Parameter("dct_code", OracleDbType.Varchar2, code, 10));
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new C24ChargeItemEnrichment(Text(reader, "ChargeItemCode")!, Text(reader, "ChargeItemName"));
        }
    }

    private static OracleCommand Command(OracleConnection connection, OracleTransaction transaction, string sql) =>
        new(sql, connection) { BindByName = true, Transaction = transaction };
    private static string ToRocDate(DateOnly date) => $"{date.Year - 1911:000}{date:MMdd}";
    private static int RoomScopeValue(string roomScope) => roomScope switch
    {
        C24RoomScopes.All => 0,
        C24RoomScopes.Emergency => 1,
        C24RoomScopes.NonEmergency => 2,
        _ => throw new ArgumentException("C24 房別範圍不正確。", nameof(roomScope))
    };
    private static string JoinVisitKey(IDataRecord row) => string.Join('|',
        Text(row, "VisitDate"), Text(row, "VisitTime"), Text(row, "VisitRoom"), Text(row, "VisitNo"));
    private static string? Text(IDataRecord row, string name)
    {
        try { return row[name] is DBNull ? null : Convert.ToString(row[name]); }
        catch (IndexOutOfRangeException) { return null; }
    }
    private static decimal Decimal(IDataRecord row, string name)
    {
        try { return row[name] is DBNull ? 0m : Convert.ToDecimal(row[name]); }
        catch (IndexOutOfRangeException) { return 0m; }
    }
    private static int? NullableInt(IDataRecord row, string name)
    {
        try { return row[name] is DBNull ? null : Convert.ToInt32(row[name]); }
        catch (IndexOutOfRangeException) { return null; }
    }
    private static DateOnly ReadDate(IDataRecord row, string name, DateOnly fallback)
    {
        var value = Text(row, name);
        if (DateOnly.TryParseExact(value, "yyyyMMdd", out var date)) return date;
        if (value?.Length == 7 && int.TryParse(value[..3], out var year)
            && DateOnly.TryParseExact($"{year + 1911}{value[3..]}", "yyyyMMdd", out date)) return date;
        return fallback;
    }
}
