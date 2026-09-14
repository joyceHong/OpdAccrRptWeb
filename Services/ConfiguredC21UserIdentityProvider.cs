using Microsoft.Extensions.Options;

namespace OpdAccrRptWeb.Services;

public sealed class ConfiguredC21UserIdentityProvider(IOptions<C21Options> options) : IC21UserIdentityProvider
{
    public string IdentitySource => "ConfiguredMockUser";

    public string? GetCurrentUserId() => string.IsNullOrWhiteSpace(options.Value.CurrentUserId)
        ? null
        : options.Value.CurrentUserId.Trim();
}
