using Microsoft.Extensions.Options;

namespace OpdAccrRptWeb.Services;

public sealed class ConfiguredC23UserIdentityProvider(IOptions<C23Options> options) : IC23UserIdentityProvider
{
    public string IdentitySource => "ConfiguredMockUser";

    public string? GetCurrentUserId() => string.IsNullOrWhiteSpace(options.Value.CurrentUserId)
        ? null
        : options.Value.CurrentUserId.Trim();
}
