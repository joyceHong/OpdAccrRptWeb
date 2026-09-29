using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IM1DoctorDailyReportRepository
{
    Task<IReadOnlyList<M1DoctorDailyAggregateRow>> QueryAsync(
        string rocDate,
        CancellationToken cancellationToken = default);
}

