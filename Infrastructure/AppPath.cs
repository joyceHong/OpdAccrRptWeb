using Microsoft.AspNetCore.Http;

namespace OpdAccrRptWeb.Infrastructure;

public static class AppPath
{
    public static string WithPathBase(HttpRequest request, string path)
    {
        return request.PathBase.Add(new PathString(path)).Value ?? path;
    }
}
