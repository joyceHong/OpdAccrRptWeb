namespace OpdAccrRptWeb.Services;

public interface IC23UserIdentityProvider
{
    string IdentitySource { get; }
    string? GetCurrentUserId();
}
