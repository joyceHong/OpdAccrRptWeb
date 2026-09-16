using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC144XlsxRenderer
{
    byte[] Render(
        IReadOnlyList<C144DebtDetailReportViewModel> rows,
        string worksheetName,
        CancellationToken cancellationToken = default);
}
