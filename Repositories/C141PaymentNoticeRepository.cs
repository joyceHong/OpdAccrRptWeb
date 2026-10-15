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

    public C141PartialPaymentData LoadPartialPaymentData(
        C141AdmissionKey key,
        string medicalRecordNumber,
        bool isDayCare,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        connection.Open();
        var stays = new List<C141PartialPaymentStay>();
        string? sameSickDate = null;
        using (var command = CreateCommand(connection, C141PaymentNoticeSql.PartialPaymentStays(isDayCare), key))
        using (OracleDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                sameSickDate ??= ReadText(reader, "SameSickDate")?.Trim();
                stays.Add(new C141PartialPaymentStay(
                    ReadText(reader, "PaymentCode")?.Trim() ?? string.Empty,
                    ReadText(reader, "AdmissionDate")?.Trim(),
                    ReadText(reader, "DischargeDate")?.Trim(),
                    ReadText(reader, "ExtendsFreePeriod")?.Trim() == "Y",
                    ReadText(reader, "EmergencyAdmissionDate")?.Trim()));
                string? identity = ReadText(reader, "InsuranceIdentity")?.Trim();
                if (identity is "01" or "35")
                    throw new DataException("C141 部分負擔住院段身分不一致。");
            }
        }

        int priorChargedDays = 0;
        int priorFreeDays = 0;
        if (!isDayCare && !string.IsNullOrWhiteSpace(sameSickDate))
            (priorChargedDays, priorFreeDays) = ReadPriorDays(
                connection, key, medicalRecordNumber, cancellationToken);

        decimal cap;
        using (var command = new OracleCommand(C141PaymentNoticeSql.PartialPaymentCap, connection))
        using (OracleDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
        {
            if (!reader.Read())
                throw new DataException("C141 找不到部分負擔上限設定。");
            string effectiveDate = ReadText(reader, "chOrdPriDay")?.Trim() ?? string.Empty;
            cap = ReadDecimal(reader, key.AdmissionDate.CompareTo(effectiveDate) >= 0
                ? "rlOrdPriA2" : "rlOrdPriB2")
                ?? throw new DataException("C141 部分負擔上限設定缺少金額。");
        }

        return new C141PartialPaymentData(
            DateOnly.FromDateTime(GetOracleNow(cancellationToken).Date),
            stays,
            priorChargedDays,
            priorFreeDays,
            cap);
    }

    public decimal GetPeriodInsuranceAmount(
        C141AdmissionKey key,
        DateOnly begin,
        DateOnly end,
        bool isDayCare,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var connection = new OracleConnection(connectionStringProvider.GetConnectionString());
        using var command = CreateCommand(connection, C141PaymentNoticeSql.PartialPeriodAmount(isDayCare), key);
        C141OracleParameters.AddVarchar2(command, "periodBegin", begin.ToDateTime(TimeOnly.MinValue).ToRocDateString(), 7);
        C141OracleParameters.AddVarchar2(command, "periodEnd", end.ToDateTime(TimeOnly.MinValue).ToRocDateString(), 7);
        connection.Open();
        using OracleDataReader reader = command.ExecuteReader();
        decimal total = 0m;
        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();
            total += decimal.Floor((ReadDecimal(reader, "Amount1") ?? 0m) + 0.5m);
        }
        return total;
    }

    private static (int ChargedDays, int FreeDays) ReadPriorDays(
        OracleConnection connection,
        C141AdmissionKey key,
        string medicalRecordNumber,
        CancellationToken cancellationToken)
    {
        var sameSickDates = new List<string>();
        using (var command = new OracleCommand(C141PaymentNoticeSql.Readmission, connection) { BindByName = true })
        {
            C141OracleParameters.AddFixedChar(command, "mrNo", medicalRecordNumber.Trim(), 10);
            C141OracleParameters.AddFixedChar(command, "opDate", key.AdmissionDate, 7);
            using OracleDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (ReadText(reader, "chReHospType")?.Trim() == "0")
                    sameSickDates.Add(ReadText(reader, "SameSickBefDate")?.Trim() ?? string.Empty);
            }
        }
        if (sameSickDates.Count == 0)
            return (0, 0);

        var admissions = new List<C141AdmissionKey>();
        using (var command = new OracleCommand(C141PaymentNoticeSql.PreviousAdmissions, connection) { BindByName = true })
        {
            C141OracleParameters.AddFixedChar(command, "mrNo", medicalRecordNumber.Trim(), 10);
            C141OracleParameters.AddFixedChar(command, "opDate", key.AdmissionDate, 7);
            using OracleDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                admissions.Add(new C141AdmissionKey(
                    ReadText(reader, "AdmissionDate")?.Trim() ?? string.Empty,
                    ReadText(reader, "AdmissionTime")?.Trim() ?? string.Empty,
                    ReadText(reader, "AdmissionRoom")?.Trim() ?? string.Empty,
                    ReadDecimal(reader, "AdmissionNumber") ?? 0m));
            }
        }

        int chargedDays = 0;
        int freeDays = 0;
        foreach (string sameSickDate in sameSickDates)
        foreach (C141AdmissionKey previous in admissions.Take(Math.Max(0, admissions.Count - 1)))
        {
            if (previous.AdmissionDate != sameSickDate)
                continue;
            using var command = CreateCommand(connection, C141PaymentNoticeSql.PreviousStays, previous);
            using OracleDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                cancellationToken.ThrowIfCancellationRequested();
                DateOnly begin = ParseRocDate(ReadText(reader, "AdmissionDate"));
                DateOnly end = ParseRocDate(ReadText(reader, "DischargeDate"));
                int days = Math.Max(0, end.DayNumber - begin.DayNumber);
                if (ReadText(reader, "PaymentCode")?.Trim() == "000")
                    chargedDays += days;
                else
                    freeDays += days;
            }
        }
        if (freeDays > 30)
            chargedDays += freeDays - 30;
        return (chargedDays == 0 ? 0 : chargedDays + 1, freeDays == 0 ? 0 : freeDays + 1);
    }

    private static DateOnly ParseRocDate(string? value)
    {
        if (value is null || value.Length < 7 || !int.TryParse(value[..3], out int year)
            || !DateOnly.TryParseExact($"{year + 1911:0000}{value[3..7]}", "yyyyMMdd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
            throw new DataException("C141 部分負擔日期資料無效。");
        return date;
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
