using OpdAccrRptWeb.Models;
namespace OpdAccrRptWeb.Services;
public sealed record C7PatientAccessAudit(string Actor,string? InputUserId,DateOnly Start,DateOnly End,string StartTime,string EndTime,C7ChargeKind ChargeKind,int RowCount,long DurationMilliseconds,IReadOnlyList<string> QueryIds,string CorrelationId);
public interface IC7PatientAccessAuditWriter { Task WriteAsync(C7PatientAccessAudit audit,CancellationToken token=default); }
public sealed class C7SerilogPatientAccessAuditWriter(ILogger<C7SerilogPatientAccessAuditWriter> logger):IC7PatientAccessAuditWriter
{public Task WriteAsync(C7PatientAccessAudit a,CancellationToken token=default){logger.LogInformation("C7 detail accessed Actor={Actor} InputUserId={InputUserId} Start={Start} End={End} StartTime={StartTime} EndTime={EndTime} Kind={Kind} RowCount={RowCount} DurationMs={DurationMs} QueryIds={QueryIds} CorrelationId={CorrelationId}",a.Actor,a.InputUserId,a.Start,a.End,a.StartTime,a.EndTime,a.ChargeKind,a.RowCount,a.DurationMilliseconds,string.Join(',',a.QueryIds.Distinct()),a.CorrelationId);return Task.CompletedTask;}}
