namespace OpdAccrRptWeb.ViewModels;

public enum C141LineBotMessageKind
{
    PaymentNotice,
    CardReminder
}

public sealed record C141LineBotCase(
    C141AdmissionKey Key,
    string? MedicalRecordNumber,
    string? PatientName,
    string? BedNumber,
    string? InsuranceIdentity,
    string? PaymentType,
    string? InsuranceCategory,
    string? InpatientSequence,
    string? HealthCardNumber,
    decimal? ReceivableAmount,
    decimal? PayTotal,
    string? ContactMedicalRecordNumber,
    string? ContactPatientId)
{
    public decimal NetAmount => (ReceivableAmount ?? 0m) - (PayTotal ?? 0m);
}

public sealed record C141LineBotRequest(
    C141LineBotMessageKind Kind,
    IReadOnlyList<C141AdmissionKey>? Cases);

public sealed record C141LineBotDelivery(
    C141AdmissionKey Key,
    string Status,
    string Message);

public sealed record C141LineBotBatchResult(IReadOnlyList<C141LineBotDelivery> Results)
{
    public int Sent => Results.Count(result => result.Status is "Sent" or "SentButNotRecorded");
    public int Skipped => Results.Count(result => result.Status == "Skipped");
    public int Failed => Results.Count(result => result.Status is "Failed" or "Unknown");
    public int SentButNotRecorded => Results.Count(result => result.Status == "SentButNotRecorded");
}

public sealed record C141LineBotPayload(
    DateTimeOffset? ScheduledTime,
    int Type,
    string Num,
    string ReceivePId,
    int Priority,
    string? Title,
    string? Message,
    IReadOnlyList<string> TitleParms,
    IReadOnlyList<string> MessageParms,
    bool LogErrorMessage);

public sealed record C141LineBotSendResult(bool Success);
