using FemhDb;
using Microsoft.Extensions.Options;

namespace OpdAccrRptWeb.Infrastructure;

public class ConnectionStringProvider : IConnectionStringProvider
{
    private readonly ILogger<ConnectionStringProvider> _logger;
    private readonly DatabaseConnectionOptions _options;
    private readonly Func<DatabaseEndpointOptions, string> _resolveConnectionString;

    public ConnectionStringProvider(
        IOptions<DatabaseConnectionOptions> options,
        ILogger<ConnectionStringProvider> logger)
        : this(options, logger, ResolveWithFemhDb)
    {
    }

    internal ConnectionStringProvider(
        IOptions<DatabaseConnectionOptions> options,
        ILogger<ConnectionStringProvider> logger,
        Func<DatabaseEndpointOptions, string> resolveConnectionString)
    {
        _options = options.Value;
        _logger = logger;
        _resolveConnectionString = resolveConnectionString;
    }

    public string GetConnectionString()
    {
        DatabaseEndpointOptions endpoint = _options.GetSelectedEndpoint();

        try
        {
            return _resolveConnectionString(endpoint);
        }
        catch (Exception)
        {
            _logger.LogError("取得資料庫連線字串失敗: {DatabaseName}", endpoint.DatabaseName);
            throw new InvalidOperationException(
                $"無法取得資料庫 [{endpoint.DatabaseName}] 的連線字串。");
        }
    }

    private static string ResolveWithFemhDb(DatabaseEndpointOptions endpoint) =>
        DbUser.GetConnectionString(
            endpoint.DatabaseName,
            endpoint.ApplicationName,
            DbUser.ApplicationType.Oracle);
}
