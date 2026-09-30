using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public interface IOpdPriceQueryService
{
    Task<OpdPriceVisitPage> QueryVisitsAsync(OpdPriceVisitRequest request,string actor,CancellationToken token);
    Task<OpdPriceDetail> QueryDetailAsync(OpdPriceDetailRequest request,string actor,CancellationToken token);
    Task<IReadOnlyList<OpdPriceSectionOption>> SearchSectionsAsync(string query,CancellationToken token);
    Task<OpdReceiptPreview> CreateReceiptAsync(string receiptToken,string actor,CancellationToken token);
}

public interface IOpdPriceTokenService
{
    string ProtectVisit(OpdPriceVisitKey key,string actor);
    string ProtectReceipt(OpdPriceReceiptKey key,string actor);
    bool TryReadVisit(string token,string actor,out OpdPriceVisitKey key);
    bool TryReadReceipt(string token,string actor,out OpdPriceReceiptKey key);
}
