using OpdAccrRptWeb.Models;
namespace OpdAccrRptWeb.Services;
public interface IOpdPriceReceiptRenderer { Task<OpdReceiptPreview> RenderAsync(OpdPriceReceiptKey key,CancellationToken token); }
