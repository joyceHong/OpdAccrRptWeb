namespace OpdAccrRptWeb.Services;

public sealed class ReportExportOptions
{
    public const string SectionName = "ReportExport";

    public string RootDirectory { get; set; } = "App_Data/report-exports";

    public int SynchronousRowLimit { get; set; } = 30_000;

    public int BatchSize { get; set; } = 5_000;

    public int QueueCapacity { get; set; } = 10;

    public int RetentionHours { get; set; } = 72;

    public int CleanupIntervalMinutes { get; set; } = 60;
}
