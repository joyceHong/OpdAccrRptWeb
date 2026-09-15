using OpdAccrRptWeb.ViewModels;
namespace OpdAccrRptWeb.Services;
public interface IC12ReportService
{
    Task<C12MedicalReceiptSummaryViewModel> CreateAsync(C12ReportRequest request,string userId,CancellationToken cancellationToken=default);
}
