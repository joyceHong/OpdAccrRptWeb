using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC141NoticeHistoryRepository
{
    IReadOnlyList<C141NoticeHistoryRow> Query(string startDateRoc,
        string endDateRoc, CancellationToken cancellationToken);
}
