using System.Globalization;
using Microsoft.Extensions.Options;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Services;

public sealed class C141LineBotService(
    IC141LineBotRepository repository,
    IC141LineBotClient client,
    IOptions<C141LineBotOptions> options) : IC141LineBotService
{
    private static readonly HashSet<string> CardExcludedBedPrefixes = new(StringComparer.OrdinalIgnoreCase)
    {
        "04E0", "04B2", "04D3"
    };

    public async Task<C141LineBotBatchResult> SendAsync(
        C141LineBotMessageKind kind,
        IReadOnlyList<C141AdmissionKey> cases,
        string userId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        if (cases.Count == 0)
            throw new ArgumentException("請至少選取一筆 C141 個案。", nameof(cases));
        if (cases.Distinct().Count() != cases.Count)
            throw new ArgumentException("C141 個案清單不得包含重複四鍵。", nameof(cases));
        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(nameof(kind));
        if (kind == C141LineBotMessageKind.PaymentNotice && userId.Length > 10)
            return new C141LineBotBatchResult(cases.Select(key =>
                new C141LineBotDelivery(key, "Failed", "登入者代碼超過通知紀錄欄位長度，未發送通知。")).ToArray());

        C141LineBotOptions settings = options.Value;
        if (!settings.Enabled)
            return new C141LineBotBatchResult(cases.Select(key =>
                new C141LineBotDelivery(key, "Skipped", "LineBot 功能尚未啟用。")).ToArray());

        DateTimeOffset serverNow;
        try
        {
            serverNow = repository.GetOracleNow(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OracleException)
        {
            return new C141LineBotBatchResult(cases.Select(key =>
                new C141LineBotDelivery(key, "Failed", "資料庫時間查詢失敗，未發送通知。")).ToArray());
        }
        catch (Exception)
        {
            return new C141LineBotBatchResult(cases.Select(key =>
                new C141LineBotDelivery(key, "Failed", "資料庫時間查詢失敗，未發送通知。")).ToArray());
        }

        var results = new List<C141LineBotDelivery>(cases.Count);
        foreach (C141AdmissionKey key in cases)
        {
            cancellationToken.ThrowIfCancellationRequested();
            C141LineBotCase? item;
            try
            {
                item = repository.LoadCase(key, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OracleException)
            {
                results.Add(new(key, "Failed", "資料庫查詢失敗，未發送通知。"));
                continue;
            }
            catch (Exception)
            {
                results.Add(new(key, "Failed", "資料庫查詢失敗，未發送通知。"));
                continue;
            }

            if (item is null)
            {
                results.Add(new(key, "Failed", "找不到目前的 C141 個案，未發送通知。"));
                continue;
            }

            if (kind == C141LineBotMessageKind.PaymentNotice && !CanWritePaymentRecord(item))
            {
                results.Add(new(key, "Failed", "本案資料超過通知紀錄欄位可接受範圍，未發送通知。"));
                continue;
            }

            string? skipReason = ValidateRecipient(item, kind);
            if (skipReason is not null)
            {
                results.Add(new(key, "Skipped", skipReason));
                continue;
            }

            string patientId = item.ContactPatientId!.Trim();
            C141LineBotPayload payload = CreatePayload(item, patientId, kind, settings, serverNow);
            C141LineBotSendResult sendResult;
            try
            {
                sendResult = await client.SendAsync(settings.SenderId, payload, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                results.Add(new(key, "Unknown", "服務未確認結果；系統不會自動重送，請先查核送達狀態。"));
                continue;
            }

            if (!sendResult.Success)
            {
                results.Add(new(key, "Failed", "LineBot 未回報送達成功。"));
                continue;
            }

            if (kind == C141LineBotMessageKind.CardReminder)
            {
                results.Add(new(key, "Sent", "讀卡通知已送出。"));
                continue;
            }

            try
            {
                repository.RecordPaymentNoticeSuccess(item, userId, serverNow, cancellationToken);
                results.Add(new(key, "Sent", "繳款通知已送出並完成紀錄。"));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                results.Add(new(key, "SentButNotRecorded", "LineBot 已送出，但資料庫紀錄失敗；請先查核，勿自動重送。"));
            }
        }

        return new C141LineBotBatchResult(results);
    }

    internal static string? ValidateRecipient(C141LineBotCase item, C141LineBotMessageKind kind)
    {
        string medicalRecordNumber = item.MedicalRecordNumber?.Trim() ?? string.Empty;
        string contactMedicalRecordNumber = item.ContactMedicalRecordNumber?.Trim() ?? string.Empty;
        string patientId = item.ContactPatientId?.Trim() ?? string.Empty;
        if (medicalRecordNumber.Length == 0
            || !string.Equals(medicalRecordNumber, contactMedicalRecordNumber, StringComparison.OrdinalIgnoreCase))
            return "沒有找到本案病歷號對應的優先聯絡人。";
        if (patientId.Length != 10 || patientId.Any(character => !char.IsAsciiLetterOrDigit(character)))
            return "本案優先聯絡人的 PID 格式不符，已略過。";

        if (kind == C141LineBotMessageKind.CardReminder)
        {
            if (item.InsuranceIdentity != "30")
                return "讀卡通知僅適用於健保身分 30。";
            if (!string.IsNullOrWhiteSpace(item.HealthCardNumber))
                return "健保卡已有讀卡資料，已略過讀卡通知。";
            string bedPrefix = item.BedNumber?.Trim() ?? string.Empty;
            if (bedPrefix.Length >= 4 && CardExcludedBedPrefixes.Contains(bedPrefix[..4]))
                return "此床位不適用讀卡通知。";
        }

        return null;
    }

    private static C141LineBotPayload CreatePayload(
        C141LineBotCase item,
        string patientId,
        C141LineBotMessageKind kind,
        C141LineBotOptions settings,
        DateTimeOffset sentAt)
    {
        string template = kind == C141LineBotMessageKind.PaymentNotice
            ? settings.PaymentTemplateNumber
            : settings.CardReminderTemplateNumber;
        string medicalRecordNumber = item.MedicalRecordNumber?.Trim() ?? string.Empty;
        string patientName = item.PatientName?.Trim() ?? string.Empty;
        string bedNumber = item.BedNumber?.Trim() ?? string.Empty;
        string[] messageParameters = kind == C141LineBotMessageKind.PaymentNotice
            ?
            [
                patientName,
                medicalRecordNumber,
                bedNumber,
                sentAt.DateTime.ToRocDateString("yyy/MM/dd"),
                item.NetAmount.ToString("0.##", CultureInfo.InvariantCulture)
            ]
            : [patientName, medicalRecordNumber, bedNumber];

        return new C141LineBotPayload(
            null,
            2,
            template,
            patientId,
            0,
            null,
            null,
            Array.Empty<string>(),
            messageParameters,
            false);
    }

    private static bool CanWritePaymentRecord(C141LineBotCase item)
    {
        if (item.Key.AdmissionNumber < 0m
            || decimal.Truncate(item.Key.AdmissionNumber) != item.Key.AdmissionNumber
            || item.Key.AdmissionNumber > 9999m)
            return false;

        string medicalRecordNumber = item.MedicalRecordNumber?.Trim() ?? string.Empty;
        string sequence = item.InpatientSequence?.Trim() ?? string.Empty;
        int transactionNumberLength = medicalRecordNumber.Length + sequence.Length;
        return transactionNumberLength <= 10;
    }
}
