using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed record C5PatientAccessAudit(string UserId, DateTimeOffset OccurredAt, C5DataSource DataSource,
    DateOnly Start, DateOnly End, string? SectionCode, string? RoomNo, string? ChargeCode,
    string? InsuranceIdentityCode, int RowCount, long DurationMilliseconds, IReadOnlyList<C5QueryId> QueryIds);
public interface IC5PatientAccessAuditWriter { Task WriteAsync(C5PatientAccessAudit audit, CancellationToken cancellationToken = default); }
public sealed class C5SerilogPatientAccessAuditWriter(ILogger<C5SerilogPatientAccessAuditWriter> logger) : IC5PatientAccessAuditWriter
{
    public Task WriteAsync(C5PatientAccessAudit a, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("C5 patient detail accessed. UserId={UserId} OccurredAt={OccurredAt} Source={Source} Start={Start} End={End} Section={Section} Room={Room} Charge={Charge} Identity={Identity} RowCount={RowCount} DurationMs={DurationMs} QueryIds={QueryIds}", a.UserId,a.OccurredAt,a.DataSource,a.Start,a.End,a.SectionCode,a.RoomNo,a.ChargeCode,a.InsuranceIdentityCode,a.RowCount,a.DurationMilliseconds,string.Join(',',a.QueryIds.Distinct()));
        return Task.CompletedTask;
    }
}
