using Microsoft.AspNetCore.Mvc;

namespace OpdAccrRptWeb.Services;

public interface IC141RazorViewRenderer
{
    Task<string> RenderToStringAsync(
        ActionContext actionContext,
        string viewPath,
        object model,
        CancellationToken cancellationToken);
}
