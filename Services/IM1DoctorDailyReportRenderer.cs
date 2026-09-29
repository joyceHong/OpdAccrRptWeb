using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IM1DoctorDailyReportRenderer
{
    M1RenderedFile RenderXlsx(M1DoctorDailyReportSnapshot snapshot, string userName);
}
