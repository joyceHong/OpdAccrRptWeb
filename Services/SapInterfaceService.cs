using System.Globalization;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed class SapInterfaceService(
    ISapInterfaceRepository repository,
    ILogger<SapInterfaceService> logger) : ISapInterfaceService
{
    private static readonly (string Code, string Name)[] Events =
    [
        ("SAPCASH", "櫃員現金"),
        ("SAPCONS", "合約記帳"),
        ("SAPACC", "批價收入"),
        ("SAPREV2", "轉撥收入")
    ];

    public async Task<string> GetDefaultDateAsync(CancellationToken cancellationToken)
    {
        string? rocDate = await repository.GetDefaultRocDateAsync(cancellationToken);
        if (TryParseRocDate(rocDate, out DateOnly date))
            return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return DateOnly.FromDateTime(DateTime.Today.AddDays(-1))
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<SapInterfaceEventStatus>> GetStatusAsync(string businessDate,
        CancellationToken cancellationToken)
    {
        string rocDate = ToRocDate(ParseBusinessDate(businessDate));
        IReadOnlyDictionary<string, bool> completed = await repository.GetCompletedAsync(rocDate, cancellationToken);
        return Events.Select(item => new SapInterfaceEventStatus(item.Code, item.Name,
            completed.GetValueOrDefault(item.Code))).ToArray();
    }

    public async Task<SapInterfaceRunResult> RunAsync(SapInterfaceRequest request, CancellationToken cancellationToken)
    {
        DateOnly date = ParseBusinessDate(request.BusinessDate);
        string rocDate = ToRocDate(date);
        string gregorianDate = date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        HashSet<string> confirmed = (request.ConfirmRerun ?? []).ToHashSet(StringComparer.Ordinal);
        var selected = Events.Where(item => item.Code switch
        {
            "SAPCASH" => request.RunCash,
            "SAPCONS" => request.RunContract,
            "SAPACC" => request.RunAcc,
            "SAPREV2" => request.RunRev,
            _ => false
        }).ToArray();
        IReadOnlyDictionary<string, bool> completed = await repository.GetCompletedAsync(rocDate, cancellationToken);
        var confirmationRequired = selected
            .Where(item => completed.GetValueOrDefault(item.Code) && !confirmed.Contains(item.Code))
            .Select(item => new SapInterfaceEventStatus(item.Code, item.Name, true)).ToArray();
        if (confirmationRequired.Length > 0)
            return new SapInterfaceRunResult(request.BusinessDate, [], confirmationRequired);

        var results = new List<SapInterfaceEventResult>();
        foreach (var item in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                SapInterfaceRepositoryRunResult run = await repository.RunEventAsync(item.Code, rocDate, gregorianDate,
                    confirmed.Contains(item.Code), cancellationToken);
                string status = run.Status switch
                {
                    SapInterfaceRepositoryRunStatus.Completed => "completed",
                    SapInterfaceRepositoryRunStatus.NoData => "noData",
                    SapInterfaceRepositoryRunStatus.ConfirmationRequired => "confirmationRequired",
                    _ => throw new ArgumentOutOfRangeException(nameof(run.Status), run.Status, null)
                };
                if (status == "confirmationRequired")
                {
                    results.Add(new SapInterfaceEventResult(item.Code, item.Name, status));
                    return new SapInterfaceRunResult(request.BusinessDate, results,
                        [new SapInterfaceEventStatus(item.Code, item.Name, true)]);
                }
                results.Add(new SapInterfaceEventResult(item.Code, item.Name, status));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "SAP event failed. Event={Event}, BusinessDate={BusinessDate}",
                    item.Code, request.BusinessDate);
                results.Add(new SapInterfaceEventResult(item.Code, item.Name, "failed", "執行失敗，該項資料已回復。"));
            }
        }
        return new SapInterfaceRunResult(request.BusinessDate, results, []);
    }

    private static DateOnly ParseBusinessDate(string? businessDate)
    {
        if (!DateOnly.TryParseExact(businessDate, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out DateOnly date) || date.Year is < 1912 or > 2910)
            throw new ArgumentException("請輸入有效的西元日期（1912 至 2910 年）。", nameof(businessDate));
        return date;
    }

    private static string ToRocDate(DateOnly date) =>
        (date.Year - 1911).ToString("D3", CultureInfo.InvariantCulture)
        + date.ToString("MMdd", CultureInfo.InvariantCulture);

    private static bool TryParseRocDate(string? value, out DateOnly date)
    {
        date = default;
        if (value is not { Length: 7 } || !int.TryParse(value[..3], out int rocYear)
            || !int.TryParse(value.AsSpan(3, 2), out int month)
            || !int.TryParse(value.AsSpan(5, 2), out int day)
            || rocYear is < 1 or > 999)
            return false;
        return DateOnly.TryParse($"{rocYear + 1911:D4}-{month:D2}-{day:D2}",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}
