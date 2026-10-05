using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IOpdPriceQueryRepository
{
    int CountVisits(string medicalRecordNo, string rocDate, string? newSectionCode, string? fallbackLegacySection);
    Task<IReadOnlyList<OpdPriceVisitSource>> QueryVisitsAsync(string medicalRecordNo,
        string rocDate, string? newSectionCode, string? fallbackLegacySection, int offset, int pageSize, CancellationToken token);
    Task<OpdPriceVisitSource?> QueryVisitAsync(OpdPriceVisitKey key, CancellationToken token);
    Task<IReadOnlyList<OpdPriceChargeSource>> QueryDrugsAsync(OpdPriceVisitKey key,
        bool showDc, CancellationToken token);
    Task<IReadOnlyList<OpdPriceChargeSource>> QueryOrdersAsync(OpdPriceVisitKey key,
        bool showDc, CancellationToken token);
    Task<IReadOnlyList<OpdPriceReceiptSource>> QueryReceiptsAsync(OpdPriceVisitKey key,
        bool showDc, CancellationToken token);
    Task<OpdReceiptHeader?> QueryReceiptHeaderAsync(OpdPriceReceiptKey key, CancellationToken token);
    Task<IReadOnlyList<OpdReceiptChargeAggregate>> QueryReceiptChargesAsync(
        OpdPriceReceiptKey key, CancellationToken token);
    Task<IReadOnlyDictionary<string, string>> QueryChargeNamesAsync(
        IEnumerable<string> codes, CancellationToken token);
}
