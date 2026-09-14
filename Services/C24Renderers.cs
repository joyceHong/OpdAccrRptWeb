using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public enum C24OutputFormat { Html, Json, Pdf, Xlsx }

public sealed record C24RenderPayload(
    C24OutputFormat Format,
    IReadOnlyList<C24DebtPaymentDetail> Details,
    IReadOnlyList<C24Summary> Summaries);

public interface IC24CanonicalResultRenderer
{
    C24OutputFormat Format { get; }
    C24RenderPayload Render(C24CanonicalResult result);
}

public abstract class C24CanonicalResultRenderer(C24OutputFormat format) : IC24CanonicalResultRenderer
{
    public C24OutputFormat Format { get; } = format;
    public C24RenderPayload Render(C24CanonicalResult result) => new(Format, result.Details, result.Summaries);
}

public sealed class C24HtmlRenderer() : C24CanonicalResultRenderer(C24OutputFormat.Html);
public sealed class C24JsonRenderer() : C24CanonicalResultRenderer(C24OutputFormat.Json);
public sealed class C24PdfRenderer() : C24CanonicalResultRenderer(C24OutputFormat.Pdf);
public sealed class C24XlsxRenderer() : C24CanonicalResultRenderer(C24OutputFormat.Xlsx);
