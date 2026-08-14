using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IReportCatalogService
{
    ReportIndexViewModel GetReportIndex();
}
