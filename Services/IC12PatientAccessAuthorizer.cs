namespace OpdAccrRptWeb.Services;
public interface IC12PatientAccessAuthorizer { Task<bool> AuthorizeAsync(string userId, CancellationToken cancellationToken); }
public sealed class AllowConfiguredC12PatientAccessAuthorizer : IC12PatientAccessAuthorizer
{ public Task<bool> AuthorizeAsync(string userId,CancellationToken ct)=>Task.FromResult(true); }
public sealed class C12AccessDeniedException() : UnauthorizedAccessException("沒有權限查詢 C12 病歷資料。");
