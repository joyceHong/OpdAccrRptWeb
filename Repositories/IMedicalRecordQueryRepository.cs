using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Repositories;

public interface IMedicalRecordQueryRepository
{
    int Count(MedicalRecordQueryFilters filters);

    Task<IReadOnlyList<MedicalRecordSource>> QueryPageAsync(
        MedicalRecordQueryFilters filters,
        int offset,
        int pageSize,
        CancellationToken token);

    Task<MedicalRecordDetailSource?> QueryDetailAsync(
        string medicalRecordNo,
        CancellationToken token);

    Task<IReadOnlyList<string>> QueryMergedMedicalRecordNumbersAsync(
        string medicalRecordNo,
        CancellationToken token);

    Task<decimal?> QueryDebtTotalAsync(
        IReadOnlyCollection<string> medicalRecordNumbers,
        CancellationToken token);
}
