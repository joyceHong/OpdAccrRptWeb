using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C141PaymentNoticeRepository(IConnectionStringProvider connectionStringProvider)
    : IC141PaymentNoticeRepository
{
    public DateTimeOffset GetOracleNow(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        using var command = new OracleCommand(C141PaymentNoticeSql.OracleNow, connection);
        connection.Open();
        object? value = command.ExecuteScalar();
        if (value is null or DBNull)
            throw new DataException("Oracle 未回傳 C141 伺服器時間。");

        DateTime serverTime = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        return new DateTimeOffset(DateTime.SpecifyKind(serverTime, DateTimeKind.Unspecified), TimeSpan.FromHours(8));
    }

    public C141PaymentNoticeSource? LoadNoticeSource(
        C141AdmissionKey key,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        connection.Open();
        C141NoticeBasicData? basic = LoadBasic(connection, key, cancellationToken);
        if (basic is null)
            return null;

        decimal? temporaryAmount2 = ReadScalarAmount(
            connection, C141PaymentNoticeSql.TemporaryAmount2, key, cancellationToken);
        IReadOnlyList<C141NoticeCategory> categories = LoadCategories(connection, key, cancellationToken);
        decimal? payTotal = ReadScalarAmount(connection, C141PaymentNoticeSql.PayTotal, key, cancellationToken);
        decimal? discountAmount = ReadScalarAmount(
            connection, C141PaymentNoticeSql.DiscountAmount, key, cancellationToken);

        return new C141PaymentNoticeSource(
            key,
            basic,
            categories,
            temporaryAmount2,
            payTotal,
            discountAmount,
            PartialPaymentCalculationAvailable: basic.InsuranceIdentity != "30");
    }

    public void RecordBeforeDocument(
        C141PaymentNoticeDocument document,
        string userId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (userId.Length > 10)
            throw new ArgumentException("C141 寫入者代碼不可超過 10 個字元。", nameof(userId));
        cancellationToken.ThrowIfCancellationRequested();

        DateTimeOffset recordedAt = GetOracleNow(cancellationToken);
        string serverDate7 = recordedAt.DateTime.ToRocDateString();
        string serverDate13 = $"{serverDate7}{recordedAt:HHmmss}";
        string transactionMrNo = BuildTransactionMedicalRecordNumber(document.Basic);

        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        connection.Open();
        using OracleTransaction transaction = connection.BeginTransaction();
        try
        {
            using (var update = new OracleCommand(C141PaymentNoticeSql.UpdateBeforeDocument, connection)
            {
                BindByName = true,
                Transaction = transaction
            })
            {
                AddKeyParameters(update, document.Key);
                C141OracleParameters.AddVarchar2(update, "serverDate7", serverDate7, 7);
                if (update.ExecuteNonQuery() != 1)
                    throw new DataException("C141 通知日期更新筆數不符。");
            }

            using (var insert = new OracleCommand(C141PaymentNoticeSql.InsertNoticeTransaction, connection)
            {
                BindByName = true,
                Transaction = transaction
            })
            {
                AddKeyParameters(insert, document.Key);
                C141OracleParameters.AddVarchar2(insert, "opdUniNo", C141LineBotRepository.BuildNoticeUniqueNumber(document.Key), 20);
                C141OracleParameters.AddVarchar2(insert, "mrNo", transactionMrNo, 10);
                C141OracleParameters.AddVarchar2(insert, "serverDate13", serverDate13, 13);
                C141OracleParameters.AddVarchar2(
                    insert,
                    "amountDue",
                    document.Totals.RemainingAmountG.ToString("0.##", CultureInfo.InvariantCulture),
                    20);
                C141OracleParameters.AddVarchar2(insert, "userId", userId, 10);
                C141OracleParameters.AddVarchar2(insert, "serverDate7", serverDate7, 7);
                C141OracleParameters.AddVarchar2(insert, "serverTime6", $"{recordedAt:HHmmss}", 6);
                if (insert.ExecuteNonQuery() != 1)
                    throw new DataException("C141 通知紀錄寫入筆數不符。");
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void RecordAfterDocument(
        C141AdmissionKey key,
        string entryDateRoc,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (entryDateRoc.Length != 7 || entryDateRoc.Any(character => !char.IsAsciiDigit(character)))
            throw new ArgumentException("C141 文件進入日期格式無效。", nameof(entryDateRoc));
        cancellationToken.ThrowIfCancellationRequested();

        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        using var command = new OracleCommand(C141PaymentNoticeSql.UpdateAfterDocument, connection)
        {
            BindByName = true
        };
        AddKeyParameters(command, key);
        C141OracleParameters.AddVarchar2(command, "entryDate7", entryDateRoc, 7);
        connection.Open();
        if (command.ExecuteNonQuery() != 1)
            throw new DataException("C141 文件產生後日期更新筆數不符。");
    }

    private static C141NoticeBasicData? LoadBasic(
        OracleConnection connection,
        C141AdmissionKey key,
        CancellationToken cancellationToken)
    {
        using var command = CreateCommand(connection, C141PaymentNoticeSql.Basic, key);
        using OracleDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow);
        cancellationToken.ThrowIfCancellationRequested();
        if (!reader.Read())
            return null;

        return new C141NoticeBasicData(
            ReadText(reader, "MedicalRecordNumber") ?? string.Empty,
            ReadText(reader, "PatientName") ?? string.Empty,
            ReadText(reader, "SectionCode") ?? string.Empty,
            ReadText(reader, "SectionName") ?? string.Empty,
            ReadText(reader, "BedNumber") ?? string.Empty,
            ReadText(reader, "InpatientAdmissionDate"),
            ReadText(reader, "PaymentType"),
            ReadText(reader, "InsuranceIdentity"),
            ReadText(reader, "InsuranceCategory"),
            ReadText(reader, "DoctorName"),
            ReadText(reader, "InpatientSequence"),
            ReadText(reader, "HealthCardNumber"));
    }

    private static IReadOnlyList<C141NoticeCategory> LoadCategories(
        OracleConnection connection,
        C141AdmissionKey key,
        CancellationToken cancellationToken)
    {
        using var command = CreateCommand(connection, C141PaymentNoticeSql.Categories, key);
        using OracleDataReader reader = command.ExecuteReader();
        var categories = new List<C141NoticeCategory>();
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            categories.Add(new C141NoticeCategory(
                ReadText(reader, "DepartmentCode"),
                ReadText(reader, "DepartmentName"),
                ReadDecimal(reader, "Amount1"),
                ReadDecimal(reader, "Amount2"),
                ReadDecimal(reader, "Subsidy1"),
                ReadDecimal(reader, "Subsidy2"),
                ReadDecimal(reader, "Subsidy3"),
                ReadDecimal(reader, "Subsidy4"),
                ReadDecimal(reader, "Subsidy5"),
                ReadDecimal(reader, "Subsidy6")));
        }

        return categories;
    }

    private static decimal? ReadScalarAmount(
        OracleConnection connection,
        string sql,
        C141AdmissionKey key,
        CancellationToken cancellationToken)
    {
        using var command = CreateCommand(connection, sql, key);
        cancellationToken.ThrowIfCancellationRequested();
        object? value = command.ExecuteScalar();
        return value is null or DBNull
            ? null
            : Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    private static OracleCommand CreateCommand(
        OracleConnection connection,
        string sql,
        C141AdmissionKey key)
    {
        var command = new OracleCommand(sql, connection) { BindByName = true };
        C141RecalculationRepository.AddAdmissionKeyParameters(command, key);
        return command;
    }

    private static void AddKeyParameters(OracleCommand command, C141AdmissionKey key) =>
        C141RecalculationRepository.AddAdmissionKeyParameters(command, key);

    private static string BuildTransactionMedicalRecordNumber(C141NoticeBasicData basic)
    {
        string medicalRecordNumber = basic.MedicalRecordNumber.Trim();
        string sequence = basic.InpatientSequence?.Trim() ?? string.Empty;
        string value = string.IsNullOrEmpty(sequence)
            ? medicalRecordNumber
            : string.Concat(medicalRecordNumber, "-", sequence).Replace("-", string.Empty, StringComparison.Ordinal);
        if (value.Length > 10)
            throw new ArgumentException("C141 通知紀錄病歷號超過欄位長度。", nameof(basic));
        return value;
    }

    private static string? ReadText(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture)?.TrimEnd();
    }

    private static decimal? ReadDecimal(OracleDataReader reader, string name)
    {
        int ordinal = reader.GetOrdinal(name);
        return reader.IsDBNull(ordinal)
            ? null
            : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);
    }
}
