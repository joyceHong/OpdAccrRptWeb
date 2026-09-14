namespace OpdAccrRptWeb.Services;

public interface IC21UserIdentityProvider
{
    string? GetCurrentUserId();
    string IdentitySource { get; }
}
