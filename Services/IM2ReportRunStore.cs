using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public interface IM2ReportRunStore
{
    string Save(string actor, DateOnly reportMonth, M2CalculationBasis calculationBasis,
        M2VisitScope visitScope, M2TimeSlot timeSlot,
        IReadOnlyList<M2DoctorMonthlyReportRow> rows);
    bool TryGet(string runId, string actor, out M2DoctorMonthlyReportSnapshot snapshot);
}
