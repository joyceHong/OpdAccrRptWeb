using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IC3ReportRepository
{
    Task<IReadOnlyList<C3MovementRow>> QueryDayAsync(C3ValidatedRequest request, string runDate,
        DepartmentFilterMode departmentMode, CancellationToken cancellationToken = default);
}
