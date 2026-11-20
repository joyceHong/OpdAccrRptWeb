using Microsoft.AspNetCore.Http;
using OpdAccrRptWeb.Infrastructure;

namespace OpdAccrRptWeb.Tests;

public sealed class AppPathTests
{
    [Theory]
    [InlineData("", "/Report/C1")]
    [InlineData("/OpdAccrRpt", "/OpdAccrRpt/Report/C1")]
    [InlineData("/femhF1482/OpdAccrRpt", "/femhF1482/OpdAccrRpt/Report/C1")]
    public void WithPathBase_AddsDeploymentPath(string basePath, string expected)
    {
        var context = new DefaultHttpContext();
        context.Request.PathBase = new PathString(basePath);

        Assert.Equal(expected, AppPath.WithPathBase(context.Request, "/Report/C1"));
    }
}
