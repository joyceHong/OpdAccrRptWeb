using System.Globalization;

namespace OpdAccrRptWeb.Models;

public sealed record C4MaterialReportRequest(
    string StartDate, string EndDate, string? SectionPrefix = null,
    int PageNumber = 1, int PageSize = 10)
{
    public C4ValidatedRequest Validate()
    {
        if (!DateOnly.TryParseExact(StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly start) || start.Year < 1912)
            throw new ArgumentException("請輸入有效的 C4 起始日期。");
        if (!DateOnly.TryParseExact(EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly end) || end.Year < 1912)
            throw new ArgumentException("請輸入有效的 C4 截止日期。");
        if (start > end) throw new ArgumentException("C4 起始日期不可晚於截止日期。");
        if (PageNumber < 1 || PageSize is not (10 or 30 or 50))
            throw new ArgumentException("C4 分頁條件不正確。");
        string? prefix = string.IsNullOrWhiteSpace(SectionPrefix)
            ? null : SectionPrefix.Trim().ToUpperInvariant();
        if (prefix?.IndexOfAny(['\'', '"', ';', '-', '/', '*', '%', '_']) >= 0)
            throw new ArgumentException("C4 科別／部門代碼格式不正確。");
        return new(start, end, ToRoc(start), ToRoc(end), prefix, PageNumber, PageSize);
    }

    internal static string ToRoc(DateOnly value) => $"{value.Year - 1911:000}{value:MMdd}";
}

public sealed record C4ValidatedRequest(DateOnly Start, DateOnly End, string RocStartDate,
    string RocEndDate, string? SectionPrefix, int PageNumber, int PageSize);

public sealed record C4MaterialSourceRow(string RoomType, string? Detail, string? MaterialCode,
    string? ClaimCode, string? OrderName, string? Unit, string? ChargeCode,
    string? LegacySectionCode, decimal Total);

public sealed record C4MaterialReportRow(int Id, string Type, string? MaterialCode,
    string? ClaimCode, string? OrderName, string? Unit, string? ChargeCode,
    decimal Total, string? SectionCode);

public sealed record C4ReportMetadata(string DateB, string DateE, string UserId, string Today);
