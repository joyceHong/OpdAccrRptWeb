namespace OpdAccrRptWeb.Services;
public sealed record OpdPriceAuditEvent(string Actor,string Operation,string Outcome,int RowCount,long ElapsedMilliseconds,string TraceId,string ConditionFingerprint);
public interface IOpdPricePatientAccessAuditWriter { Task WriteAsync(OpdPriceAuditEvent value,CancellationToken token); }
public sealed class OpdPriceSerilogAuditWriter(ILogger<OpdPriceSerilogAuditWriter> logger) : IOpdPricePatientAccessAuditWriter
{
    public Task WriteAsync(OpdPriceAuditEvent value,CancellationToken token)
    { token.ThrowIfCancellationRequested();logger.LogInformation("Q1 access audit. Actor={Actor} Operation={Operation} Outcome={Outcome} Rows={Rows} ElapsedMs={ElapsedMs} TraceId={TraceId} Fingerprint={Fingerprint}",value.Actor,value.Operation,value.Outcome,value.RowCount,value.ElapsedMilliseconds,value.TraceId,value.ConditionFingerprint);return Task.CompletedTask; }
}
