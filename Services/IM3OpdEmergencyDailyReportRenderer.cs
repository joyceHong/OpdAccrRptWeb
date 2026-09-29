using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IM3OpdEmergencyDailyReportRenderer
{
    M3RenderedFile RenderXlsx(M3OpdEmergencyDailyReportSnapshot snapshot, string userName);
}
