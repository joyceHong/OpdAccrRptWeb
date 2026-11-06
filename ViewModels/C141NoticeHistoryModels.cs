namespace OpdAccrRptWeb.ViewModels;

public sealed record C141NoticeHistoryRequest(string? StartDate, string? EndDate);

public sealed record C141NoticeHistoryRow(
    string? MedicalRecordNumber,
    string? NoticeDate,
    string? NoticeMethod,
    string? PaymentDate,
    decimal? NoticeAmount,
    decimal? CashAmount,
    decimal? CreditCardAmount);
