namespace OpdAccrRptWeb.Services;

public sealed class C23RebuildAuthorizationService(
    IC23UserIdentityProvider identityProvider) : IC23RebuildAuthorizationService
{
    public bool CanRebuild(bool forceRebuild, out string? userId)
    {
        userId = identityProvider.GetCurrentUserId();
        return userId is not null;
    }
}
