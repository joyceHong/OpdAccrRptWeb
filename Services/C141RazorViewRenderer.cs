using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace OpdAccrRptWeb.Services;

public sealed class C141RazorViewRenderer(
    ICompositeViewEngine viewEngine,
    ITempDataProvider tempDataProvider,
    IModelMetadataProvider metadataProvider) : IC141RazorViewRenderer
{
    public async Task<string> RenderToStringAsync(
        ActionContext actionContext,
        string viewPath,
        object model,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actionContext);
        ArgumentException.ThrowIfNullOrWhiteSpace(viewPath);
        ArgumentNullException.ThrowIfNull(model);
        cancellationToken.ThrowIfCancellationRequested();

        ViewEngineResult result = viewEngine.GetView(null, viewPath, isMainPage: true);
        if (!result.Success)
            result = viewEngine.FindView(actionContext, viewPath, isMainPage: true);
        if (!result.Success || result.View is null)
            throw new InvalidOperationException($"找不到 C141 預覽樣板：{viewPath}");

        var viewData = new ViewDataDictionary(metadataProvider, new ModelStateDictionary())
        {
            Model = model
        };
        var tempData = new TempDataDictionary(actionContext.HttpContext, tempDataProvider);
        using var writer = new StringWriter();
        var viewContext = new ViewContext(
            actionContext,
            result.View,
            viewData,
            tempData,
            writer,
            new HtmlHelperOptions());
        await result.View.RenderAsync(viewContext);
        cancellationToken.ThrowIfCancellationRequested();
        return writer.ToString();
    }
}
