using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public interface IOrganizationUnitCodeService
{
    Task<OrganizationUnitMapping?> ResolveLegacyCodeAsync(
        string newCode,
        bool activePlaceOnly,
        CancellationToken cancellationToken = default);

    Task<OrganizationUnitMapping?> ResolveLegacySectionCodeAsync(
        string newCode,
        CancellationToken cancellationToken = default) =>
        ResolveLegacyCodeAsync(newCode, activePlaceOnly: false, cancellationToken: cancellationToken);

    async Task<OrganizationUnitMapping?> ResolveSectionMappingAsync(
        string newCode,
        string legacyCode,
        CancellationToken cancellationToken = default)
    {
        OrganizationUnitMapping? mapping = await ResolveLegacySectionCodeAsync(newCode, cancellationToken);
        return mapping is not null && string.Equals(mapping.LegacyCode, legacyCode.Trim(),
            StringComparison.OrdinalIgnoreCase) ? mapping : null;
    }

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
