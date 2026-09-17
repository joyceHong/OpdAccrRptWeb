using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C16ReportService(IC16ReportRepository repository, IC16LegacyReducer reducer, IReportTotalCountCache totalCountCache) : IC16ReportService
{
    public async Task<C16ReportResult> QueryAsync(C16PreviewRequest request, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        C16QueryPeriod period = request.ValidateAndCreatePeriod();
        IReadOnlyList<C16SourceRow> sourceRows = request.Source switch
        {
            C16Source.Inpatient => await repository.QueryInpatientByAccountingDateAsync(request, period, cancellationToken),
            _ when request.EffectiveDateBasis == C16DateBasis.VisitDate => await repository.QueryOutpatientByVisitDateAsync(request, period, cancellationToken),
            _ => await repository.QueryOutpatientByAccountingDateAsync(request, period, cancellationToken)
        };
        IReadOnlyList<C16ReportRow> rows = reducer.Reduce(sourceRows, request.Source);
        cancellationToken.ThrowIfCancellationRequested();
        int total = totalCountCache.GetOrCreate("C16", new Dictionary<string, string?>
        {
            ["StartDate"] = period.StartDate, ["EndDate"] = period.EndDate,
            ["Source"] = request.Source.ToString(), ["ReportType"] = request.ReportType.ToString(),
            ["DateBasis"] = request.EffectiveDateBasis.ToString()
        }, () => rows.Count);
        var pageRows = rows.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize)
            .Select(row => ToViewModel(row, request.Source)).ToList();
        var page = new ReportDataAndColumns<C16MedicalSubsidyReportViewModel>
        {
            Columns = ModelDescriptionsHelper.GetPropertyDescriptions<C16MedicalSubsidyReportViewModel>()
                .Where(column => IsVisibleColumn(column.Key, request.Source)).ToList(),
            Data = pageRows, TotalCount = total, PageNumber = request.PageNumber, PageSize = request.PageSize,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize)
        };
        return new C16ReportResult(request, rows, page);
    }

    private static bool IsVisibleColumn(string key, C16Source source) => source == C16Source.Inpatient
        ? key is not "encounterOrdinal" and not "sectionName" and not "rl25" and not "rl49Drug"
        : key is not "encounterOrdinal" and not "dischargeDate" and not "days";

    internal static C16MedicalSubsidyReportViewModel ToViewModel(C16ReportRow row, C16Source source) => new()
    {
        PatientName=row.PatientName,PatientId=row.PatientId,BirthDate=row.BirthDate,VisitDate=row.VisitDate,
        DischargeDate=row.DischargeDate,Days=row.Days,SectionName=row.SectionName,Diagnosis=row.Diagnosis,
        SubsidyType=row.SubsidyType,Rl25=row.Rl25,Rl49=row.Rl49,Rl49Drug=row.Rl49Drug,Rl50=row.Rl50,
        Total=source==C16Source.Inpatient?row.InpatientTotal:row.OutpatientTotal,EncounterOrdinal=row.EncounterOrdinal
    };
}
