using Microsoft.Extensions.Logging;
using OpdAccrRptWeb.Help;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

internal sealed class FakeHealthCenterRepository : IHealthCenterRepository
{
    public int C171CountCalls { get; private set; }

    public int C171PageCalls { get; private set; }

    public int TotalCount { get; set; }

    public List<object> C171Data { get; set; } = [];

    public Exception? C171CountException { get; set; }

    public Exception? C171PageException { get; set; }

    public int C174CountCalls { get; private set; }

    public int C174PageCalls { get; private set; }

    public int C174TotalCount { get; set; }

    public List<object> C174Data { get; set; } = [];

    public Exception? C174CountException { get; set; }

    public Exception? C174PageException { get; set; }

    public List<HealthCenterContractBillingReport> C174ExportData { get; set; } = [];

    public Exception? C174BatchException { get; set; }

    public List<(int Offset, int BatchSize)> C174BatchCalls { get; } = [];

    public int GetHealthCenterDataCount(SearchReportCondition searchCondition)
    {
        C171CountCalls++;
        if (C171CountException is not null)
        {
            throw C171CountException;
        }

        return TotalCount;
    }

    public List<T> GetHealthCenterDataPage<T>(SearchReportCondition searchCondition)
    {
        C171PageCalls++;
        if (C171PageException is not null)
        {
            throw C171PageException;
        }

        return C171Data.Cast<T>().ToList();
    }

    public List<ModelDescriptionsHelper.PropertyMetadata> GetHelthCenterDetailColumns() => [];

    public List<ModelDescriptionsHelper.PropertyMetadata> GetHelthCenterCountColumns() => [];

    public List<T> GetHealthCenterCountData<T>(SearchReportCondition searchCondition) => [];

    public List<ModelDescriptionsHelper.PropertyMetadata> GetHealthCheckupVisitsColumns() => [];

    public List<T> GetHealthCheckupVisitsData<T>(SearchReportCondition searchCondition) => [];

    public List<ModelDescriptionsHelper.PropertyMetadata> GetHealthCenterContractBillingReportColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<HealthCenterContractBillingReport>();

    public int GetHealthCenterContractBillingReportCount(SearchReportCondition searchCondition)
    {
        C174CountCalls++;
        if (C174CountException is not null)
        {
            throw C174CountException;
        }

        return C174TotalCount;
    }

    public List<T> GetHealthCenterContractBillingReportPage<T>(SearchReportCondition searchCondition)
    {
        C174PageCalls++;
        if (C174PageException is not null)
        {
            throw C174PageException;
        }

        return C174Data.Cast<T>().ToList();
    }

    public List<HealthCenterContractBillingReport> GetHealthCenterContractBillingReportBatch(
        SearchReportCondition searchCondition,
        int offset,
        int batchSize)
    {
        C174BatchCalls.Add((offset, batchSize));
        if (C174BatchException is not null)
        {
            throw C174BatchException;
        }

        return C174ExportData.Skip(offset).Take(batchSize).ToList();
    }
}

internal sealed class FakeReferralMemberRepository : IReferralMemberRepository
{
    public int CountCalls { get; private set; }

    public int PageCalls { get; private set; }

    public int TotalCount { get; set; }

    public int PageRowCount { get; set; }

    public string? LastCountStartDate { get; private set; }

    public string? LastCountEndDate { get; private set; }

    public string? LastCountSource { get; private set; }

    public string? LastPageSource { get; private set; }

    public int? LastPageNumber { get; private set; }

    public int? LastPageSize { get; private set; }

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<ReferralMemberReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        CountCalls++;
        LastCountStartDate = searchCondition.StartDate;
        LastCountEndDate = searchCondition.EndDate;
        LastCountSource = searchCondition.EncounterSource;
        return TotalCount;
    }

    public List<ReferralMemberReportViewModel> GetPage(SearchReportCondition searchCondition)
    {
        PageCalls++;
        LastPageSource = searchCondition.EncounterSource;
        LastPageNumber = searchCondition.PageNumber;
        LastPageSize = searchCondition.PageSize;
        return Enumerable.Range(0, PageRowCount)
            .Select(_ => new ReferralMemberReportViewModel())
            .ToList();
    }
}

internal sealed class FakeSafeNeedleRepository : ISafeNeedleRepository
{
    public int CountCalls { get; private set; }

    public int PageCalls { get; private set; }

    public int TotalCount { get; set; }

    public Exception? CountException { get; set; }

    public List<SafeNeedleReportViewModel> Data { get; set; } = [];

    public string? LastStartDate { get; private set; }

    public string? LastEndDate { get; private set; }

    public string? LastSource { get; private set; }

    public string? LastPrefix { get; private set; }

    public int? LastPageNumber { get; private set; }

    public int? LastPageSize { get; private set; }

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<SafeNeedleReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        CountCalls++;
        if (CountException is not null)
        {
            throw CountException;
        }

        LastStartDate = searchCondition.StartDate;
        LastEndDate = searchCondition.EndDate;
        LastSource = searchCondition.EncounterSource;
        LastPrefix = searchCondition.StationOrBedPrefix;
        return TotalCount;
    }

    public List<SafeNeedleReportViewModel> GetPage(SearchReportCondition searchCondition)
    {
        PageCalls++;
        LastStartDate = searchCondition.StartDate;
        LastEndDate = searchCondition.EndDate;
        LastSource = searchCondition.EncounterSource;
        LastPrefix = searchCondition.StationOrBedPrefix;
        LastPageNumber = searchCondition.PageNumber;
        LastPageSize = searchCondition.PageSize;
        return Data;
    }
}

internal sealed class FakeContractPaymentDetailRepository : IContractPaymentDetailRepository
{
    public int CountCalls { get; private set; }
    public int PageCalls { get; private set; }
    public int TotalCount { get; set; }
    public Exception? CountException { get; set; }
    public List<ContractPaymentDetailReportViewModel> Data { get; set; } = [];
    public List<SearchReportCondition> CountConditions { get; } = [];
    public List<SearchReportCondition> PageConditions { get; } = [];

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<ContractPaymentDetailReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        CountCalls++;
        CountConditions.Add(Copy(searchCondition));
        if (CountException is not null)
        {
            throw CountException;
        }

        return TotalCount;
    }

    public List<ContractPaymentDetailReportViewModel> GetPage(SearchReportCondition searchCondition)
    {
        PageCalls++;
        PageConditions.Add(Copy(searchCondition));
        return Data;
    }

    private static SearchReportCondition Copy(SearchReportCondition source) => new()
    {
        ReportCode = source.ReportCode,
        StartDate = source.StartDate,
        EndDate = source.EndDate,
        EncounterSource = source.EncounterSource,
        BillingCode = source.BillingCode,
        PageNumber = source.PageNumber,
        PageSize = source.PageSize
    };
}

internal sealed class FakeCashierCashSummaryRepository : ICashierCashSummaryRepository
{
    public int CountCalls { get; private set; }
    public int PageCalls { get; private set; }
    public int TotalCount { get; set; }
    public Exception? CountException { get; set; }
    public Exception? PageException { get; set; }
    public List<CashierCashSummaryReportViewModel> Data { get; set; } = [];
    public List<SearchReportCondition> CountConditions { get; } = [];
    public List<SearchReportCondition> PageConditions { get; } = [];

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<CashierCashSummaryReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        CountCalls++;
        CountConditions.Add(Copy(searchCondition));
        if (CountException is not null)
        {
            throw CountException;
        }

        return TotalCount;
    }

    public List<CashierCashSummaryReportViewModel> GetPage(SearchReportCondition searchCondition)
    {
        PageCalls++;
        PageConditions.Add(Copy(searchCondition));
        if (PageException is not null)
        {
            throw PageException;
        }

        return Data;
    }

    private static SearchReportCondition Copy(SearchReportCondition source) => new()
    {
        ReportCode = source.ReportCode,
        StartDate = source.StartDate,
        EndDate = source.EndDate,
        PageNumber = source.PageNumber,
        PageSize = source.PageSize
    };
}

internal sealed class FakeOutpatientReceivableBalanceRepository
    : IOutpatientReceivableBalanceRepository
{
    public int CountCalls { get; private set; }
    public int PageCalls { get; private set; }
    public int TotalCount { get; set; }
    public Exception? CountException { get; set; }
    public Exception? PageException { get; set; }
    public List<OutpatientReceivableBalanceReportViewModel> Data { get; set; } = [];
    public List<SearchReportCondition> CountConditions { get; } = [];
    public List<SearchReportCondition> PageConditions { get; } = [];

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<OutpatientReceivableBalanceReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        CountCalls++;
        CountConditions.Add(Copy(searchCondition));
        if (CountException is not null)
        {
            throw CountException;
        }

        return TotalCount;
    }

    public List<OutpatientReceivableBalanceReportViewModel> GetPage(
        SearchReportCondition searchCondition)
    {
        PageCalls++;
        PageConditions.Add(Copy(searchCondition));
        if (PageException is not null)
        {
            throw PageException;
        }

        return Data;
    }

    private static SearchReportCondition Copy(SearchReportCondition source) => new()
    {
        ReportCode = source.ReportCode,
        EndDate = source.EndDate,
        ReceivableBalanceType = source.ReceivableBalanceType,
        PageNumber = source.PageNumber,
        PageSize = source.PageSize
    };
}

internal sealed class PassthroughReportTotalCountCache : IReportTotalCountCache
{
    public int GetOrCreate(
        string reportCode,
        IReadOnlyDictionary<string, string?> normalizedFilters,
        Func<int> countFactory) => countFactory();

    public void Invalidate(string reportCode) { }
}

internal sealed class FakeSurgicalAccountingRepository : ISurgicalAccountingRepository
{
    public int CountCalls { get; private set; }

    public int PageCalls { get; private set; }

    public int TotalCount { get; set; }

    public Exception? CountException { get; set; }

    public List<SurgicalAccountingReportViewModel> Data { get; set; } = [];

    public string? LastStartDate { get; private set; }

    public string? LastEndDate { get; private set; }

    public List<ModelDescriptionsHelper.PropertyMetadata> GetColumns() =>
        ModelDescriptionsHelper.GetPropertyDescriptions<SurgicalAccountingReportViewModel>();

    public int GetCount(SearchReportCondition searchCondition)
    {
        CountCalls++;
        LastStartDate = searchCondition.StartDate;
        LastEndDate = searchCondition.EndDate;
        if (CountException is not null)
        {
            throw CountException;
        }

        return TotalCount;
    }

    public List<SurgicalAccountingReportViewModel> GetPage(SearchReportCondition searchCondition)
    {
        PageCalls++;
        LastStartDate = searchCondition.StartDate;
        LastEndDate = searchCondition.EndDate;
        return Data;
    }
}

internal sealed class FakeReportExportService : IReportExportService
{
    public ReportExportDispatchResult DispatchResult { get; set; } =
        new(Array.Empty<byte>(), "C174_test.xlsx", null);

    public ReportExportJob? Job { get; set; }

    public ReportExportDownloadResult DownloadResult { get; set; } = new(null, null);

    public Exception? DispatchException { get; set; }

    public int DispatchCalls { get; private set; }

    public ReportExportDispatchResult Dispatch(SearchReportCondition searchCondition)
    {
        DispatchCalls++;
        if (DispatchException is not null)
        {
            throw DispatchException;
        }
        return DispatchResult;
    }

    public ReportExportJob? GetJob(Guid jobId) => Job;

    public ReportExportDownloadResult GetDownload(Guid jobId) => DownloadResult;
}

internal sealed class FakeReportCatalogService : IReportCatalogService
{
    public ReportIndexViewModel GetReportIndex() => new()
    {
        Categories = [],
        DefaultStartDate = "2026-08-18",
        DefaultEndDate = "2026-08-18"
    };
}

internal sealed class CapturingLogger<T> : ILogger<T>
{
    public List<CapturedLog> Entries { get; } = [];

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        var properties = state is IEnumerable<KeyValuePair<string, object?>> values
            ? values.Where(value => value.Key != "{OriginalFormat}")
                .ToDictionary(value => value.Key, value => value.Value, StringComparer.Ordinal)
            : new Dictionary<string, object?>(StringComparer.Ordinal);
        Entries.Add(new CapturedLog(logLevel, exception, formatter(state, exception), properties));
    }
}

internal sealed record CapturedLog(
    LogLevel Level,
    Exception? Exception,
    string Message,
    IReadOnlyDictionary<string, object?> Properties);
