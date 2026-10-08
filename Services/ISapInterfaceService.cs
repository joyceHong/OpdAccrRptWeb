using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public interface ISapInterfaceService
{
    Task<string> GetDefaultDateAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<SapInterfaceEventStatus>> GetStatusAsync(string businessDate, CancellationToken cancellationToken);
    Task<SapInterfaceRunResult> RunAsync(SapInterfaceRequest request, CancellationToken cancellationToken);
}
