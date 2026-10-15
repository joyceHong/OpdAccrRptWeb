using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public interface IC141PaymentNoticeService
{
    DateTimeOffset GetOracleNow(CancellationToken cancellationToken = default);

    C141PaymentNoticeDocument? Calculate(
        C141AdmissionKey key,
        CancellationToken cancellationToken = default);

    void RecordBeforeDocument(
        C141PaymentNoticeDocument document,
        string userId,
        CancellationToken cancellationToken = default);

    void RecordAfterDocument(
        C141AdmissionKey key,
        string entryDateRoc,
        CancellationToken cancellationToken = default);
}
