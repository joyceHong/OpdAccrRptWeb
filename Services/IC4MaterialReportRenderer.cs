using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC4MaterialReportRenderer
{
    byte[] RenderPdf(C4MaterialPreviewViewModel model);
}
