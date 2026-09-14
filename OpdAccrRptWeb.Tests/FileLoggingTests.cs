using Microsoft.Extensions.Configuration;
using OpdAccrRptWeb.Infrastructure;

namespace OpdAccrRptWeb.Tests;

public sealed class FileLoggingTests
{
    [Fact]
    public void Startup_WithWritableLogPath_CreatesRollingLogFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"opd-log-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            var logger = FileLoggingConfiguration.CreateLogger(
                CreateConfiguration(Path.Combine(directory, "opd-accr-rpt-.log"), 14),
                directory,
                includeConsole: false);
            logger.Error(new InvalidOperationException("controlled failure"), "startup smoke test");
            (logger as IDisposable)?.Dispose();

            Assert.NotEmpty(Directory.GetFiles(directory, "opd-accr-rpt-*.log"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Startup_WithInvalidRetention_FailsVisibly()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            FileLoggingConfiguration.CreateLogger(
                CreateConfiguration("logs/invalid-.log", 0),
                Directory.GetCurrentDirectory(),
                includeConsole: false));

        Assert.Contains("FileLogging", exception.ToString());
    }

    [Fact]
    public void Startup_WithUnwritableShape_FailsVisibly()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"opd-log-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var parentFile = Path.Combine(directory, "not-a-directory");
        File.WriteAllText(parentFile, "occupied");
        try
        {
            Assert.ThrowsAny<Exception>(() => FileLoggingConfiguration.CreateLogger(
                CreateConfiguration(Path.Combine(parentFile, "opd-.log"), 14),
                directory,
                includeConsole: false));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static IConfiguration CreateConfiguration(string path, int retainedFileCountLimit)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FileLogging:Path"] = path,
                ["FileLogging:RetainedFileCountLimit"] = retainedFileCountLimit.ToString()
            })
            .Build();
    }
}
