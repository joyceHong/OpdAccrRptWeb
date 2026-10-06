using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public interface IRegistrationQueryService
{
    Task<RegistrationQueryResult> QueryAsync(
        RegistrationQueryRequest request,
        CancellationToken token);

    Task<IReadOnlyList<RegistrationSectionOption>> SearchSectionsAsync(
        string query,
        CancellationToken token);

    Task<IReadOnlyList<RegistrationDoctorOption>> SearchDoctorsAsync(
        string query,
        CancellationToken token);
}
