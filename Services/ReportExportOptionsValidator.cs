using Microsoft.Extensions.Options;

namespace OpdAccrRptWeb.Services;

public sealed class ReportExportOptionsValidator : IValidateOptions<ReportExportOptions>
{
    public ValidateOptionsResult Validate(string? name, ReportExportOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.RootDirectory))
        {
            return ValidateOptionsResult.Fail("ReportExport:RootDirectory 不可為空白。");
        }
        if (options.SynchronousRowLimit < 0
            || options.BatchSize <= 0
            || options.QueueCapacity <= 0
            || options.RetentionHours <= 0
            || options.CleanupIntervalMinutes <= 0)
        {
            return ValidateOptionsResult.Fail("ReportExport 數值設定必須符合允許範圍。");
        }
        return ValidateOptionsResult.Success;
    }
}
