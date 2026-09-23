using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C8ReportService(IC8ReportRepository repository, IC8ReportResultCache cache)
    : IC8ReportService
{
    public async Task<C8ReportResult> QueryAsync(C8ReportRequest request, string actor,
        CancellationToken token = default)
    {
        C8ValidatedRequest validated = request.Validate();
        if (cache.TryGet(actor, validated, out C8CachedResult hit))
            return Build(validated, hit.Rows);

        IReadOnlyList<C8SourceRow> source = await repository.QueryAsync(
            validated.RocStartDate, validated.RocEndDate, token);
        string[] lookupCodes = source
            .Where(row => Emergency(Trim(row.LegacySectionCode), Trim(row.SectionRoomContext)) is null)
            .Select(row => Trim(row.LegacySectionCode))
            .Where(code => code.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        IReadOnlyDictionary<string, string> mappings =
            await repository.GetSectionMappingsAsync(lookupCodes, token);

        var rows = source.Select(row =>
        {
            string legacy = Trim(row.LegacySectionCode);
            string section = Emergency(legacy, Trim(row.SectionRoomContext))
                ?? (mappings.TryGetValue(legacy, out string? mapped) ? mapped.Trim() : string.Empty);
            return new C8ReportRow(Trim(row.VisitDate), Trim(row.MedicalRecordNo), section,
                Trim(row.IdentityCode), Trim(row.ChargeUserId), Trim(row.ChargeCode),
                Trim(row.ChargeName), row.InsuranceUnitPrice ?? 0m,
                row.SelfPayUnitPrice ?? 0m, row.Quantity ?? 0m,
                row.InsuranceAmount ?? 0m, row.SelfPayAmount ?? 0m);
        }).ToList();

        cache.Set(actor, validated, new(validated, rows));
        return Build(validated, rows);
    }

    internal static string? Emergency(string code, string roomContext) => roomContext == "E"
        ? code switch
        {
            "0201" => "11910", "0281" => "11920", "0220" or "0221" => "11930",
            "0230" => "11309", _ => null
        }
        : null;

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;
    private static C8ReportResult Build(C8ValidatedRequest request, IReadOnlyList<C8ReportRow> all)
    {
        int total = all.Count;
        var data = all.Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize).Select(C8PatchBillDetailViewModel.From).ToList();
        return new(request, all, new()
        {
            Columns = ModelDescriptionsHelper.GetPropertyDescriptions<C8PatchBillDetailViewModel>().ToList(),
            Data = data, TotalCount = total, PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize)
        }, "C8_PATCH_BILL_DETAIL");
    }
}
