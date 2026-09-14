namespace OpdAccrRptWeb.Services;

public interface IC23RebuildAuthorizationService
{
    bool CanRebuild(bool forceRebuild, out string? userId);
}
