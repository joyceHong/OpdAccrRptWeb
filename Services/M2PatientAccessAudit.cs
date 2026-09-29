using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public sealed record M2PatientAccessAudit(
    string Actor,
    string? ReportMonth,
    M2CalculationBasis CalculationBasis,
    M2VisitScope VisitScope,
    M2TimeSlot TimeSlot,
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    int RowCount,
    long NumericChecksum,
    string? RunId,
    string Operation,
    string? Format,
    string Outcome,
    string CorrelationId);

public interface IM2PatientAccessAuditWriter
{
    Task WriteAsync(M2PatientAccessAudit audit, CancellationToken cancellationToken = default);
}

public sealed class M2SerilogPatientAccessAuditWriter(
    ILogger<M2SerilogPatientAccessAuditWriter> logger) : IM2PatientAccessAuditWriter
{
    public Task WriteAsync(M2PatientAccessAudit audit, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "M2 accessed Actor={Actor} ReportMonth={ReportMonth} CalculationBasis={CalculationBasis} VisitScope={VisitScope} TimeSlot={TimeSlot} StartedAt={StartedAt} EndedAt={EndedAt} RowCount={RowCount} NumericChecksum={NumericChecksum} RunId={RunId} Operation={Operation} Format={Format} Outcome={Outcome} CorrelationId={CorrelationId}",
            audit.Actor, audit.ReportMonth, audit.CalculationBasis, audit.VisitScope, audit.TimeSlot,
            audit.StartedAt, audit.EndedAt, audit.RowCount, audit.NumericChecksum, audit.RunId,
            audit.Operation, audit.Format, audit.Outcome, audit.CorrelationId);
        return Task.CompletedTask;
    }
}
