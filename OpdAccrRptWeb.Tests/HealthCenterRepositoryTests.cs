using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;
using OpdAccrRptWeb.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace OpdAccrRptWeb.Tests;

public sealed class HealthCenterRepositoryTests
{
    [Fact]
    public void Constructor_DoesNotResolveConnectionBeforeAQueryRuns()
    {
        var provider = new InvalidConnectionStringProvider();

        _ = new HealthCenterRepository(provider, NullLogger<HealthCenterRepository>.Instance);

        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public void CreateC171Parameters_UsesOneBasedOffsetAndBoundPageSize()
    {
        var parameters = HealthCenterRepository.CreateC171Parameters(new SearchReportCondition
        {
            StartDate = "1150101",
            EndDate = "1150131",
            PageNumber = 3,
            PageSize = 30
        });

        Assert.Equal(60L, ReadProperty<long>(parameters, "rowOffset"));
        Assert.Equal(30, ReadProperty<int>(parameters, "pageSize"));
        Assert.Equal("1150101", ReadProperty<string>(parameters, "strSDate"));
    }

    [Fact]
    public void CreateC171Parameters_MaximumPageNumber_DoesNotOverflowOffset()
    {
        var parameters = HealthCenterRepository.CreateC171Parameters(new SearchReportCondition
        {
            PageNumber = int.MaxValue,
            PageSize = 50
        });

        Assert.Equal(((long)int.MaxValue - 1) * 50, ReadProperty<long>(parameters, "rowOffset"));
    }

    [Fact]
    public void C171PageSql_UsesBoundPagingAndSourceRowIdTieBreaker()
    {
        Assert.Contains("OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY", HealthCenterRepository.C171PageSql);
        Assert.Contains("ORDER BY SourceRowId", HealthCenterRepository.C171PageSql);
        Assert.DoesNotContain("ORDER BY SourceRank", HealthCenterRepository.C171PageSql);
        Assert.Contains("ROWIDTOCHAR(o.ROWID)", HealthCenterRepository.C171BaseSql);
        Assert.Contains("ROWIDTOCHAR(d.ROWID)", HealthCenterRepository.C171BaseSql);

        var publicProjection = HealthCenterRepository.C171PageSql.Split("FROM (", StringSplitOptions.None)[0];
        Assert.DoesNotContain("SourceRowId", publicProjection);
        Assert.DoesNotContain("SourceRank", publicProjection);
    }

    [Fact]
    public void GetHealthCenterDataCount_ConnectionFailure_PropagatesIndependently()
    {
        var repository = CreateRepository(new InvalidConnectionStringProvider());

        Assert.ThrowsAny<Exception>(() => repository.GetHealthCenterDataCount(new SearchReportCondition
        {
            StartDate = "1150801",
            EndDate = "1150831",
            PageNumber = 1,
            PageSize = 10
        }));
    }

    [Fact]
    public void GetHealthCenterDataPage_ConnectionFailure_PropagatesIndependently()
    {
        var repository = CreateRepository(new InvalidConnectionStringProvider());

        Assert.ThrowsAny<Exception>(() => repository.GetHealthCenterDataPage<HealthCenterDetailViewModel>(
            new SearchReportCondition
            {
                StartDate = "1150801",
                EndDate = "1150831",
                PageNumber = 2,
                PageSize = 30
            }));
    }

    [Fact]
    public void ExecuteC171Count_Success_LogsOneStructuredCompletionEvent()
    {
        var logger = new CapturingLogger<HealthCenterRepository>();
        var repository = CreateRepository(new InvalidConnectionStringProvider(), logger);

        var result = repository.ExecuteC171Count(CreateC171Condition(), () => 42);

        Assert.Equal(42, result);
        CapturedLog entry = Assert.Single(logger.Entries);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Equal("C171", entry.Properties["ReportCode"]);
        Assert.True(Assert.IsType<long>(entry.Properties["CountSqlElapsedMs"]) >= 0);
        Assert.Equal(42, entry.Properties["TotalCount"]);
        Assert.Equal(
            ["CountSqlElapsedMs", "EndDate", "ReportCode", "StartDate", "TotalCount"],
            entry.Properties.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ExecuteC171Page_Success_LogsOneStructuredCompletionEvent()
    {
        var logger = new CapturingLogger<HealthCenterRepository>();
        var repository = CreateRepository(new InvalidConnectionStringProvider(), logger);

        var result = repository.ExecuteC171Page(
            CreateC171Condition(),
            () => new List<HealthCenterDetailViewModel> { new() });

        Assert.Single(result);
        CapturedLog entry = Assert.Single(logger.Entries);
        Assert.Equal(Microsoft.Extensions.Logging.LogLevel.Information, entry.Level);
        Assert.Null(entry.Exception);
        Assert.Equal("C171", entry.Properties["ReportCode"]);
        Assert.True(Assert.IsType<long>(entry.Properties["PageSqlElapsedMs"]) >= 0);
        Assert.Equal(2, entry.Properties["PageNumber"]);
        Assert.Equal(30, entry.Properties["PageSize"]);
        Assert.Equal(1, entry.Properties["ReturnedRows"]);
        Assert.Equal(
            ["EndDate", "PageNumber", "PageSize", "PageSqlElapsedMs", "ReportCode", "ReturnedRows", "StartDate"],
            entry.Properties.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ExecuteC171Page_Failure_PropagatesWithoutCompletionEvent()
    {
        var logger = new CapturingLogger<HealthCenterRepository>();
        var repository = CreateRepository(new InvalidConnectionStringProvider(), logger);
        var exception = new InvalidOperationException("query failed");

        var actual = Assert.Throws<InvalidOperationException>(() => repository.ExecuteC171Page<HealthCenterDetailViewModel>(
            CreateC171Condition(),
            () => throw exception));

        Assert.Same(exception, actual);
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public void CreateC174Parameters_UsesOneBasedOffsetAndSharedDateParameters()
    {
        var parameters = HealthCenterRepository.CreateC174Parameters(new SearchReportCondition
        {
            StartDate = "1150801",
            EndDate = "1150831",
            PageNumber = 3,
            PageSize = 30
        });

        Assert.Equal("1150801", ReadProperty<string>(parameters, "strSDate"));
        Assert.Equal("1150831", ReadProperty<string>(parameters, "strEDate"));
        Assert.Equal(60L, ReadProperty<long>(parameters, "rowOffset"));
        Assert.Equal(30, ReadProperty<int>(parameters, "pageSize"));
        Assert.Contains(":strSDate", HealthCenterRepository.C174CountSql);
        Assert.Contains(":strEDate", HealthCenterRepository.C174CountSql);
        Assert.Contains(":strSDate", HealthCenterRepository.C174PageSql);
        Assert.Contains(":strEDate", HealthCenterRepository.C174PageSql);
    }

    [Fact]
    public void CreateC174Parameters_MaximumPageNumber_DoesNotOverflowOffset()
    {
        var parameters = HealthCenterRepository.CreateC174Parameters(new SearchReportCondition
        {
            PageNumber = int.MaxValue,
            PageSize = 50
        });

        Assert.Equal(((long)int.MaxValue - 1) * 50, ReadProperty<long>(parameters, "rowOffset"));
    }

    [Fact]
    public void C174PageSql_UsesBoundPagingAndPrivateDeterministicTieBreaker()
    {
        Assert.Contains("ROWIDTOCHAR(ROWID)  AS SourceRowId", HealthCenterRepository.C174BaseSql);
        Assert.Contains("SourceRowId", HealthCenterRepository.C174PageSql);
        Assert.Contains("OFFSET :rowOffset ROWS FETCH NEXT :pageSize ROWS ONLY", HealthCenterRepository.C174PageSql);

        var publicProjection = HealthCenterRepository.C174PageSql.Split("FROM (", StringSplitOptions.None)[0];
        Assert.DoesNotContain("SourceRowId", publicProjection);
    }

    [Fact]
    public void GetHealthCenterContractBillingReportCount_ConnectionFailure_Propagates()
    {
        var repository = CreateRepository(new InvalidConnectionStringProvider());

        Assert.ThrowsAny<Exception>(() => repository.GetHealthCenterContractBillingReportCount(
            new SearchReportCondition
            {
                StartDate = "1150801",
                EndDate = "1150831",
                PageNumber = 1,
                PageSize = 10
            }));
    }

    [Fact]
    public void GetHealthCenterContractBillingReportBatch_ConnectionFailure_Propagates()
    {
        var repository = CreateRepository(new InvalidConnectionStringProvider());

        Assert.ThrowsAny<Exception>(() => repository.GetHealthCenterContractBillingReportBatch(
            new SearchReportCondition { StartDate = "1150801", EndDate = "1150831" },
            offset: 5_000,
            batchSize: 5_000));
    }

    private static HealthCenterRepository CreateRepository(
        IConnectionStringProvider provider,
        Microsoft.Extensions.Logging.ILogger<HealthCenterRepository>? logger = null) =>
        new(provider, logger ?? NullLogger<HealthCenterRepository>.Instance);

    private static SearchReportCondition CreateC171Condition() => new()
    {
        ReportCode = "C171",
        StartDate = "1150801",
        EndDate = "1150831",
        PageNumber = 2,
        PageSize = 30
    };

    private static T ReadProperty<T>(object value, string name)
    {
        return (T)value.GetType().GetProperty(name)!.GetValue(value)!;
    }

    private sealed class InvalidConnectionStringProvider : IConnectionStringProvider
    {
        public int Calls { get; private set; }

        public string GetConnectionString()
        {
            Calls++;
            return "invalid connection string";
        }
    }
}
