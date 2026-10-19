using System.Globalization;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC1SurgicalPrintService
{
    C1SurgicalPrintPreviewViewModel CreatePreview(
        SearchReportCondition condition, string generatedBy, CancellationToken cancellationToken = default);
}

public sealed class C1SurgicalPrintService(ISurgicalAccountingRepository repository) : IC1SurgicalPrintService
{
    private const int RowsPerPage = 16;

    public C1SurgicalPrintPreviewViewModel CreatePreview(
        SearchReportCondition condition, string generatedBy, CancellationToken cancellationToken = default)
    {
        DateOnly start = DateOnly.ParseExact(condition.StartDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        DateOnly end = DateOnly.ParseExact(condition.EndDate!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var query = new SearchReportCondition
        {
            ReportCode = "C1",
            StartDate = DateTimeExtensions.ToRocDateString(start.ToDateTime(TimeOnly.MinValue)),
            EndDate = DateTimeExtensions.ToRocDateString(end.ToDateTime(TimeOnly.MinValue))
        };
        List<SurgicalAccountingReportViewModel> rows = repository.GetAll(query, cancellationToken);
        var groups = rows
            .GroupBy(row => row.EncounterType?.Trim() ?? string.Empty)
            .OrderBy(group => group.Key.StartsWith("e", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(group => group.Key, StringComparer.Ordinal);
        var pageData = new List<(string EncounterType, IReadOnlyList<SurgicalAccountingReportViewModel> Rows)>();
        foreach (var group in groups)
        {
            SurgicalAccountingReportViewModel[] groupRows = group.ToArray();
            for (int offset = 0; offset < groupRows.Length; offset += RowsPerPage)
            {
                cancellationToken.ThrowIfCancellationRequested();
                pageData.Add((group.Key, groupRows.Skip(offset).Take(RowsPerPage).ToArray()));
            }
        }
        C1SurgicalPrintPage[] pages = pageData
            .Select((page, index) => new C1SurgicalPrintPage(
                page.EncounterType, index + 1, pageData.Count, page.Rows))
            .ToArray();
        DateTime generatedAt = DateTime.Now;
        return new C1SurgicalPrintPreviewViewModel(
            FormatRocDate(query.StartDate), FormatRocDate(query.EndDate),
            $"{FormatRocDate(DateTimeExtensions.ToRocDateString(generatedAt))} {generatedAt:HH:mm:ss}",
            generatedBy, pages);
    }

    private static string FormatRocDate(string? value) => value is { Length: 7 }
        ? $"{value[..3]}/{value[3..5]}/{value[5..7]}" : value ?? string.Empty;
}
