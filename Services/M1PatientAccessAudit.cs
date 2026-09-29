namespace OpdAccrRptWeb.Services;

public sealed record M1PatientAccessAudit(
    string Actor,
    DateOnly? ReportDate,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    int RowCount,
    string? RunId,
    string Operation,
    string? Format,
    string Outcome,
    string CorrelationId);

public interface IM1PatientAccessAuditWriter
{
    Task WriteAsync(M1PatientAccessAudit audit, CancellationToken cancellationToken = default);
}

public sealed class M1SerilogPatientAccessAuditWriter(
    ILogger<M1SerilogPatientAccessAuditWriter> logger) : IM1PatientAccessAuditWriter
{
    public Task WriteAsync(M1PatientAccessAudit audit, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "M1 accessed Actor={Actor} ReportDate={ReportDate} StartedAt={StartedAt} EndedAt={EndedAt} RowCount={RowCount} RunId={RunId} Operation={Operation} Format={Format} Outcome={Outcome} CorrelationId={CorrelationId}",
            audit.Actor, audit.ReportDate, audit.StartedAt, audit.EndedAt, audit.RowCount,
            audit.RunId, audit.Operation, audit.Format, audit.Outcome, audit.CorrelationId);
        return Task.CompletedTask;
    }
}

