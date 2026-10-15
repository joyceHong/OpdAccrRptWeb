using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IOrganizationUnitMappingRepository
{
    Task<IReadOnlyList<OrganizationUnitMapping>> FindByNewCodeAsync(
        string newCode,
        bool activePlaceOnly,
        CancellationToken cancellationToken = default);

    async Task<IReadOnlyList<OrganizationUnitMapping>> FindByNewCodeAsync(
        string newCode,
        OrganizationUnitMappingScope scope,
        bool activePlaceOnly,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<OrganizationUnitMapping> mappings = await FindByNewCodeAsync(
            newCode, activePlaceOnly, cancellationToken);
        return scope == OrganizationUnitMappingScope.SectionOnly
            ? mappings.Where(mapping => mapping.Source == OrganizationUnitSource.Section).ToArray()
            : mappings;
    }

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
