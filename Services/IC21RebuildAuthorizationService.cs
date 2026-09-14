namespace OpdAccrRptWeb.Services;

public interface IC21RebuildAuthorizationService
{
    bool CanRebuild(bool forceRebuild, out string? userId);
}
