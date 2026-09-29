using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public interface IM1ReportRunStore
{
    string Save(string actor, DateOnly reportDate, IReadOnlyList<M1DoctorDailyReportRow> rows);
    bool TryGet(string runId, string actor, out M1DoctorDailyReportSnapshot snapshot);
}

