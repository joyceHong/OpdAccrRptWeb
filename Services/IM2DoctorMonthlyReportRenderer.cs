using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IM2DoctorMonthlyReportRenderer
{
    M2RenderedFile RenderXlsx(M2DoctorMonthlyReportSnapshot snapshot, string userName);
}
