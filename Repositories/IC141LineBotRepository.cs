using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC141LineBotRepository
{
    DateTimeOffset GetOracleNow(CancellationToken cancellationToken);

    C141LineBotCase? LoadCase(C141AdmissionKey key, CancellationToken cancellationToken);

    void RecordPaymentNoticeSuccess(
        C141LineBotCase item,
        string userId,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken);
}
