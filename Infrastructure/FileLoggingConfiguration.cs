using Serilog;

namespace OpdAccrRptWeb.Infrastructure;

internal static class FileLoggingConfiguration
{
    internal static Serilog.ILogger CreateLogger(
        IConfiguration configuration,
        string contentRootPath,
        bool includeConsole)
    {
        var logPath = configuration["FileLogging:Path"] ?? "logs/opd-accr-rpt-.log";
        var retainedFileCountLimit = configuration.GetValue<int?>("FileLogging:RetainedFileCountLimit") ?? 14;
        if (string.IsNullOrWhiteSpace(logPath) || retainedFileCountLimit <= 0)
        {
            throw new InvalidOperationException(
                "FileLogging 設定無效。Path 不可為空，RetainedFileCountLimit 必須大於零。");
        }

        var fullLogPath = Path.GetFullPath(logPath, contentRootPath);
        EnsureLogDirectoryIsWritable(fullLogPath);
        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.File(
                fullLogPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: retainedFileCountLimit,
                shared: true);

        if (includeConsole)
        {
            loggerConfiguration.WriteTo.Console();
        }

        return loggerConfiguration.CreateLogger();
    }

    private static void EnsureLogDirectoryIsWritable(string fullLogPath)
    {
        var directory = Path.GetDirectoryName(fullLogPath)
            ?? throw new InvalidOperationException("FileLogging:Path 必須包含有效的目錄。");
        Directory.CreateDirectory(directory);

        var probePath = Path.Combine(directory, $".opd-log-write-test-{Guid.NewGuid():N}");
        using var probe = new FileStream(
            probePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 1,
            FileOptions.DeleteOnClose);
    }
}
