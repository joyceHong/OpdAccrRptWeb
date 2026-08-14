using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services
{
    public interface IReportService
    {
        ReportDataAndColumns<T> ReportDataAndColumns<T>(SearchReportCondition searchCondition);
    }
}
