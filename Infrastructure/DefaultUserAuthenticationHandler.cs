using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace OpdAccrRptWeb.Infrastructure;

public sealed class DefaultUserAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "DefaultUser";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        string? userId = configuration["DefaultLogin:UserId"];
        if (string.IsNullOrWhiteSpace(userId))
            return Task.FromResult(AuthenticateResult.Fail("Default login user is not configured."));

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, userId)],
            SchemeName);
        var principal = new ClaimsPrincipal(identity);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, SchemeName)));
    }
}
