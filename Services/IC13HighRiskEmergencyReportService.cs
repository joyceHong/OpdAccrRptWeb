using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC13LegacyPhoneMasker
{
    string Mask(string? value);
}

public interface IC13HighRiskEmergencyReportService
{
    C13PreviewViewModel CreatePreview(
        SearchReportCondition condition,
        string generatedBy,
        CancellationToken cancellationToken = default);
}
