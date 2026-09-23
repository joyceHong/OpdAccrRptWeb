namespace OpdAccrRptWeb.Services;

public sealed record C8PatientAccessAudit(string Actor, DateOnly StartDate, DateOnly EndDate,
    int RowCount, long DurationMilliseconds, string QueryId, string Outcome,
    string CorrelationId);
public interface IC8PatientAccessAuditWriter
{
    Task WriteAsync(C8PatientAccessAudit audit, CancellationToken token = default);
}
public sealed class C8SerilogPatientAccessAuditWriter(ILogger<C8SerilogPatientAccessAuditWriter> logger)
    : IC8PatientAccessAuditWriter
{
    public Task WriteAsync(C8PatientAccessAudit audit, CancellationToken token = default)
    {
        logger.LogInformation(
            "C8 detail accessed Actor={Actor} Start={Start} End={End} RowCount={RowCount} DurationMs={DurationMs} QueryId={QueryId} Outcome={Outcome} CorrelationId={CorrelationId}",
            audit.Actor, audit.StartDate, audit.EndDate, audit.RowCount,
            audit.DurationMilliseconds, audit.QueryId, audit.Outcome, audit.CorrelationId);
        return Task.CompletedTask;
    }
}
