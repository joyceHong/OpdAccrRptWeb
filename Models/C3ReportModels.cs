using System.Globalization;

namespace OpdAccrRptWeb.Models;

public enum CareSource { O, I }
public enum ReportDetailType { Summary = 0, Detail = 1 }
public enum LogisticsType { All = 0, Logistics = 1, NonLogistics = 2 }
public enum DepartmentFilterMode
{
    None, Station, Sys56, Sys53Wound, Sys52, Sys51, Sys39, Sys38, Hd3, Hd4, Hd5, Pd,
    Er, Anesthesia, OperatingRoom, Endoscopy, Beauty, Cath, Radiology, RadiologyTechnology,
    Ent, Delivery, Pharmacy, Dispensing, GeneralLocation
}

public sealed record C3ReportRequest(
    string StartDate,
    string EndDate,
    CareSource Source,
    ReportDetailType DetailType = ReportDetailType.Summary,
    LogisticsType LogisticsType = LogisticsType.All,
    string? DepartmentCode = null,
    IReadOnlyList<string>? RoomCodes = null,
    IReadOnlyList<string>? ChargeCodes = null,
    int PageNumber = 1,
    int PageSize = 10)
{
    public C3ValidatedRequest Validate()
    {
        if (!Enum.IsDefined(Source) || !Enum.IsDefined(DetailType) || !Enum.IsDefined(LogisticsType))
            throw new ArgumentException("C3 來源、明細或物流條件不正確。");
        if (!DateOnly.TryParseExact(StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly start) || start.Year < 1912)
            throw new ArgumentException("請輸入有效的 C3 起始日期。");
        if (!DateOnly.TryParseExact(EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly end) || end.Year < 1912)
            throw new ArgumentException("請輸入有效的 C3 截止日期。");
        if (start > end) throw new ArgumentException("C3 起始日期不可晚於截止日期。");
        if (PageNumber < 1 || PageSize is not (10 or 30 or 50))
            throw new ArgumentException("C3 分頁條件不正確。");
        IReadOnlyList<string> rooms = NormalizeCodes(RoomCodes, "診間");
        IReadOnlyList<string> charges = NormalizeCodes(ChargeCodes, "批價碼");
        string? department = NormalizeCode(DepartmentCode);
        return new C3ValidatedRequest(start, end, ToRoc(start), ToRoc(end), Source, DetailType,
            LogisticsType, department, rooms, charges, PageNumber, PageSize);
    }

    public static IReadOnlyList<string> ParseCodes(string? value) => string.IsNullOrWhiteSpace(value)
        ? [] : NormalizeCodes(value.Split(',', StringSplitOptions.RemoveEmptyEntries), "代碼");

    private static IReadOnlyList<string> NormalizeCodes(IEnumerable<string>? values, string label)
    {
        string[] result = (values ?? []).Select(NormalizeCode).Where(x => x is not null)
            .Select(x => x!).Distinct(StringComparer.Ordinal).ToArray();
        if (result.Length > 50) throw new ArgumentException($"C3 {label}最多接受 50 筆。");
        return result;
    }

    private static string? NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string normalized = value.Trim().ToUpperInvariant();
        if (normalized.IndexOfAny(['\'', '"', ';', '-', '/', '*']) >= 0)
            throw new ArgumentException("C3 代碼格式不正確。");
        return normalized;
    }

    internal static string ToRoc(DateOnly value) => $"{value.Year - 1911:000}{value:MMdd}";
}

public sealed record C3ValidatedRequest(DateOnly Start, DateOnly End, string RocStartDate,
    string RocEndDate, CareSource Source, ReportDetailType DetailType, LogisticsType LogisticsType,
    string? DepartmentCode, IReadOnlyList<string> RoomCodes, IReadOnlyList<string> ChargeCodes,
    int PageNumber, int PageSize);

public sealed record C3MovementRow(
    int MovementType, string RunDate, string Room, string Station, string ChargeCode,
    string SectionCode, string SystemCode, string? DctNo, string? MrNo, string? PatientName,
    string? DoctorName, string? SelfPay, string MaterialName, string MaterialCode,
    string InventoryFlag, string? Project, string? ClaimCode, decimal SignedQuantity);

public sealed record DepartmentAssignment(string Diagnose, string Dispensary, string Section);
public sealed record SectionMapping(string NewCode, string DisplayName);

public sealed record C3ReportRow(
    string Diagnose, string? Dispensary, string? Section, string ChargeCode, string MaterialCode,
    string MaterialName, string InventoryType, decimal TotalSum, string? DctNo, string? MrNo,
    string? PName, string? Op1Date, string? DrName, string? SPay);
