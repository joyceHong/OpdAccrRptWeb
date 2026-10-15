using System.Data;
using System.Globalization;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Infrastructure;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Repositories;

public sealed class C141LineBotRepository(IConnectionStringProvider connectionStringProvider)
    : IC141LineBotRepository
{
    internal const string PaymentNoticePrintType = "LineBot發送繳款通知單";

    public DateTimeOffset GetOracleNow(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        using var command = new OracleCommand("SELECT SYSDATE FROM DUAL", connection);
        connection.Open();
        object? value = command.ExecuteScalar();
        if (value is null or DBNull)
            throw new DataException("Oracle 未回傳 C141 伺服器時間。");

        DateTime serverTime = Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        return new DateTimeOffset(DateTime.SpecifyKind(serverTime, DateTimeKind.Unspecified), TimeSpan.FromHours(8));
    }

    public C141LineBotCase? LoadCase(C141AdmissionKey key, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(key);
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        using var command = new OracleCommand(C141LineBotSql.Case, connection) { BindByName = true };
        C141RecalculationRepository.AddAdmissionKeyParameters(command, key);
        connection.Open();
        using OracleDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow);
        cancellationToken.ThrowIfCancellationRequested();
        if (!reader.Read())
            return null;

        return new C141LineBotCase(
            key,
            ReadText(reader, "MedicalRecordNumber"),
            ReadText(reader, "PatientName"),
            ReadText(reader, "BedNumber"),
            ReadText(reader, "InsuranceIdentity"),
            ReadText(reader, "PaymentType"),
            ReadText(reader, "InsuranceCategory"),
            ReadText(reader, "InpatientSequence"),
            ReadText(reader, "HealthCardNumber"),
            ReadDecimal(reader, "ReceivableAmount"),
            ReadDecimal(reader, "PayTotal"),
            ReadText(reader, "ContactMedicalRecordNumber"),
            ReadText(reader, "ContactPatientId"));
    }

    public void RecordPaymentNoticeSuccess(
        C141LineBotCase item,
        string userId,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (userId.Length > 10)
            throw new ArgumentException("C141 寫入者代碼不可超過 10 個字元。", nameof(userId));
        cancellationToken.ThrowIfCancellationRequested();

        C141AdmissionKey key = item.Key;
        string rocDate = recordedAt.DateTime.ToRocDateString();
        string serverDateTime = $"{rocDate}{recordedAt:HHmmss}";
        string opdUniNo = BuildNoticeUniqueNumber(key);
        string transactionMrNo = BuildTransactionMedicalRecordNumber(item);
        string amount = item.NetAmount.ToString("0.##", CultureInfo.InvariantCulture);

        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        connection.Open();
        using OracleTransaction transaction = connection.BeginTransaction();
        try
        {
            using (var update = new OracleCommand(C141LineBotSql.UpdateNoticeDates, connection)
            {
                BindByName = true,
                Transaction = transaction
            })
            {
                AddKeyParameters(update, key);
                C141OracleParameters.AddVarchar2(update, "serverDate7", rocDate, 7);
                if (update.ExecuteNonQuery() != 1)
                    throw new DataException("C141 通知日期更新筆數不符。");
            }

            using (var insert = new OracleCommand(C141LineBotSql.InsertNoticeLog, connection)
            {
                BindByName = true,
                Transaction = transaction
            })
            {
                AddKeyParameters(insert, key);
                C141OracleParameters.AddVarchar2(insert, "opdUniNo", opdUniNo, 20);
                C141OracleParameters.AddVarchar2(insert, "mrNo", transactionMrNo, 10);
                C141OracleParameters.AddVarchar2(insert, "serverDate13", serverDateTime, 13);
                C141OracleParameters.AddVarchar2(insert, "printType", PaymentNoticePrintType, 50);
                C141OracleParameters.AddVarchar2(insert, "amountDue", amount, 20);
                C141OracleParameters.AddVarchar2(insert, "userId", userId, 10);
                C141OracleParameters.AddVarchar2(insert, "serverDate7", rocDate, 7);
                C141OracleParameters.AddVarchar2(insert, "serverTime6", $"{recordedAt:HHmmss}", 6);
                if (insert.ExecuteNonQuery() != 1)
                    throw new DataException("C141 通知紀錄寫入筆數不符。");
            }

            using (var delete = new OracleCommand(C141LineBotSql.DeleteZeroSequencePrepayment, connection)
            {
                BindByName = true,
                Transaction = transaction
            })
            {
                AddKeyParameters(delete, key);
                delete.ExecuteNonQuery();
            }

            using (var insert = new OracleCommand(C141LineBotSql.InsertPrepayment, connection)
            {
                BindByName = true,
                Transaction = transaction
            })
            {
                AddKeyParameters(insert, key);
                string opdUnion = $"{key.AdmissionDate}@{key.AdmissionTime}@{key.AdmissionRoom.TrimEnd()}@{key.AdmissionNumber:0000}@0";
                C141OracleParameters.AddVarchar2(insert, "opdUnion", opdUnion, 30);
                C141OracleParameters.AddVarchar2(insert, "amount", amount, 20);
                C141OracleParameters.AddVarchar2(insert, "userId", userId, 10);
                C141OracleParameters.AddVarchar2(insert, "serverDate13", serverDateTime, 13);
                C141OracleParameters.AddVarchar2(insert, "paymentType", item.PaymentType, 5);
                C141OracleParameters.AddVarchar2(insert, "insuranceIdentity", item.InsuranceIdentity, 5);
                C141OracleParameters.AddVarchar2(insert, "insuranceCategory", item.InsuranceCategory, 5);
                C141OracleParameters.AddVarchar2(insert, "inpatientSequence", item.InpatientSequence, 5);
                if (insert.ExecuteNonQuery() != 1)
                    throw new DataException("C141 預繳紀錄寫入筆數不符。");
            }

            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    internal static string BuildNoticeUniqueNumber(C141AdmissionKey key)
    {
        if (key.AdmissionNumber < 0m
            || decimal.Truncate(key.AdmissionNumber) != key.AdmissionNumber
            || key.AdmissionNumber > 9999m)
            throw new ArgumentException("C141 住院序號超過通知紀錄可接受範圍。", nameof(key));

        return string.Concat(
            key.AdmissionDate,
            key.AdmissionTime,
            key.AdmissionRoom.PadRight(C141OracleParameters.AdmissionRoomLength, ' '),
            key.AdmissionNumber.ToString("0000", CultureInfo.InvariantCulture),
            "01");
    }

    internal static string BuildTransactionMedicalRecordNumber(C141LineBotCase item)
    {
        string medicalRecordNumber = item.MedicalRecordNumber?.Trim() ?? string.Empty;
        string sequence = item.InpatientSequence?.Trim() ?? string.Empty;
        string value = string.IsNullOrEmpty(sequence)
            ? medicalRecordNumber
            : string.Concat(medicalRecordNumber, "-", sequence).Replace("-", string.Empty, StringComparison.Ordinal);
        if (value.Length > 10)
            throw new ArgumentException("C141 通知紀錄病歷號超過欄位長度。", nameof(item));
        return value;
    }

    private static void AddKeyParameters(OracleCommand command, C141AdmissionKey key) =>
        C141RecalculationRepository.AddAdmissionKeyParameters(command, key);

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
