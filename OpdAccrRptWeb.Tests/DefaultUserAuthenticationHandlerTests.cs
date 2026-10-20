using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpdAccrRptWeb.Infrastructure;

namespace OpdAccrRptWeb.Tests;

public sealed class DefaultUserAuthenticationHandlerTests
{
    [Fact]
    public async Task ServerConfiguredAccountIgnoresWindowsAndRequestValues()
    {
        using ServiceProvider services = BuildServices("F1482");
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Headers["X-User-Id"] = "E100";
        context.Request.QueryString = new QueryString("?userId=E100");

        AuthenticateResult result = await context.AuthenticateAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("F1482", result.Principal!.Identity!.Name);
        Assert.Equal(DefaultUserAuthenticationHandler.SchemeName,
            result.Principal.Identity.AuthenticationType);
    }

    [Fact]
    public async Task MissingServerAccountDoesNotAuthenticate()
    {
        using ServiceProvider services = BuildServices(null);
        var context = new DefaultHttpContext { RequestServices = services };

        AuthenticateResult result = await context.AuthenticateAsync();

        Assert.False(result.Succeeded);
    }

    private static ServiceProvider BuildServices(string? userId)
    {
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DefaultLogin:UserId"] = userId
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddAuthentication(DefaultUserAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, DefaultUserAuthenticationHandler>(
                DefaultUserAuthenticationHandler.SchemeName, _ => { });
        return services.BuildServiceProvider();
    }
}
