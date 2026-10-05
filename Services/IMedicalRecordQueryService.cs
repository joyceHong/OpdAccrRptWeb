using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public interface IMedicalRecordQueryService
{
    Task<MedicalRecordQueryPage> QueryAsync(
        MedicalRecordQueryRequest request,
        CancellationToken token);

    Task<MedicalRecordDetail> QueryDetailAsync(
        MedicalRecordDetailRequest request,
        CancellationToken token);
}
