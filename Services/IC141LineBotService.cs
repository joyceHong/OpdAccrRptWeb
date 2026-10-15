using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC141LineBotService
{
    Task<C141LineBotBatchResult> SendAsync(
        C141LineBotMessageKind kind,
        IReadOnlyList<C141AdmissionKey> cases,
        string userId,
        CancellationToken cancellationToken = default);
}
