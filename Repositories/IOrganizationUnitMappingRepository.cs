using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IOrganizationUnitMappingRepository
{
    Task<IReadOnlyList<OrganizationUnitMapping>> FindByNewCodeAsync(
        string newCode,
        bool activePlaceOnly,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationUnitMapping>> FindByLegacyCodeAsync(
        string legacyCode,
        OrganizationUnitMappingScope scope,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationUnitMapping>> SearchAsync(
        string query,
        bool includeSections,
        bool includePlaces,
        bool activePlaceOnly,
        int limit,
        CancellationToken cancellationToken = default);
}
