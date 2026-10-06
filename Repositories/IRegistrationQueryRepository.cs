using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IRegistrationQueryRepository
{
    int Count(RegistrationQueryFilters filters);

    Task<IReadOnlyList<RegistrationSource>> QueryPageAsync(
        RegistrationQueryFilters filters,
        int offset,
        int pageSize,
        CancellationToken token);

    Task<IReadOnlyList<string>> QueryMergedMedicalRecordNumbersAsync(
        string medicalRecordNo,
        CancellationToken token);

    Task<RegistrationSummarySource> QuerySummaryAsync(
        string registrationDate,
        string time,
        string room,
        CancellationToken token);

    Task<IReadOnlyList<RegistrationDoctorOption>> SearchDoctorsAsync(
        string query,
        CancellationToken token);
}
