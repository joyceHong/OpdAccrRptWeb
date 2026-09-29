using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IM2DoctorMonthlyReportRepository
{
    Task<IReadOnlyList<M2DoctorMonthlySourceRow>> QueryStatisticsAsync(
        string rocMonth, M2VisitScope visitScope, M2TimeSlot timeSlot,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<M2DoctorMonthlySourceRow>> QueryActualVisitDayAsync(
        string rocMonth, int day, M2VisitScope visitScope, M2TimeSlot timeSlot,
        CancellationToken cancellationToken = default);
}
