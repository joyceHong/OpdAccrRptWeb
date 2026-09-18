using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public interface IOrganizationUnitCodeService
{
    Task<OrganizationUnitMapping?> ResolveLegacyCodeAsync(
        string newCode,
        bool activePlaceOnly,
        CancellationToken cancellationToken = default);

    Task<OrganizationUnitMapping?> ResolveNewCodeAsync(
        string legacyCode,
        string roomType,
        OrganizationUnitMappingScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationUnitMapping>> SearchAsync(
        string query,
        bool includeSections,
        bool includePlaces,
        bool activePlaceOnly,
        int limit = 20,
        CancellationToken cancellationToken = default);
}
