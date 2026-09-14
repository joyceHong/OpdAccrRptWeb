using Microsoft.Extensions.Options;

namespace OpdAccrRptWeb.Services;

public sealed class C21RebuildAuthorizationService(
    IC21UserIdentityProvider identityProvider,
    IOptions<C21Options> options)
    : IC21RebuildAuthorizationService
{
    public bool CanRebuild(bool forceRebuild, out string? userId)
    {
        userId = identityProvider.GetCurrentUserId();
        return userId is not null && (!forceRebuild || options.Value.RebuildEnabled);
    }
}
