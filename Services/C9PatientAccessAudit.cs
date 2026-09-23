namespace OpdAccrRptWeb.Services;

public sealed record C9PatientAccessAudit(string Actor, DateOnly StartDate, DateOnly EndDate,
    DateTimeOffset ExecutedAt, int RowCount, string QueryId, string SummaryRendererStatus,
    string DetailRendererStatus, long DurationMilliseconds, string Outcome, string CorrelationId);
public interface IC9PatientAccessAuditWriter { Task WriteAsync(C9PatientAccessAudit audit, CancellationToken token = default); }
public sealed class C9SerilogPatientAccessAuditWriter(ILogger<C9SerilogPatientAccessAuditWriter> logger) : IC9PatientAccessAuditWriter
{
    public Task WriteAsync(C9PatientAccessAudit a, CancellationToken token = default)
    {
        logger.LogInformation("C9 detail accessed Actor={Actor} Start={Start} End={End} ExecutedAt={ExecutedAt} RowCount={RowCount} QueryId={QueryId} SummaryRenderer={SummaryRenderer} DetailRenderer={DetailRenderer} DurationMs={DurationMs} Outcome={Outcome} CorrelationId={CorrelationId}", a.Actor, a.StartDate, a.EndDate, a.ExecutedAt, a.RowCount, a.QueryId, a.SummaryRendererStatus, a.DetailRendererStatus, a.DurationMilliseconds, a.Outcome, a.CorrelationId);
        return Task.CompletedTask;
    }
}
