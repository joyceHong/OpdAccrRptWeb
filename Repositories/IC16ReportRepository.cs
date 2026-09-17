using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IC16ReportRepository
{
    Task<IReadOnlyList<C16SourceRow>> QueryOutpatientByVisitDateAsync(C16PreviewRequest request, C16QueryPeriod period, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<C16SourceRow>> QueryOutpatientByAccountingDateAsync(C16PreviewRequest request, C16QueryPeriod period, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<C16SourceRow>> QueryInpatientByAccountingDateAsync(C16PreviewRequest request, C16QueryPeriod period, CancellationToken cancellationToken = default);
}
