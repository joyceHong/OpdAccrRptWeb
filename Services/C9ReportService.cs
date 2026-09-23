using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C9ReportService : IC9ReportService
{
    private readonly IC9ReportRepository _repository;
    private readonly IC9ReportResultCache _cache;
    private readonly IC9TransientFailurePolicy _failurePolicy;

    public C9ReportService(IC9ReportRepository repository, IC9ReportResultCache cache,
        IC9TransientFailurePolicy? failurePolicy = null)
    {
        _repository = repository;
        _cache = cache;
        _failurePolicy = failurePolicy ?? new C9OracleFailurePolicy();
    }

    public async Task<C9ReportResult> QueryAsync(C9ReportRequest request, string actor,
        CancellationToken token = default)
    {
        C9ValidatedRequest validated = request.Validate();
        if (_cache.TryGet(actor, validated, out C9CachedResult hit))
            return Build(validated, hit.Rows);

        var rows = new List<C9ReportRow>();
        for (DateOnly date = validated.StartDate; date <= validated.EndDate; date = date.AddDays(1))
        {
            token.ThrowIfCancellationRequested();
            IReadOnlyList<C9SourceRow> daily = await QueryDayWithRetryAsync(
                C9ReportRequest.ToRoc(date), token);
            rows.AddRange(daily.Select(Map));
        }

        IReadOnlyList<C9ReportRow> immutable = rows.AsReadOnly();
        _cache.Set(actor, validated, new(validated, immutable));
        return Build(validated, immutable);
    }

    private async Task<IReadOnlyList<C9SourceRow>> QueryDayWithRetryAsync(string rocDate,
        CancellationToken token)
    {
        for (int attempt = 1; ; attempt++)
        {
            try { return await _repository.QueryDayAsync(rocDate, token); }
            catch (Exception exception) when (!token.IsCancellationRequested
                && attempt < _failurePolicy.MaxAttempts && _failurePolicy.IsTransient(exception))
            {
                await Task.Delay(_failurePolicy.RetryDelay, token);
            }
        }
    }

    internal static C9ReportRow Map(C9SourceRow source)
    {
        decimal discount = source.DiscountAmountSource ?? 0m;
        decimal payable = source.PayableAmountSource ?? 0m;
        return new(Trim(source.VisitDate), Trim(source.MedicalRecordNo),
            Trim(source.PatientName), Trim(source.DoctorName), Trim(source.SectionName),
            Trim(source.ChargeUserId), Trim(source.ChargeCode), Trim(source.ChargeName),
            discount, payable, discount + payable);
    }

    internal static IReadOnlyList<C9ReportGroup> Group(IReadOnlyList<C9ReportRow> rows) => rows
        .OrderBy(row => row.ChargeCode, StringComparer.Ordinal)
        .GroupBy(row => row.ChargeCode, StringComparer.Ordinal)
        .Select(group => new C9ReportGroup(group.Key, group.ToList().AsReadOnly(),
            group.Sum(row => row.TotalAmount), group.Sum(row => row.DiscountAmount),
            group.Sum(row => row.PayableAmount)))
        .ToList().AsReadOnly();

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;

    private static C9ReportResult Build(C9ValidatedRequest request,
        IReadOnlyList<C9ReportRow> all)
    {
        int total = all.Count;
        var data = all.Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize).Select(C9MaterialAccountingMonthlyViewModel.From).ToList();
        return new(request, all, new()
        {
            Columns = ModelDescriptionsHelper
                .GetPropertyDescriptions<C9MaterialAccountingMonthlyViewModel>().ToList(),
            Data = data,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalPages = total == 0 ? 0 : (int)Math.Ceiling(total / (double)request.PageSize)
        }, C9Sql.ResourceId);
    }
}
