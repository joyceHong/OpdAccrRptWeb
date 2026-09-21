using System.Globalization;

namespace OpdAccrRptWeb.Models;

public enum C5DataSource { Outpatient, Inpatient }
public enum C5DetailType { Summary = 0, Daily = 1, PatientDetail = 2 }
public enum C5EncounterType { All = 0, Emergency = 1, Outpatient = 2 }
public enum C5ChargeKind { Drug = 0, Order = 1 }

public sealed record C5ReportRequest(
    string StartDate,
    string EndDate,
    C5DataSource DataSource = C5DataSource.Outpatient,
    C5DetailType DetailType = C5DetailType.Summary,
    C5EncounterType EncounterType = C5EncounterType.All,
    C5ChargeKind ChargeKind = C5ChargeKind.Drug,
    string? NewOrganizationUnitCode = null,
    string? LegacySectionCode = null,
    string? RoomNo = null,
    string? ChargeCode = null,
    string? InsuranceIdentityCode = null,
    int PageNumber = 1,
    int PageSize = 10)
{
    public C5ValidatedRequest Validate()
    {
        if (!Enum.IsDefined(DataSource) || !Enum.IsDefined(DetailType) ||
            !Enum.IsDefined(EncounterType) || !Enum.IsDefined(ChargeKind))
        {
            throw new ArgumentException("C5 查詢選項不正確。");
        }

        if (!DateOnly.TryParseExact(StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly start) || start.Year < 1912)
        {
            throw new ArgumentException("請輸入有效的 C5 起始日期。", nameof(StartDate));
        }
        if (!DateOnly.TryParseExact(EndDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly end) || end.Year < 1912)
        {
            throw new ArgumentException("請輸入有效的 C5 截止日期。", nameof(EndDate));
        }
        if (start > end)
        {
            throw new ArgumentException("C5 起始日期不可晚於截止日期。", nameof(StartDate));
        }
        if (PageNumber < 1 || PageSize is not (10 or 30 or 50))
        {
            throw new ArgumentException("C5 分頁條件不正確。");
        }

        string? newCode = NormalizeCode(NewOrganizationUnitCode);
        string? legacyCode = NormalizeCode(LegacySectionCode);
        if (newCode is not null && legacyCode is not null)
        {
            throw new ArgumentException("新科別代碼與舊科別代碼不得同時輸入。",
                nameof(NewOrganizationUnitCode));
        }

        bool inpatient = DataSource == C5DataSource.Inpatient;
        return new C5ValidatedRequest(
            start, end, ToRoc(start), ToRoc(end), DataSource, DetailType,
            inpatient ? C5EncounterType.All : EncounterType, ChargeKind, newCode, legacyCode,
            inpatient ? null : NormalizeCode(RoomNo), NormalizeCode(ChargeCode),
            NormalizeCode(InsuranceIdentityCode), PageNumber, PageSize);
    }

    internal static string ToRoc(DateOnly value) => $"{value.Year - 1911:000}{value:MMdd}";

    private static string? NormalizeCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string normalized = value.Trim().ToUpperInvariant();
        if (normalized.IndexOfAny(['\'', '"', ';']) >= 0)
        {
            throw new ArgumentException("C5 代碼格式不正確。");
        }
        return normalized;
    }
}

public sealed record C5ValidatedRequest(
    DateOnly Start,
    DateOnly End,
    string RocStartDate,
    string RocEndDate,
    C5DataSource DataSource,
    C5DetailType DetailType,
    C5EncounterType EncounterType,
    C5ChargeKind ChargeKind,
    string? NewOrganizationUnitCode,
    string? LegacySectionCode,
    string? RoomNo,
    string? ChargeCode,
    string? InsuranceIdentityCode,
    int PageNumber,
    int PageSize);

public sealed record C5SourceRow(string RoomType, string LegacySectionCode, string ChargeCode,
    string ChargeName, string ServiceDate, string? DoctorId, string? DoctorName,
    string? MedicalRecordNo, string? PatientName, string? SPay, decimal Quantity,
    decimal Pric1, decimal Pric2, decimal Amount1, decimal Amount2, decimal? Sub3);

public sealed record C5ReportRow(string RoomType, string SectionCode, string? SectionName,
    string ChargeCode, string ChargeName, string ServiceDate, string? DoctorId, string? DoctorName,
    string? MedicalRecordNo, string? PatientName, decimal Quantity, decimal UnitPrice, decimal Amount);
