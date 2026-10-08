using System.Globalization;
using Microsoft.Extensions.Options;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed class RegistrationQueryService(
    IRegistrationQueryRepository repository,
    IReportTotalCountCache totals,
    IOrganizationUnitCodeService organizationUnits,
    IOptions<RegistrationQueryOptions>? options = null) : IRegistrationQueryService
{
    private const string ReportCode = "Q3";
    private static readonly int[] PageSizes = [10, 30, 50];
    private readonly bool usesZeroPaddedMedicalRecordNumber =
        options?.Value.UsesZeroPaddedMedicalRecordNumber ?? false;

    public async Task<RegistrationQueryResult> QueryAsync(
        RegistrationQueryRequest request,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidatePage(request.PageNumber, request.PageSize);

        RegistrationQueryFilters filters;
        try
        {
            filters = await NormalizeFiltersAsync(request, token);
        }
        catch (OrganizationUnitMappingAmbiguousException)
        {
            // There is no safe legacy-code filter; show a normal no-results response instead of 400.
            RegistrationQueryMode mode = ParseMode(request.Mode);
            return new(ModeValue(mode), [], 0, 1, request.PageSize, 0, null);
        }

        if (filters.IsSummary)
        {
            RegistrationSummarySource summary = await repository.QuerySummaryAsync(
                filters.RegDate,
                filters.Time,
                filters.Room,
                token);
            return new(
                ModeValue(filters.Mode),
                [],
                summary.TotalCount,
                1,
                request.PageSize,
                0,
                new(
                    FormatRocDate(filters.RegDate),
                    filters.Time,
                    filters.Room,
                    summary.TotalCount,
                    summary.SeenCount,
                    summary.UnseenCount,
                    summary.CancelledNumbers.Count,
                    summary.CancelledNumbers,
                    summary.CurrentNumber,
                    summary.PrebookNumber,
                    summary.RoomNumberDataMissing));
        }

        int totalCount = totals.GetOrCreate(
            ReportCode,
            CacheFilters(filters),
            () => repository.Count(filters));
        int totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        int pageNumber = totalPages == 0
            ? 1
            : Math.Min(request.PageNumber, totalPages);
        IReadOnlyList<RegistrationSource> sources = totalCount == 0
            ? []
            : await repository.QueryPageAsync(
                filters,
                checked((pageNumber - 1) * request.PageSize),
                request.PageSize,
                token);

        return new(
            ModeValue(filters.Mode),
            sources.Select(MapRow).ToArray(),
            totalCount,
            pageNumber,
            request.PageSize,
            totalPages,
            null);
    }

    public async Task<IReadOnlyList<RegistrationSectionOption>> SearchSectionsAsync(
        string query,
        CancellationToken token)
    {
        IReadOnlyList<OrganizationUnitMapping> mappings = await organizationUnits.SearchAsync(
            query,
            includeSections: true,
            includePlaces: false,
            activePlaceOnly: false,
            limit: 20,
            cancellationToken: token);

        return mappings
            .Where(mapping => mapping.LegacyCode.Length > 0 || mapping.NewCode.Length > 0)
            .Select(mapping => new RegistrationSectionOption(
                mapping.LegacyCode.Trim(),
                mapping.NewCode.Trim(),
                mapping.DisplayName.Trim()))
            .DistinctBy(mapping => $"{mapping.LegacyCode}\u001f{mapping.NewCode}", StringComparer.Ordinal)
            .ToArray();
    }

    public Task<IReadOnlyList<RegistrationDoctorOption>> SearchDoctorsAsync(
        string query,
        CancellationToken token) => repository.SearchDoctorsAsync(query, token);

    public async Task<RegistrationQueryFilters> NormalizeFiltersAsync(
        RegistrationQueryRequest request,
        CancellationToken token = default)
    {
        RegistrationQueryMode mode = ParseMode(request.Mode);
        string regDate = NormalizeDate(request.RegDate, "掛號日期");
        string medicalRecordNo = NormalizeUpper(request.MedicalRecordNo, 10);
        string patientId = NormalizeUpper(request.PatientId, 11);
        string birthDate = NormalizeDate(request.BirthDate, "出生日期");
        string legacySection = ExtractCode(request.SectionNo, 10);
        string newSection = ExtractCode(request.NewSectionNo, 10);
        string sectionNo = mode == RegistrationQueryMode.Summary
            ? string.Empty
            : await ResolveSectionCodeAsync(legacySection, newSection, token);
        string room = Normalize(request.Room, 6);
        string time = NormalizeTime(request.Time);
        int? registrationNo = NormalizeRegistrationNo(request.RegistrationNo);
        string doctorNo = ExtractCode(request.DoctorNo, 10).ToUpperInvariant();

        if (mode == RegistrationQueryMode.Summary &&
            (regDate.Length == 0 || time.Length == 0 || room.Length == 0))
        {
            throw new ArgumentException("請輸入完整查詢〔報診〕資料。");
        }

        if (medicalRecordNo.Length > 0 && patientId.Length > 0 &&
            usesZeroPaddedMedicalRecordNumber && medicalRecordNo.Length < 10)
        {
            medicalRecordNo = medicalRecordNo.PadLeft(10, '0');
        }

        IReadOnlyList<string> medicalRecordNumbers = [];
        if (medicalRecordNo.Length > 0 && patientId.Length == 0)
        {
            IReadOnlyList<string> merged = await repository.QueryMergedMedicalRecordNumbersAsync(
                medicalRecordNo,
                token);
            medicalRecordNumbers = merged
                .Select(value => value.Trim())
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            if (medicalRecordNumbers.Count == 0)
            {
                medicalRecordNumbers = [medicalRecordNo];
            }
        }

        return new(
            mode,
            regDate,
            medicalRecordNo,
            patientId,
            birthDate,
            sectionNo,
            room,
            time,
            registrationNo,
            doctorNo,
            medicalRecordNumbers);
    }

    private async Task<string> ResolveSectionCodeAsync(
        string legacySection,
        string newSection,
        CancellationToken token)
    {
        if (newSection.Length == 0)
        {
            return legacySection;
        }

        if (legacySection.Length > 0)
        {
            OrganizationUnitMapping? selectedMapping;
            try
            {
                selectedMapping = await organizationUnits.ResolveNewCodeAsync(
                    legacySection,
                    roomType: string.Empty,
                    scope: OrganizationUnitMappingScope.SectionOnly,
                    cancellationToken: token);
            }
            catch (OrganizationUnitLegacyMappingAmbiguousException)
            {
                // The selected legacy code is still an exact query filter even when its new-code
                // mapping is not unique.
                return legacySection;
            }

            string selectedNewCode = selectedMapping?.NewCode.Trim() ?? string.Empty;
            if (selectedNewCode.Length > 0 &&
                !string.Equals(newSection, selectedNewCode, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("舊科別與新科別代碼不一致。");
            }

            return legacySection;
        }

        OrganizationUnitMapping? mapping = await organizationUnits.ResolveLegacyCodeAsync(
            newSection,
            activePlaceOnly: false,
            cancellationToken: token);
        string resolvedLegacy = mapping?.LegacyCode.Trim() ?? string.Empty;

        return resolvedLegacy.Length > 0 ? resolvedLegacy : newSection;
    }

    private static IReadOnlyDictionary<string, string?> CacheFilters(RegistrationQueryFilters filters) =>
        new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["mode"] = ModeValue(filters.Mode),
            ["regDate"] = filters.RegDate,
            ["medicalRecordNo"] = filters.MedicalRecordNo,
            ["patientId"] = filters.PatientId,
            ["birthDate"] = filters.BirthDate,
            ["sectionNo"] = filters.SectionNo,
            ["room"] = filters.Room,
            ["time"] = filters.Time,
            ["registrationNo"] = filters.RegistrationNo?.ToString(CultureInfo.InvariantCulture),
            ["doctorNo"] = filters.DoctorNo,
            ["medicalRecordNumbers"] = string.Join("\u001f", filters.MedicalRecordNumbers)
        };

    private static RegistrationQueryMode ParseMode(string? value)
    {
        string normalized = (value ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "" or "registered" => RegistrationQueryMode.Registered,
            "cancelled" or "canceled" => RegistrationQueryMode.Cancelled,
            "unseen" => RegistrationQueryMode.Unseen,
            "seen" => RegistrationQueryMode.Seen,
            "unpriced" => RegistrationQueryMode.Unpriced,
            "summary" or "report" => RegistrationQueryMode.Summary,
            _ => throw new ArgumentException("查詢模式不正確。")
        };
    }

    private static string ModeValue(RegistrationQueryMode mode) => mode switch
    {
        RegistrationQueryMode.Registered => "registered",
        RegistrationQueryMode.Cancelled => "cancelled",
        RegistrationQueryMode.Unseen => "unseen",
        RegistrationQueryMode.Seen => "seen",
        RegistrationQueryMode.Unpriced => "unpriced",
        RegistrationQueryMode.Summary => "summary",
        _ => throw new ArgumentOutOfRangeException(nameof(mode))
    };

    private static string NormalizeDate(string? value, string fieldName)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        if (DateOnly.TryParseExact(normalized, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly gregorian))
        {
            return ToRocDate(gregorian);
        }

        string roc = normalized.Replace("/", string.Empty, StringComparison.Ordinal);
        if (roc.Length == 7 &&
            int.TryParse(roc[..3], NumberStyles.None, CultureInfo.InvariantCulture, out int rocYear) &&
            int.TryParse(roc[3..5], NumberStyles.None, CultureInfo.InvariantCulture, out int month) &&
            int.TryParse(roc[5..7], NumberStyles.None, CultureInfo.InvariantCulture, out int day) &&
            DateOnly.TryParseExact(
                $"{rocYear + 1911:0000}-{month:00}-{day:00}",
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            return roc;
        }

        throw new ArgumentException($"{fieldName}格式不正確。");
    }

    private static string ToRocDate(DateOnly date) =>
        $"{date.Year - 1911:000}{date:MMdd}";

    private static string NormalizeUpper(string? value, int maxLength) =>
        Normalize(value, maxLength).ToUpperInvariant();

    private static string Normalize(string? value, int maxLength)
    {
        string normalized = (value ?? string.Empty).Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    private static string ExtractCode(string? value, int maxLength)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return string.Empty;
        }

        string code = normalized.Split(['｜', '|', ' ', '\t'], StringSplitOptions.RemoveEmptyEntries)[0];
        return Normalize(code, maxLength);
    }

    private static string NormalizeTime(string? value)
    {
        string normalized = Normalize(value, 1);
        if (normalized.Length > 0 && normalized is not ("1" or "2" or "3"))
        {
            throw new ArgumentException("時段必須為 1、2 或 3。");
        }

        return normalized;
    }

    private static int? NormalizeRegistrationNo(string? value)
    {
        string normalized = (value ?? string.Empty).Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        if (normalized.Length > 3 ||
            !int.TryParse(normalized, NumberStyles.None, CultureInfo.InvariantCulture, out int number))
        {
            throw new ArgumentException("掛號序號格式不正確。");
        }

        return number;
    }

    private static void ValidatePage(int pageNumber, int pageSize)
    {
        if (pageNumber < 1 || !PageSizes.Contains(pageSize))
        {
            throw new ArgumentException("分頁條件不正確。");
        }
    }

    private static RegistrationRow MapRow(RegistrationSource source)
    {
        string rawDate = Text(source.RegistrationDate);
        string time = Text(source.Time);
        int registrationNo = source.RegistrationNo ?? 0;
        string room = Text(source.Room);
        string medicalRecordNo = Text(source.MedicalRecordNo);
        string registrationKey = string.Join("|", rawDate, time, room,
            registrationNo.ToString(CultureInfo.InvariantCulture), medicalRecordNo);
        bool cancelled = Text(source.CancelledFlag) == "1";
        string state = cancelled
            ? "DC"
            : CompareStatus(source.DigitalStatus)
                ? "End"
                : Text(source.FirstVisitFlag) == "0" ? "1st" : string.Empty;

        return new(
            registrationKey,
            RegistrationType(source.RegistrationType),
            FormatRocDate(rawDate),
            rawDate,
            time,
            registrationNo,
            room,
            medicalRecordNo,
            Text(source.PatientName),
            Text(source.PatientId),
            FormatRocDate(Text(source.BirthDate)),
            Text(source.Sex),
            Text(source.SectionCode),
            Text(source.SectionName),
            Text(source.DoctorCode),
            Text(source.DoctorName),
            Text(source.Financial1),
            Text(source.Financial2),
            Text(source.PaymentType),
            Text(source.Sequence),
            Text(source.RoomAddress),
            Text(source.CreatedBy),
            Text(source.CreatedDate),
            Text(source.CancelledBy),
            Text(source.CancelledDate),
            state,
            IsPriced(source.QuoteDisplayFlag) ? "Price" : string.Empty,
            Text(source.EmergencyReturnFlag) == "1" ? "Y" : string.Empty,
            cancelled);
    }

    public static string FormatRocDate(string? value)
    {
        string normalized = Text(value);
        return normalized.Length < 7
            ? string.Empty
            : $"{normalized[..3]}/{normalized.Substring(3, 2)}/{normalized.Substring(5, 2)}";
    }

    private static bool CompareStatus(string? value)
    {
        string normalized = Text(value);
        return normalized.Length > 0 && normalized != "0";
    }

    private static bool IsPriced(string? value) =>
        Text(value) is "Y" or "S" or "U" or "Q";

    private static string RegistrationType(string? value)
    {
        _ = int.TryParse(Text(value), NumberStyles.Integer, CultureInfo.InvariantCulture, out int type);
        return type switch
        {
            1 or 2 => "現掛",
            3 or 4 => "當預",
            5 or 6 => "當診語音",
            >= 11 and <= 14 => "預約",
            15 or 16 => "預約語音",
            17 => "診間預約",
            _ => "其它"
        };
    }

    private static string Text(string? value) => value?.Trim() ?? string.Empty;
}
