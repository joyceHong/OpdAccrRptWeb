namespace OpdAccrRptWeb.Services;

public sealed class DefaultLoginRebuildUserIdentityProvider(IConfiguration configuration)
    : IC21UserIdentityProvider, IC23UserIdentityProvider
{
    public string IdentitySource => "DefaultLogin";

    public string? GetCurrentUserId()
    {
        string? userId = configuration["DefaultLogin:UserId"];
        return string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
    }
}
