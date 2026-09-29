namespace OpdAccrRptWeb.Services;

public sealed record M3PatientAccessAudit(string Actor, DateOnly? ReportDate,
    DateTimeOffset StartedAt, DateTimeOffset EndedAt, int RowCount, long NumericChecksum,
    string? SourceWatermark, string? RunId, string Operation, string? Format, string Outcome,
    string? FailureStage, string CorrelationId);

public interface IM3PatientAccessAuditWriter
{
    Task WriteAsync(M3PatientAccessAudit audit, CancellationToken cancellationToken = default);
}

public sealed class M3SerilogPatientAccessAuditWriter(ILogger<M3SerilogPatientAccessAuditWriter> logger)
    : IM3PatientAccessAuditWriter
{
    public Task WriteAsync(M3PatientAccessAudit audit, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("M3 accessed Actor={Actor} ReportDate={ReportDate} StartedAt={StartedAt} EndedAt={EndedAt} RowCount={RowCount} NumericChecksum={NumericChecksum} SourceWatermark={SourceWatermark} RunId={RunId} Operation={Operation} Format={Format} Outcome={Outcome} FailureStage={FailureStage} CorrelationId={CorrelationId}",
            audit.Actor, audit.ReportDate, audit.StartedAt, audit.EndedAt, audit.RowCount,
            audit.NumericChecksum, audit.SourceWatermark, audit.RunId, audit.Operation,
            audit.Format, audit.Outcome, audit.FailureStage, audit.CorrelationId);
        return Task.CompletedTask;
    }
}
