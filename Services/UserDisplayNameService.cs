using OpdAccrRptWeb.Repositories;
using Oracle.ManagedDataAccess.Client;

namespace OpdAccrRptWeb.Services;

public sealed class UserDisplayNameService(
    IUserProfileRepository userProfileRepository,
    ILogger<UserDisplayNameService> logger) : IUserDisplayNameService
{
    public async Task<string?> GetDisplayNameAsync(
        string? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId)) return null;

        try
        {
            string? name = await userProfileRepository.GetUserNameAsync(userId, cancellationToken);
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        catch (OracleException exception)
        {
            logger.LogWarning(exception, "無法取得登入者姓名。");
            return null;
        }
    }
}
