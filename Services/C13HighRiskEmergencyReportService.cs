using System.Text;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C13LegacyPhoneMasker : IC13LegacyPhoneMasker
{
    private static readonly Encoding Big5 = CreateEncoding();

    public string Mask(string? value)
    {
        string trimmed = value?.Trim() ?? string.Empty;
        byte[] bytes = Big5.GetBytes(trimmed);
        if (bytes.Length <= 4) return trimmed;

        int prefixLength = 4;
        while (prefixLength > 0)
        {
            string prefix = Big5.GetString(bytes, 0, prefixLength);
            if (!prefix.Contains('\uFFFD', StringComparison.Ordinal))
                return prefix + new string('*', bytes.Length - prefixLength);
            prefixLength--;
        }
        return new string('*', bytes.Length);
    }

    private static Encoding CreateEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(950, EncoderFallback.ExceptionFallback, DecoderFallback.ReplacementFallback);
    }
}

public sealed class C13HighRiskEmergencyReportService(
    IC13HighRiskEmergencyRepository repository,
    TimeProvider timeProvider) : IC13HighRiskEmergencyReportService
{
    public C13PreviewViewModel CreatePreview(
        SearchReportCondition condition,
        string generatedBy,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTimeOffset generatedAt = timeProvider.GetLocalNow();
        IReadOnlyList<C13HighRiskEmergencyReportViewModel> rows =
            repository.GetAllForPreview(condition, cancellationToken);
        return new C13PreviewViewModel
        {
            StartDate = ToRocDisplay(condition.StartDate),
            EndDate = ToRocDisplay(condition.EndDate),
            GeneratedAt = $"{generatedAt.Year - 1911:000}/{generatedAt:MM/dd  HH:mm:ss}",
            GeneratedBy = generatedBy,
            Rows = rows
        };
    }

    private static string ToRocDisplay(string? value) =>
        DateOnly.TryParseExact(value, "yyyy-MM-dd", out DateOnly date)
            ? $"{date.Year - 1911:000}/{date:MM/dd}"
            : string.Empty;
}
