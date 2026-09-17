using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public interface IOrganizationUnitCodeService
{
    Task<OrganizationUnitMapping?> ResolveLegacyCodeAsync(
        string newCode,
        bool activePlaceOnly,
        CancellationToken cancellationToken = default);
}
