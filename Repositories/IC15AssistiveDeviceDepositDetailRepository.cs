using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC15AssistiveDeviceDepositDetailRepository
{
    IReadOnlyList<C15SourceRow> Query(
        SearchReportCondition condition,
        CancellationToken cancellationToken = default);
}
