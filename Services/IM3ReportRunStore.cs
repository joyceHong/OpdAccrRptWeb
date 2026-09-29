using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public interface IM3ReportRunStore
{
    string Save(string actor, DateOnly reportDate, IReadOnlyList<M3OpdEmergencyDailyReportRow> rows,
        M3NineKpis nineKpis, string? sourceWatermark = null);
    bool TryGet(string runId, string actor, out M3OpdEmergencyDailyReportSnapshot snapshot);
}
