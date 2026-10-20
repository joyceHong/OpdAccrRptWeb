using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using MiniExcelLibs;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class ReportExportServiceTests
{
    [Fact]
    public void Dispatch_UnknownReportCode_RejectsBeforeRepositoryAccess()
    {
        var repository = new FakeHealthCenterRepository();
        var service = CreateService(repository, new FakeJobStore(), new FakeExportQueue());

        Assert.Throws<ArgumentException>(() => service.Dispatch(new SearchReportCondition
        {
            ReportCode = "C999", StartDate = "2026-08-01", EndDate = "2026-08-31"
        }));
        Assert.Equal(0, repository.C171CountCalls);
        Assert.Equal(0, repository.C174CountCalls);
    }

    [Fact]
    public void GenerateWorkbook_OneC174Row_CreatesReadableHeaderAndData()
    {
        var repository = new FakeHealthCenterRepository
        {
            C174ExportData =
            [
                new HealthCenterContractBillingReport
                {
                    BillingCode = "B01",
                    BillingName = "合約記帳",
                    TotalAmount = 123.45m
                }
            ]
        };
        var service = CreateService(repository, new FakeJobStore(), new FakeExportQueue());

        using var output = new MemoryStream();
        service.GenerateWorkbook(RocCondition(), output);

        output.Position = 0;
        IDictionary<string, object?> row = Assert.Single(
            output.Query(useHeaderRow: true).Cast<IDictionary<string, object?>>());
        Assert.Equal("B01", row["記帳代碼"]);
        Assert.Equal(123.45d, row["總金額"]);
    }

    [Fact]
    public void GenerateWorkbook_BlankCellsKeepThinBorders()
    {
        var repository = new FakeHealthCenterRepository
        {
            C174ExportData =
            [
                new HealthCenterContractBillingReport
                {
                    BillingCode = "B01",
                    BillingName = null,
                    TotalAmount = 123.45m
                }
            ]
        };
        var service = CreateService(repository, new FakeJobStore(), new FakeExportQueue());

        using var output = new MemoryStream();
        service.GenerateWorkbook(RocCondition(), output);

        output.Position = 0;
        using SpreadsheetDocument document = SpreadsheetDocument.Open(output, false);
        WorksheetPart worksheetPart = Assert.Single(document.WorkbookPart!.WorksheetParts);
        List<Row> rows = worksheetPart.Worksheet.GetFirstChild<SheetData>()!.Elements<Row>().ToList();
        Row header = rows[0];
        Row data = rows[1];
        string blankCellReference = header.Elements<Cell>()
            .Single(cell => cell.CellValue?.Text == "記帳名稱")
            .CellReference!.Value!;
        Cell blankCell = data.Elements<Cell>()
            .Single(cell => cell.CellReference!.Value == $"{blankCellReference[..^1]}2");

        AssertCellHasThinBorder(document, blankCell);
    }

    [Fact]
    public void GenerateWorkbook_SixtyOneRows_UsesBatchesAndExportsEveryRow()
    {
        var repository = new FakeHealthCenterRepository
        {
            C174ExportData = Enumerable.Range(1, 61)
                .Select(index => new HealthCenterContractBillingReport { BillingCode = $"B{index:000}" })
                .ToList()
        };
        var service = CreateService(
            repository,
            new FakeJobStore(),
            new FakeExportQueue(),
            new ReportExportOptions { BatchSize = 30 });

        using var output = new MemoryStream();
        service.GenerateWorkbook(RocCondition(), output);

        output.Position = 0;
        List<IDictionary<string, object?>> rows = output.Query(useHeaderRow: true)
            .Cast<IDictionary<string, object?>>().ToList();
        Assert.Equal(61, rows.Count);
        Assert.Equal([(0, 30), (30, 30), (60, 30)], repository.C174BatchCalls);
        Assert.Equal("B001", rows[0]["記帳代碼"]);
        Assert.Equal("B061", rows[^1]["記帳代碼"]);
    }

    [Fact]
    public void GenerateWorkbook_C141ExportsTwelveVisibleColumnsAcrossBatches()
    {
        var c141 = new FakeC141RankingService
        {
            Rows = Enumerable.Range(1, 5).Select(CreateC141Row).ToArray()
        };
        var service = CreateService(
            new FakeHealthCenterRepository(),
            new FakeJobStore(),
            new FakeExportQueue(),
            new ReportExportOptions { BatchSize = 2 },
            c141RankingService: c141);

        using var output = new MemoryStream();
        service.GenerateWorkbook(new SearchReportCondition
        {
            ReportCode = "C141",
            EndDate = "2026-10-13",
            C141MedicalRecordNumber = "C36979",
            C141BedNumber = "A12",
            C141MinimumNetAmount = 100m
        }, output);

        output.Position = 0;
        List<IDictionary<string, object?>> rows = output.Query(useHeaderRow: true)
            .Cast<IDictionary<string, object?>>().ToList();
        Assert.Equal(5, rows.Count);
        Assert.Equal(12, rows[0].Count);
        Assert.Equal("C36979", rows[0]["病歷號"]);
        Assert.Equal(1d, rows[0]["累計淨額"]);
        Assert.Equal([(0, 2), (2, 2), (4, 2)], c141.BatchCalls);
        Assert.All(c141.SearchConditions, search =>
        {
            Assert.Equal("C36979", search.MedicalRecordNumber);
            Assert.Equal("A12", search.BedNumber);
            Assert.Equal(100m, search.MinimumNetAmount);
        });
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public void Dispatch_C141UsesConfiguredSynchronousLimit(int synchronousLimit, bool expectJob)
    {
        var c141 = new FakeC141RankingService
        {
            Rows = [CreateC141Row(1), CreateC141Row(2)]
        };
        var store = new FakeJobStore();
        var queue = new FakeExportQueue();
        var service = CreateService(
            new FakeHealthCenterRepository(),
            store,
            queue,
            new ReportExportOptions { SynchronousRowLimit = synchronousLimit, BatchSize = 1 },
            c141RankingService: c141);

        ReportExportDispatchResult result = service.Dispatch(new SearchReportCondition
        {
            ReportCode = "C141",
            EndDate = "2026-10-13",
            C141MedicalRecordNumber = "C36979",
            C141BedNumber = "A12",
            C141MinimumNetAmount = 100m
        }, "FEH\\alice");

        Assert.Equal(expectJob, result.Job is not null);
        Assert.Equal(expectJob, result.Workbook is null);
        if (expectJob)
        {
            Assert.Equal("C141", store.Created!.SearchCondition.ReportCode);
            Assert.Equal("C36979", store.Created.SearchCondition.C141MedicalRecordNumber);
            Assert.Equal("A12", store.Created.SearchCondition.C141BedNumber);
            Assert.Equal(100m, store.Created.SearchCondition.C141MinimumNetAmount);
            Assert.Equal(1, queue.Calls);
        }
        else
        {
            Assert.NotNull(result.FileName);
            Assert.Matches("^C141_\\d{8}_\\d{6}\\.xlsx$", result.FileName);
            Assert.Equal(0, queue.Calls);
        }
    }

    [Fact]
    public void Dispatch_C141ZeroRowsReturnsNoDataWithoutCreatingWorkbookOrJob()
    {
        var c141 = new FakeC141RankingService { Rows = [] };
        var queue = new FakeExportQueue();
        var service = CreateService(
            new FakeHealthCenterRepository(),
            new FakeJobStore(),
            queue,
            c141RankingService: c141);

        ReportExportDispatchResult result = service.Dispatch(new SearchReportCondition
        {
            ReportCode = "C141",
            EndDate = "2026-10-13"
        }, "FEH\\alice");

        Assert.True(result.NoData);
        Assert.Null(result.Workbook);
        Assert.Null(result.Job);
        Assert.Equal(0, queue.Calls);
    }

    [Fact]
    public void C141BackgroundJobStatusAndDownloadAreRestrictedToCreator()
    {
        var c141 = new FakeC141RankingService
        {
            Rows = [CreateC141Row(1), CreateC141Row(2)]
        };
        var store = new FakeJobStore();
        var service = CreateService(
            new FakeHealthCenterRepository(),
            store,
            new FakeExportQueue(),
            new ReportExportOptions { SynchronousRowLimit = 1 },
            c141RankingService: c141);

        ReportExportDispatchResult result = service.Dispatch(new SearchReportCondition
        {
            ReportCode = "C141",
            EndDate = "2026-10-13"
        }, "FEH\\alice");
        Guid jobId = result.Job!.JobId;

        Assert.Equal("FEH\\alice", result.Job.CreatedBy);
        Assert.NotNull(service.GetJob(jobId, "feh\\ALICE"));
        Assert.Null(service.GetJob(jobId, "FEH\\bob"));
        Assert.Null(service.GetJob(jobId));
        ReportExportDownloadResult denied = service.GetDownload(jobId, "FEH\\bob");
        Assert.Null(denied.Job);
        Assert.Null(denied.Content);
    }

    [Fact]
    public void BackgroundJobForOtherReportRecordsAndChecksCreator()
    {
        var repository = new FakeHealthCenterRepository { C174TotalCount = 30_001 };
        var service = CreateService(repository, new FakeJobStore(), new FakeExportQueue());

        ReportExportDispatchResult result = service.Dispatch(WebCondition(), "F1482");
        Guid id = result.Job!.JobId;

        Assert.Equal("F1482", result.Job.CreatedBy);
        Assert.NotNull(service.GetJob(id, "F1482"));
        Assert.Null(service.GetJob(id, "OTHER"));
        Assert.Null(service.GetDownload(id, "OTHER").Job);
    }

    [Fact]
    public void Dispatch_C141RequiresServerProvidedOwnerIdentity()
    {
        var c141 = new FakeC141RankingService { Rows = [CreateC141Row(1)] };
        var service = CreateService(
            new FakeHealthCenterRepository(),
            new FakeJobStore(),
            new FakeExportQueue(),
            c141RankingService: c141);

        Assert.Throws<ArgumentException>(() => service.Dispatch(new SearchReportCondition
        {
            ReportCode = "C141",
            EndDate = "2026-10-13"
        }));
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(30_000, false)]
    [InlineData(30_001, true)]
    public void Dispatch_ThresholdBoundary_SelectsExpectedMode(int totalCount, bool background)
    {
        var repository = new FakeHealthCenterRepository { C174TotalCount = totalCount };
        var store = new FakeJobStore();
        var queue = new FakeExportQueue();
        var service = CreateService(repository, store, queue);

        var result = service.Dispatch(WebCondition());

        Assert.Equal(background, result.Job is not null);
        Assert.Equal(background, result.Workbook is null);
        Assert.Equal(background, queue.Calls == 1);
    }

    [Fact]
    public void Dispatch_FullQueue_RemovesCreatedJob()
    {
        var repository = new FakeHealthCenterRepository { C174TotalCount = 30_001 };
        var store = new FakeJobStore();
        var service = CreateService(repository, store, new FakeExportQueue { Accept = false });

        var result = service.Dispatch(WebCondition());

        Assert.True(result.QueueFull);
        Assert.Equal(store.Created!.JobId, store.RemovedJobId);
    }

    [Theory]
    [InlineData("C171")]
    [InlineData("C172")]
    [InlineData("C173")]
    [InlineData("C174")]
    [InlineData("C18")]
    [InlineData("C19")]
    public void Dispatch_ThirtyThousandOneRows_QueuesEveryHealthReportDefinition(string reportCode)
    {
        var health = new FakeHealthCenterRepository
        {
            TotalCount = 30_001,
            C172TotalCount = 30_001,
            C173TotalCount = 30_001,
            C174TotalCount = 30_001
        };
        var referral = new FakeReferralMemberRepository { TotalCount = 30_001 };
        var safeNeedle = new FakeSafeNeedleRepository { TotalCount = 30_001 };
        var queue = new FakeExportQueue();
        ReportExportService service = CreateService(
            health, new FakeJobStore(), queue, referralMemberRepository: referral,
            safeNeedleRepository: safeNeedle);
        string endDate = reportCode == "C19" ? "2026-08-01" : "2026-08-31";

        ReportExportDispatchResult result = service.Dispatch(new SearchReportCondition
        {
            ReportCode = reportCode,
            StartDate = "2026-08-01",
            EndDate = endDate,
            EncounterSource = EncounterSources.Emergency
        });

        Assert.NotNull(result.Job);
        Assert.Null(result.Workbook);
        Assert.Equal(1, queue.Calls);
    }

    [Theory]
    [InlineData("C171", "記帳代碼", "P01")]
    [InlineData("C172", "責任中心代碼", "CC01")]
    [InlineData("C173", "就診月份", "11508")]
    [InlineData("C18", "診院代碼", "H01")]
    [InlineData("C19", "醫令碼", "SDS3")]
    public void GenerateWorkbook_SupportedHealthReport_UsesDefinitionColumnsAndRows(
        string reportCode,
        string expectedHeader,
        string expectedValue)
    {
        var health = new FakeHealthCenterRepository();
        var referral = new FakeReferralMemberRepository();
        var safeNeedle = new FakeSafeNeedleRepository();
        switch (reportCode)
        {
            case "C171":
                health.C171ExportData.Add(new HealthCenterDetailViewModel { PostingCode = expectedValue });
                break;
            case "C172":
                health.C172ExportData.Add(new HealthCenterCountViewModel { CenterCode = expectedValue });
                break;
            case "C173":
                health.C173ExportData.Add(new HealthCheckupVisits
                {
                    Chop1date = expectedValue,
                    Chop1sec = "0294"
                });
                break;
            case "C18":
                referral.Data.Add(new ReferralMemberReportViewModel { ClinicCode = expectedValue });
                break;
            case "C19":
                safeNeedle.Data.Add(new SafeNeedleReportViewModel { OrderCode = expectedValue });
                break;
        }
        ReportExportService service = CreateService(
            health, new FakeJobStore(), new FakeExportQueue(),
            new ReportExportOptions { BatchSize = 1 }, referral, safeNeedle);

        using var output = new MemoryStream();
        service.GenerateWorkbook(new SearchReportCondition
        {
            ReportCode = reportCode,
            StartDate = "1150801",
            EndDate = "1150831",
            EncounterSource = EncounterSources.Emergency
        }, output);

        output.Position = 0;
        IDictionary<string, object?> row = Assert.Single(
            output.Query(useHeaderRow: true).Cast<IDictionary<string, object?>>());
        Assert.Equal(expectedValue, row[expectedHeader]);
    }

    [Fact]
    public void GenerateWorkbook_C1_UsesFullResultAndConvertsIsoDatesForRepository()
    {
        var repository = new FakeSurgicalAccountingRepository
        {
            Data = [new SurgicalAccountingReportViewModel
            {
                EncounterType = "門診", SurgicalOrderCode = "64202B", Amount = 123.45m
            }]
        };
        var service = new ReportExportService(
            new FakeHealthCenterRepository(), new FakeJobStore(), new FakeExportQueue(),
            Options.Create(new ReportExportOptions { BatchSize = 30 }), TimeProvider.System,
            surgicalAccountingRepository: repository);

        using var output = new MemoryStream();
        service.GenerateWorkbook(new SearchReportCondition
        {
            ReportCode = "C1", StartDate = "2026-08-01", EndDate = "2026-08-31"
        }, output);

        output.Position = 0;
        IDictionary<string, object?> row = Assert.Single(
            output.Query(useHeaderRow: true).Cast<IDictionary<string, object?>>());
        Assert.Equal("64202B", row["手術碼"]);
        Assert.Equal(123.45d, row["金額"]);
        Assert.Equal("1150801", repository.LastStartDate);
        Assert.Equal("1150831", repository.LastEndDate);
    }

    [Fact]
    public void GenerateWorkbook_C143_CombinesDischargedAndInHospitalRowsInOneWorksheet()
    {
        var repository = new FakeC143Repository
        {
            DischargedRows =
            [
                new() { MedicalRecordNumber = "D1" },
                new() { MedicalRecordNumber = "D2" }
            ],
            InHospitalRows =
            [
                new() { MedicalRecordNumber = "I1" },
                new() { MedicalRecordNumber = "I2" }
            ]
        };
        var services = new ServiceCollection();
        services.AddSingleton<IC143AccountingBalanceDebtRepository>(repository);
        using ServiceProvider provider = services.BuildServiceProvider();
        var service = new ReportExportService(
            new FakeHealthCenterRepository(), new FakeJobStore(), new FakeExportQueue(),
            Options.Create(new ReportExportOptions { BatchSize = 3 }), TimeProvider.System,
            provider.GetRequiredService<IServiceScopeFactory>());

        using var output = new MemoryStream();
        service.GenerateWorkbook(new SearchReportCondition
        {
            ReportCode = "C143", StartDate = "2026-08-01", EndDate = "2026-08-31",
            Source = C143Sources.Inpatient, ReportType = C143ReportTypes.All
        }, output);

        output.Position = 0;
        List<IDictionary<string, object?>> rows = output.Query(useHeaderRow: true)
            .Cast<IDictionary<string, object?>>().ToList();
        Assert.Equal(["D1", "D2", "I1", "I2"],
            rows.Select(row => row["病歷號"]));
    }

    [Fact]
    public void NormalizeHealthReport_C19_PreservesRequiredFiltersAndConvertsDates()
    {
        SearchReportCondition normalized = ReportExportService.NormalizeHealthReport(new SearchReportCondition
        {
            ReportCode = "C19",
            StartDate = "2026-08-05",
            EndDate = "2026-08-05",
            EncounterSource = EncounterSources.Inpatient,
            StationOrBedPrefix = " 7A "
        });

        Assert.Equal("1150805", normalized.StartDate);
        Assert.Equal("1150805", normalized.EndDate);
        Assert.Equal(EncounterSources.Inpatient, normalized.EncounterSource);
        Assert.Equal("7A", normalized.StationOrBedPrefix);
    }

    [Fact]
    public void Dispatch_C144_UsesSharedMiniExcelDefinitionAndStableBatches()
    {
        var c144Repository = new FakeC144Repository
        {
            TotalCount = 1,
            Data =
            [
                new C144DebtDetailReportViewModel
                {
                    EncounterType = "E",
                    MedicalRecordNumber = null,
                    OutstandingAmount = 99m
                }
            ]
        };
        var services = new ServiceCollection();
        services.AddSingleton<IC144DebtDetailReportRepository>(c144Repository);
        using ServiceProvider provider = services.BuildServiceProvider();
        var service = new ReportExportService(
            new FakeHealthCenterRepository(), new FakeJobStore(), new FakeExportQueue(),
            Options.Create(new ReportExportOptions { BatchSize = 1 }), TimeProvider.System,
            provider.GetRequiredService<IServiceScopeFactory>());

        ReportExportDispatchResult result = service.Dispatch(new SearchReportCondition
        {
            ReportCode = "C144",
            StartDate = "2026-08-01",
            EndDate = "2026-08-31",
            Source = C144Sources.OpdEr
        });

        Assert.Null(result.Job);
        Assert.StartsWith("C144_", result.FileName, StringComparison.Ordinal);
        using var output = new MemoryStream(result.Workbook!);
        IDictionary<string, object?> row = Assert.Single(
            output.Query(useHeaderRow: true).Cast<IDictionary<string, object?>>());
        Assert.Equal(31, row.Count);
        Assert.Equal("E", row["診別"]);
        Assert.Equal(99d, row["尚欠"]);
        Assert.Null(row["病歷號"]);
        Assert.Equal([(0, 1), (1, 1)], c144Repository.BatchCalls);
    }

    [Fact]
    public void Dispatch_C144ThirtyThousandOneRows_QueuesBackgroundJobWithoutReadingRows()
    {
        var c144Repository = new FakeC144Repository { TotalCount = 30_001 };
        var services = new ServiceCollection();
        services.AddSingleton<IC144DebtDetailReportRepository>(c144Repository);
        using ServiceProvider provider = services.BuildServiceProvider();
        var queue = new FakeExportQueue();
        var service = new ReportExportService(
            new FakeHealthCenterRepository(), new FakeJobStore(), queue,
            Options.Create(new ReportExportOptions()), TimeProvider.System,
            provider.GetRequiredService<IServiceScopeFactory>());

        ReportExportDispatchResult result = service.Dispatch(new SearchReportCondition
        {
            ReportCode = "C144", StartDate = "2026-08-01", EndDate = "2026-08-31",
            Source = C144Sources.OpdEr
        });

        Assert.NotNull(result.Job);
        Assert.Null(result.Workbook);
        Assert.Empty(c144Repository.BatchCalls);
        Assert.Equal(1, queue.Calls);
    }

    private static ReportExportService CreateService(
        FakeHealthCenterRepository repository,
        IReportExportJobStore store,
        IReportExportQueue queue,
        ReportExportOptions? options = null,
        IReferralMemberRepository? referralMemberRepository = null,
        ISafeNeedleRepository? safeNeedleRepository = null,
        IC141RankingService? c141RankingService = null)
    {
        IServiceScopeFactory? scopeFactory = null;
        if (c141RankingService is not null)
        {
            var services = new ServiceCollection();
            services.AddSingleton(c141RankingService);
            scopeFactory = services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
        }

        return new ReportExportService(
            repository, store, queue, Options.Create(options ?? new ReportExportOptions()),
            TimeProvider.System, scopeFactory, referralMemberRepository, safeNeedleRepository);
    }

    private static SearchReportCondition WebCondition() => new()
    {
        ReportCode = "C174",
        StartDate = "2026-08-01",
        EndDate = "2026-08-31"
    };

    private static SearchReportCondition RocCondition() => new()
    {
        ReportCode = "C174",
        StartDate = "1150801",
        EndDate = "1150831"
    };

    private static C141RankingRow CreateC141Row(int number) =>
        new(new C141AdmissionKey("1150101", "1", "A12", number),
            "C36979", "測試", "A12", "1150101", "30", "1", "000", "1", "1",
            null, 200m, 120m, null, null, 65m, number, false);

    private static void AssertCellHasThinBorder(SpreadsheetDocument document, Cell cell)
    {
        Assert.NotNull(cell.StyleIndex);
        Stylesheet stylesheet = document.WorkbookPart!.WorkbookStylesPart!.Stylesheet!;
        CellFormat cellFormat = stylesheet.CellFormats!.Elements<CellFormat>()
            .ElementAt(checked((int)cell.StyleIndex!.Value));
        Assert.NotNull(cellFormat.BorderId);
        Border border = stylesheet.Borders!.Elements<Border>()
            .ElementAt(checked((int)cellFormat.BorderId!.Value));

        Assert.Equal(BorderStyleValues.Thin, border.LeftBorder?.Style?.Value);
        Assert.Equal(BorderStyleValues.Thin, border.RightBorder?.Style?.Value);
        Assert.Equal(BorderStyleValues.Thin, border.TopBorder?.Style?.Value);
        Assert.Equal(BorderStyleValues.Thin, border.BottomBorder?.Style?.Value);
    }

    private sealed class FakeExportQueue : IReportExportQueue
    {
        public bool Accept { get; init; } = true;

        public int Calls { get; private set; }

        public bool TryEnqueue(ReportExportJob job)
        {
            Calls++;
            return Accept;
        }
    }

    private sealed class FakeC141RankingService : IC141RankingService
    {
        public IReadOnlyList<C141RankingRow> Rows { get; init; } = [];
        public List<(int Offset, int PageSize)> BatchCalls { get; } = [];
        public List<C141RankingSearch> SearchConditions { get; } = [];

        public DateOnly GetOracleToday(CancellationToken cancellationToken = default) =>
            new(2026, 10, 13);

        public IReadOnlyList<C141RankingRow> Query(
            C141RankingSearch search,
            CancellationToken cancellationToken = default) => Rows;

        public int GetCount(C141RankingSearch search, CancellationToken cancellationToken = default)
        {
            SearchConditions.Add(search);
            return Rows.Count;
        }

        public IReadOnlyList<C141RankingRow> GetPage(
            C141RankingSearch search,
            int offset,
            int pageSize,
            CancellationToken cancellationToken = default)
        {
            SearchConditions.Add(search);
            BatchCalls.Add((offset, pageSize));
            return Rows.Skip(offset).Take(pageSize).ToArray();
        }
    }

    private sealed class FakeJobStore : IReportExportJobStore
    {
        public ReportExportJob? Created { get; private set; }

        public Guid? RemovedJobId { get; private set; }

        public ReportExportJob Create(SearchReportCondition condition, string? createdBy = null) => Created = new ReportExportJob
        {
            JobId = Guid.NewGuid(),
            SearchCondition = condition,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            Status = ReportExportJobStatus.Queued
        };

        public ReportExportJob? Get(Guid jobId) => Created?.JobId == jobId ? Created : null;
        public string GetTemporaryPath(Guid jobId) => throw new NotSupportedException();
        public string GetCompletedPath(ReportExportJob job) => throw new NotSupportedException();
        public void MarkRunning(Guid jobId) => throw new NotSupportedException();
        public void MarkReady(Guid jobId, string fileName) => throw new NotSupportedException();
        public void MarkFailed(Guid jobId, string message) => throw new NotSupportedException();
        public void Remove(Guid jobId) => RemovedJobId = jobId;
        public void RecoverInterruptedJobs() => throw new NotSupportedException();
        public void CleanupExpired() => throw new NotSupportedException();
    }

    private sealed class FakeC144Repository : IC144DebtDetailReportRepository
    {
        public int TotalCount { get; init; }
        public List<C144DebtDetailReportViewModel> Data { get; init; } = [];
        public List<(int Offset, int BatchSize)> BatchCalls { get; } = [];

        public int GetCount(C144Query query, CancellationToken cancellationToken = default) => TotalCount;

        public List<C144DebtDetailReportViewModel> GetPage(
            C144Query query, int offset, int pageSize, CancellationToken cancellationToken = default) =>
            Data.Skip(offset).Take(pageSize).ToList();

        public List<C144DebtDetailReportViewModel> GetBatch(
            C144Query query, int offset, int batchSize, CancellationToken cancellationToken = default)
        {
            BatchCalls.Add((offset, batchSize));
            return Data.Skip(offset).Take(batchSize).ToList();
        }

        public List<C144DebtDetailReportViewModel> GetAll(
            C144Query query, CancellationToken cancellationToken = default) => Data;
    }

    private sealed class FakeC143Repository : IC143AccountingBalanceDebtRepository
    {
        public List<C143AccountingBalanceDebtReportViewModel> OutpatientRows { get; init; } = [];
        public List<C143AccountingBalanceDebtReportViewModel> DischargedRows { get; init; } = [];
        public List<C143AccountingBalanceDebtReportViewModel> InHospitalRows { get; init; } = [];

        public int GetOutpatientEmergencyCount(
            C143Query query, CancellationToken cancellationToken = default) => OutpatientRows.Count;

        public List<C143AccountingBalanceDebtReportViewModel> GetOutpatientEmergencyPage(
            C143Query query, int offset, int pageSize, CancellationToken cancellationToken = default) =>
            OutpatientRows.Skip(offset).Take(pageSize).ToList();

        public int GetInpatientCount(
            C143Query query, int dischargeGroup, CancellationToken cancellationToken = default) =>
            dischargeGroup == 1 ? DischargedRows.Count : InHospitalRows.Count;

        public List<C143AccountingBalanceDebtReportViewModel> GetInpatientPage(
            C143Query query, int dischargeGroup, int offset, int pageSize,
            CancellationToken cancellationToken = default) =>
            (dischargeGroup == 1 ? DischargedRows : InHospitalRows)
                .Skip(offset).Take(pageSize).ToList();
    }
}
