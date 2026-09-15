using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

public interface IC12ReportRepository
{
    Task<string?> ResolveOldSectionCodeAsync(string newSectionCode, CancellationToken cancellationToken);
    Task<string?> ResolveMedicalRecordNoAsync(string inputIdentity, CancellationToken cancellationToken);
    Task<IReadOnlyList<C12VisitRow>> QueryVisitsAsync(C12ReportRequest request, string medicalRecordNo, CancellationToken cancellationToken);
    Task<IReadOnlyList<C12ChargeRow>> QueryVisitChargesAsync(C12Source source, C12VisitKey key, CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, string?>> QuerySectionNamesAsync(IEnumerable<string> sectionNos, CancellationToken cancellationToken);
    Task<C12PatientRow?> QueryPatientAsync(string medicalRecordNo, CancellationToken cancellationToken);
}
