using System.Globalization;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed class MedicalRecordQueryService(
    IMedicalRecordQueryRepository repository,
    IReportTotalCountCache totals) : IMedicalRecordQueryService
{
    private const string ReportCode = "Q2";
    private static readonly int[] PageSizes = [10, 30, 50];

    public async Task<MedicalRecordQueryPage> QueryAsync(
        MedicalRecordQueryRequest request,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request);
        MedicalRecordQueryFilters filters = NormalizeFilters(request);
        if (!filters.HasAnyCondition)
        {
            throw new ArgumentException("本資料庫可能很大不可使用自由查詢,請輸入資料再查");
        }

        ValidatePage(request.PageNumber, request.PageSize);
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
        IReadOnlyList<MedicalRecordSource> sources = totalCount == 0
            ? []
            : await repository.QueryPageAsync(
                filters,
                checked((pageNumber - 1) * request.PageSize),
                request.PageSize,
                token);

        return new(
            sources.Select(MapRow).ToArray(),
            totalCount,
            pageNumber,
            request.PageSize,
            totalPages);
    }

    public async Task<MedicalRecordDetail> QueryDetailAsync(
        MedicalRecordDetailRequest request,
        CancellationToken token)
    {
        ArgumentNullException.ThrowIfNull(request);
        string medicalRecordNo = NormalizeMedicalRecordNumber(request.MedicalRecordNo);
        if (medicalRecordNo.Length == 0)
        {
            throw new ArgumentException("請提供病歷號。");
        }

        MedicalRecordDetailSource source = await repository.QueryDetailAsync(medicalRecordNo, token)
            ?? throw new KeyNotFoundException();
        IReadOnlyList<string> mergedNumbers = await repository.QueryMergedMedicalRecordNumbersAsync(
            medicalRecordNo,
            token);
        string[] debtNumbers = mergedNumbers.Count == 0
            ? [medicalRecordNo]
            : mergedNumbers.Distinct(StringComparer.Ordinal).ToArray();
        decimal debtTotal = await repository.QueryDebtTotalAsync(debtNumbers, token) ?? 0m;
        return MapDetail(source, debtTotal);
    }

    public static MedicalRecordQueryFilters NormalizeFilters(MedicalRecordQueryRequest request)
    {
        return new(
            NormalizeLegacyValue(request.MedicalRecordNo, 10, true),
            NormalizeLegacyValue(request.IdentityNumber, 10, false),
            NormalizeLegacyValue(request.Name, 10, false),
            NormalizeLegacyValue(request.Address1, 60, false),
            NormalizeLegacyValue(request.Address2, 60, false),
            NormalizeLegacyValue(request.NewIdentityNumber, 10, false),
            NormalizeLegacyValue(request.NewName, 10, false),
            NormalizeNewBirthday(request.NewBirthday));
    }

    public static string NormalizeMedicalRecordNumber(string? value) =>
        NormalizeLegacyValue(value, 10, true);

    public static string MaskHomePhone(string? value)
    {
        string trimmed = (value ?? string.Empty).TrimEnd();
        if (trimmed.Length == 0)
        {
            return string.Empty;
        }

        int visibleLength = Math.Min(4, trimmed.Length);
        return trimmed[..visibleLength] + new string('*', trimmed.Length - visibleLength);
    }

    private static IReadOnlyDictionary<string, string?> CacheFilters(MedicalRecordQueryFilters filters) =>
        new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["medicalRecordNo"] = filters.MedicalRecordNo,
            ["identityNumber"] = filters.IdentityNumber,
            ["name"] = filters.Name,
            ["address1"] = filters.Address1,
            ["address2"] = filters.Address2,
            ["newIdentityNumber"] = filters.NewIdentityNumber,
            ["newName"] = filters.NewName,
            ["newBirthday"] = filters.NewBirthday
        };

    private static void ValidatePage(int pageNumber, int pageSize)
    {
        if (pageNumber < 1 || !PageSizes.Contains(pageSize))
        {
            throw new ArgumentException("分頁條件不正確。");
        }
    }

    private static string NormalizeNewBirthday(string? value)
    {
        string trimmed = (value ?? string.Empty).Trim();
        if (trimmed.Length == 0 || trimmed is "!" or "#")
        {
            return trimmed;
        }

        string? comparison = GetComparisonOperator(trimmed);
        string operand = comparison is null ? trimmed : trimmed[comparison.Length..];
        if (operand.Contains('-'))
        {
            if (!DateOnly.TryParseExact(operand, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateOnly date))
            {
                throw new ArgumentException("新生日格式不正確。");
            }

            operand = ToRocDate(date);
        }

        string normalized = (comparison ?? string.Empty) + operand;
        return Truncate(normalized, 10);
    }

    private static string NormalizeLegacyValue(string? value, int maxLength, bool uppercase)
    {
        string normalized = Truncate((value ?? string.Empty).Trim(), maxLength);
        return uppercase ? normalized.ToUpperInvariant() : normalized;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    private static string? GetComparisonOperator(string value) =>
        value.StartsWith(">=", StringComparison.Ordinal) || value.StartsWith("<=", StringComparison.Ordinal) || value.StartsWith("<>", StringComparison.Ordinal)
            ? value[..2]
            : value.StartsWith('>') || value.StartsWith('<')
                ? value[..1]
                : null;

    private static string ToRocDate(DateOnly date) => $"{date.Year - 1911:000}{date:MMdd}";

    private static MedicalRecordRow MapRow(MedicalRecordSource source) => new(
        Text(source.MedicalRecordNo), Text(source.IdentityNumber), Text(source.Name), Text(source.Birthday),
        Text(source.MaritalStatus), Text(source.Sex), Text(source.BloodType), Text(source.InsuranceType),
        Text(source.DiscountType), MaskHomePhone(source.HomePhone), Text(source.OfficePhone),
        Text(source.SecretLevel), Text(source.PatientCondition));

    private static MedicalRecordDetail MapDetail(MedicalRecordDetailSource source, decimal debtTotal) => new(
        Text(source.MedicalRecordNo), Text(source.IdentityNumber), Text(source.Name), Text(source.Birthday),
        Text(source.MaritalStatus), Text(source.Sex), Text(source.BloodType), Text(source.InsuranceType),
        Text(source.DiscountType), MaskHomePhone(source.HomePhone), Text(source.OfficePhone),
        Text(source.SecretLevel), Text(source.PatientCondition), Text(source.ZipCode1), Text(source.Address1),
        Text(source.ZipCode2), Text(source.Address2), Text(source.BirthPlace), Text(source.Race),
        Text(source.Language), Text(source.NativePlace), Text(source.NewIdentityNumber), Text(source.NewName),
        Text(source.NewBirthday), Text(source.SpecialName), Text(source.OldMedicalRecordNo),
        Text(source.SocialServiceStatus), Text(source.FirstVisitDate), debtTotal, Text(source.SpouseId),
        Text(source.FatherId));

    private static string Text(string? value) => value?.Trim() ?? string.Empty;
}
