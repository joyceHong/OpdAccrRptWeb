using System.Globalization;

namespace OpdAccrRptWeb.Models;

public enum C7ChargeKind { Drug = 0, Order = 1 }

public sealed record C7ReportRequest(string StartDate, string EndDate, string StartTime = "0000",
    string EndTime = "2359", string InputUserId = "", C7ChargeKind ChargeKind = C7ChargeKind.Drug,
    int PageNumber = 1, int PageSize = 10)
{
    public C7ValidatedRequest Validate()
    {
        if (!DateOnly.TryParseExact(StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var start) || start.Year < 1912)
            throw new ArgumentException("請輸入有效的 C7 起始日期。");
        if (!DateOnly.TryParseExact(EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var end) || end.Year < 1912)
            throw new ArgumentException("請輸入有效的 C7 截止日期。");
        if (start > end) throw new ArgumentException("C7 起始日期不可晚於截止日期。");
        if (!ValidTime(StartTime) || !ValidTime(EndTime))
            throw new ArgumentException("C7 時間必須是有效的 4 碼 HHmm。");
        if (string.CompareOrdinal(StartTime, EndTime) > 0)
            throw new ArgumentException("C7 起始時間不可晚於截止時間。");
        if (string.IsNullOrWhiteSpace(InputUserId))
            throw new ArgumentException("請選擇輸入人員。");
        string userId = InputUserId.Trim().ToUpperInvariant();
        if (!Enum.IsDefined(ChargeKind) || PageNumber < 1 || PageSize is not (10 or 30 or 50))
            throw new ArgumentException("C7 查詢或分頁條件不正確。");
        return new(start, end, ToRoc(start), ToRoc(end), StartTime, EndTime, userId,
            ChargeKind, PageNumber, PageSize);
    }

    public static string ToRoc(DateOnly value) => $"{value.Year - 1911:000}{value:MMdd}";
    private static bool ValidTime(string value) => value?.Length == 4 &&
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int time) &&
        time / 100 is >= 0 and <= 23 && time % 100 is >= 0 and <= 59;
}

public sealed record C7ValidatedRequest(DateOnly Start, DateOnly End, string RocStartDate,
    string RocEndDate, string StartTime, string EndTime, string InputUserId,
    C7ChargeKind ChargeKind, int PageNumber, int PageSize);
public sealed record C7InputUser(string UserId, string UserName);
public sealed record C7SourceRow(string? IdentityCode, string? LegacySectionCode,
    string? ChargeCode, string? ChargeName, string? SelfPayCode, string? Status,
    decimal? Quantity, string? InputUserId, decimal? InsuranceUnitPrice,
    decimal? SelfPayUnitPrice, decimal? InsuranceAmount, decimal? SelfPayAmount,
    string? InputUserName, string? VisitDate, string? VisitTime,
    string? MedicalRecordNo, string? RoomType);
public sealed record C7ReportRow(string VisitDate, string VisitTime, string SectionCode,
    string EncounterLabel, string? Status, string ChargeCode, string ChargeName,
    decimal UnitPrice, decimal Quantity, decimal Amount, string MedicalRecordNo,
    string IdentityCode, string InputUserId, string InputUserName);
