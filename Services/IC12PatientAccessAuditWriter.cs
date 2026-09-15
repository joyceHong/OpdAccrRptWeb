namespace OpdAccrRptWeb.Services;
public enum C12AuditOutcome { Success, NoData, Denied }
public interface IC12PatientAccessAuditWriter { Task WriteAsync(string userId,C12AuditOutcome outcome,CancellationToken cancellationToken); }
public sealed class C12SerilogPatientAccessAuditWriter(ILogger<C12SerilogPatientAccessAuditWriter> logger) : IC12PatientAccessAuditWriter
{
    public Task WriteAsync(string userId,C12AuditOutcome outcome,CancellationToken ct)
    { ct.ThrowIfCancellationRequested(); logger.LogInformation("C12 patient access audit. Outcome={Outcome}, User={User}",outcome,userId); return Task.CompletedTask; }
}
