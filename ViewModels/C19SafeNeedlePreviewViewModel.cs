namespace OpdAccrRptWeb.ViewModels;

public sealed record C19SafeNeedlePreviewRow(
    SafeNeedleReportViewModel? FirstNeedle,
    SafeNeedleReportViewModel? SecondNeedle,
    SafeNeedleReportViewModel? Injection);

public sealed record C19SafeNeedlePreviewPage(
    string Station,
    int PageNumber,
    IReadOnlyList<C19SafeNeedlePreviewRow> Rows);

public sealed record C19SafeNeedlePreviewViewModel(
    string ReportDate,
    string RocReportDate,
    string PrintedAt,
    IReadOnlyList<C19SafeNeedlePreviewPage> Pages)
{
    public static C19SafeNeedlePreviewViewModel Create(
        DateOnly reportDate,
        string encounterSource,
        IEnumerable<SafeNeedleReportViewModel> sourceRows,
        DateTimeOffset printedAt)
    {
        const int rowsPerPage = 7;
        var pages = new List<C19SafeNeedlePreviewPage>();
        var groups = sourceRows.GroupBy(row => encounterSource == "Inpatient"
            ? StationOf(row.BedNumber)
            : "ER");

        foreach (var group in groups.OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            SafeNeedleReportViewModel[] needles = group
                .Where(row => row.Category == "X").OrderBy(row => row.BedNumber, StringComparer.Ordinal)
                .ThenBy(row => row.MedicalRecordNumber, StringComparer.Ordinal)
                .ThenBy(row => row.OrderCode, StringComparer.Ordinal)
                .ThenBy(row => row.OrderDate, StringComparer.Ordinal).ToArray();
            SafeNeedleReportViewModel[] injections = group
                .Where(row => row.Category == "Y").OrderBy(row => row.BedNumber, StringComparer.Ordinal)
                .ThenBy(row => row.MedicalRecordNumber, StringComparer.Ordinal)
                .ThenBy(row => row.OrderCode, StringComparer.Ordinal)
                .ThenBy(row => row.OrderDate, StringComparer.Ordinal).ToArray();
            int rowCount = Math.Max((needles.Length + 1) / 2, injections.Length);
            var combined = new List<C19SafeNeedlePreviewRow>(rowCount);
            for (int index = 0; index < rowCount; index++)
            {
                combined.Add(new C19SafeNeedlePreviewRow(
                    ElementAtOrNull(needles, index * 2),
                    ElementAtOrNull(needles, index * 2 + 1),
                    ElementAtOrNull(injections, index)));
            }
            for (int offset = 0; offset < combined.Count; offset += rowsPerPage)
                pages.Add(new C19SafeNeedlePreviewPage(group.Key, pages.Count + 1,
                    combined.Skip(offset).Take(rowsPerPage).ToArray()));
        }

        return new C19SafeNeedlePreviewViewModel(
            reportDate.ToString("yyyy/MM/dd"),
            $"{reportDate.Year - 1911:000}{reportDate.Month:00}{reportDate.Day:00}",
            printedAt.ToString("yyyy/MM/dd"), pages);
    }

    private static SafeNeedleReportViewModel? ElementAtOrNull(
        SafeNeedleReportViewModel[] rows, int index) => index < rows.Length ? rows[index] : null;

    private static string StationOf(string? bedNumber)
    {
        string bed = bedNumber?.Trim() ?? string.Empty;
        return bed[..Math.Min(bed.Length, 3)];
    }
}
