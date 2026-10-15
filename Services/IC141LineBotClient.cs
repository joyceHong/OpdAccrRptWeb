using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC141LineBotClient
{
    Task<C141LineBotSendResult> SendAsync(
        string senderId,
        C141LineBotPayload payload,
        CancellationToken cancellationToken);
}
