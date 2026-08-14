using FemhDb;
using Microsoft.Extensions.Options;

namespace OpdAccrRptWeb.Infrastructure;

public class ConnectionStringProvider : IConnectionStringProvider
{
    private readonly ILogger<ConnectionStringProvider> _logger;
    private readonly DatabaseConnectionOptions _options;

    public ConnectionStringProvider(
        IOptions<DatabaseConnectionOptions> options,
        ILogger<ConnectionStringProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public string GetGuidAp01ConnectionString() => GetConnectionString(_options.GuidAp01);

    public string GetDbTest3ConnectionString() => GetConnectionString(_options.DbTest3);

    private string GetConnectionString(DatabaseEndpointOptions endpoint)
    {
        try
        {
            return DbUser.GetConnectionString(
                endpoint.DatabaseName,
                endpoint.ApplicationName,
                DbUser.ApplicationType.Oracle);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取得資料庫連線字串失敗: {DatabaseName}", endpoint.DatabaseName);
            throw new InvalidOperationException(
                $"無法取得資料庫 [{endpoint.DatabaseName}] 的連線字串。",
                ex);
        }
    }
}
