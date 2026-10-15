using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC141PaymentNoticeRepository
{
    DateTimeOffset GetOracleNow(CancellationToken cancellationToken);

    C141PaymentNoticeSource? LoadNoticeSource(
        C141AdmissionKey key,
        CancellationToken cancellationToken);

    C141PartialPaymentData LoadPartialPaymentData(
        C141AdmissionKey key,
        string medicalRecordNumber,
        bool isDayCare,
        CancellationToken cancellationToken);

    decimal GetPeriodInsuranceAmount(
        C141AdmissionKey key,
        DateOnly begin,
        DateOnly end,
        bool isDayCare,
        CancellationToken cancellationToken);

    void RecordBeforeDocument(
        C141PaymentNoticeDocument document,
        string userId,
        CancellationToken cancellationToken);

    void RecordAfterDocument(
        C141AdmissionKey key,
        string entryDateRoc,
        CancellationToken cancellationToken);
}
