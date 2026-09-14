using Microsoft.Extensions.Options;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportExportOptionsTests
{
    [Fact]
    public void Defaults_MatchC174ExportContract()
    {
        var options = new ReportExportOptions();

        Assert.Equal(30_000, options.SynchronousRowLimit);
        Assert.Equal(5_000, options.BatchSize);
        Assert.Equal(72, options.RetentionHours);
    }

    [Theory]
    [InlineData(0, 1, 72, 60)]
    [InlineData(5_000, 0, 72, 60)]
    [InlineData(5_000, 1, 0, 60)]
    [InlineData(5_000, 1, 72, 0)]
    public void Validate_InvalidPositiveSettings_Fails(
        int batchSize,
        int queueCapacity,
        int retentionHours,
        int cleanupIntervalMinutes)
    {
        var result = new ReportExportOptionsValidator().Validate(Options.DefaultName, new ReportExportOptions
        {
            BatchSize = batchSize,
            QueueCapacity = queueCapacity,
            RetentionHours = retentionHours,
            CleanupIntervalMinutes = cleanupIntervalMinutes
        });

        Assert.False(result.Succeeded);
    }
}
