using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IC7ReportRepository
{
    Task<IReadOnlyList<C7InputUser>> GetEligibleUsersAsync(string rocEndDate, CancellationToken token = default);
    Task<IC7ReportQuerySession> OpenSessionAsync(CancellationToken token = default);
}
public interface IC7ReportQuerySession : IAsyncDisposable
{
    Task<IReadOnlyList<C7SourceRow>> QueryDayAsync(C7ValidatedRequest request, string runDate,
        CancellationToken token = default);
}
