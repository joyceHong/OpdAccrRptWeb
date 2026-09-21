using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IC5ReportRepository
{
    Task<IC5ReportQuerySession> OpenSessionAsync(CancellationToken cancellationToken = default);
}

public interface IC5ReportQuerySession : IAsyncDisposable
{
    Task<IReadOnlyList<C5SourceRow>> QueryDayAsync(C5ValidatedRequest request, string runDate,
        C5QueryId queryId, CancellationToken cancellationToken = default);
}
