using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IOrganizationUnitMappingRepository
{
    Task<IReadOnlyList<OrganizationUnitMapping>> FindByNewCodeAsync(
        string newCode,
        bool activePlaceOnly,
        CancellationToken cancellationToken = default);
}
