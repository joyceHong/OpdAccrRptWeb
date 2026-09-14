using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C24ReportServiceTests
{
    [Fact]
    public void C24_CreatesCanonicalResultOnceThenPagesWithoutChangingSummary()
    {
        var repository = new Repository();
        var calculation = new Calculation();
        var service = new ReportService(new FakeHealthCenterRepository(), new FakeReferralMemberRepository(),
            new FakeSafeNeedleRepository(), new PassthroughReportTotalCountCache(), NullLogger<ReportService>.Instance,
            c24Repository: repository, c24CalculationService: calculation);
        var condition = new SearchReportCondition
        {
            ReportCode = "C24", StartDate = "2026-09-01", EndDate = "2026-09-01",
            Source = C24Sources.OpdEr, Mode = C24Modes.Accounting, RoomScope = C24RoomScopes.All,
            PageNumber = 2, PageSize = 10
        };

        var result = service.ReportDataAndColumns<C24DebtPaymentDetail>(condition);

        Assert.Equal(1, repository.Calls);
        Assert.Equal(1, repository.PublishCalls);
        Assert.Equal(25, repository.PublishedDetails.Count);
        Assert.Equal(1, calculation.Calls);
        Assert.Equal(10, result.Data!.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
        Assert.Single((IReadOnlyList<C24Summary>)result.Summary!);
    }

    [Fact]
    public void C24_RejectsMismatchedLegacySummaryBeforePublishingOrReturningResult()
    {
        var repository = new Repository();
        var calculation = new Calculation(mismatch: true);
        var service = CreateService(repository, calculation);

        Assert.Throws<C24LegacyPublicationException>(() =>
            service.ReportDataAndColumns<C24DebtPaymentDetail>(Condition()));
        Assert.Equal(0, repository.PublishCalls);
    }

    [Fact]
    public void C24_PublicationFailureDoesNotReturnPartialResult()
    {
        var repository = new Repository { PublishException = new InvalidOperationException("insert failed") };
        var service = CreateService(repository, new Calculation());

        Assert.Throws<InvalidOperationException>(() =>
            service.ReportDataAndColumns<C24DebtPaymentDetail>(Condition()));
        Assert.Equal(1, repository.PublishCalls);
    }

    [Fact]
    public void AccountingSingleDay_CacheHitReadsLegacyWithoutRebuilding()
    {
        var calculation = new Calculation();
        var repository = new Repository
        {
            HasLegacy = true,
            LegacyResult = new(calculation.Result.LegacyDetails, calculation.Result.LegacySummaries)
        };
        var result = CreateService(repository, calculation)
            .ReportDataAndColumns<C24DebtPaymentDetail>(Condition());

        Assert.Equal(1, repository.HasCalls);
        Assert.Equal(0, repository.Calls);
        Assert.Equal(0, repository.PublishCalls);
        Assert.Equal(1, repository.LegacyCalls);
        Assert.Equal(0, calculation.Calls);
        Assert.Equal(25, result.TotalCount);
    }

    [Fact]
    public void AccountingSingleDay_ForceRebuildSkipsCacheCheckAndReadsPublishedLegacy()
    {
        var repository = new Repository { HasLegacy = true };
        var calculation = new Calculation();
        var condition = Condition();
        condition.ForceRebuild = true;

        CreateService(repository, calculation).ReportDataAndColumns<C24DebtPaymentDetail>(condition);

        Assert.Equal(0, repository.HasCalls);
        Assert.Equal(1, repository.Calls);
        Assert.Equal(1, repository.PublishCalls);
        Assert.Equal(1, repository.LegacyCalls);
    }

    [Fact]
    public void AccountingDateRange_ReadsLegacyWithoutExistenceCheckOrRebuild()
    {
        var calculation = new Calculation();
        var repository = new Repository
        {
            LegacyResult = new(calculation.Result.LegacyDetails, calculation.Result.LegacySummaries)
        };
        var condition = Condition();
        condition.EndDate = "2026-09-02";

        CreateService(repository, calculation).ReportDataAndColumns<C24DebtPaymentDetail>(condition);

        Assert.Equal(0, repository.HasCalls);
        Assert.Equal(0, repository.Calls);
        Assert.Equal(0, repository.PublishCalls);
        Assert.Equal(1, repository.LegacyCalls);
        Assert.Equal(0, calculation.Calls);
    }

    [Fact]
    public void AccountingLegacyRead_AppliesRequestFilterOnlyToReturnedRows()
    {
        var calculation = new Calculation();
        var repository = new Repository
        {
            HasLegacy = true,
            LegacyResult = new(calculation.Result.LegacyDetails, calculation.Result.LegacySummaries)
        };
        var condition = Condition();
        condition.MedicalRecordNo = "M03";

        var result = CreateService(repository, calculation)
            .ReportDataAndColumns<C24DebtPaymentDetail>(condition);

        Assert.Single(result.Data!);
        Assert.Equal("M03", result.Data![0].MedicalRecordNo);
        Assert.Equal(25, repository.LegacyResult.Details.Count);
        Assert.Equal(1, ((IReadOnlyList<C24Summary>)result.Summary!)[0].DebtCount);
    }

    [Fact]
    public void AccountingLegacyReadFailure_DoesNotReturnRebuiltCanonicalResult()
    {
        var repository = new Repository { LegacyException = new InvalidOperationException("read failed") };
        var calculation = new Calculation();

        Assert.Throws<InvalidOperationException>(() => CreateService(repository, calculation)
            .ReportDataAndColumns<C24DebtPaymentDetail>(Condition()));
        Assert.Equal(1, repository.PublishCalls);
        Assert.Equal(1, repository.LegacyCalls);
    }

    [Theory]
    [InlineData(C24Sources.OpdEr)]
    [InlineData(C24Sources.Inpatient)]
    public void Billing_UsesDebtSourceAndBypassesAccountingLegacy(string source)
    {
        var repository = new Repository();
        var calculation = new Calculation();
        var condition = Condition();
        condition.Mode = C24Modes.Billing;
        condition.Source = source;

        CreateService(repository, calculation).ReportDataAndColumns<C24DebtPaymentDetail>(condition);

        Assert.Equal(1, repository.Calls);
        Assert.Equal(0, repository.HasCalls);
        Assert.Equal(0, repository.PublishCalls);
        Assert.Equal(0, repository.LegacyCalls);
        Assert.Equal(1, calculation.Calls);
    }

    private static ReportService CreateService(Repository repository, Calculation calculation) =>
        new(new FakeHealthCenterRepository(), new FakeReferralMemberRepository(),
            new FakeSafeNeedleRepository(), new PassthroughReportTotalCountCache(),
            NullLogger<ReportService>.Instance, c24Repository: repository,
            c24CalculationService: calculation);

    private static SearchReportCondition Condition() => new()
    {
        ReportCode = "C24", StartDate = "2026-09-01", EndDate = "2026-09-01",
        Source = C24Sources.OpdEr, Mode = C24Modes.Accounting, RoomScope = C24RoomScopes.All,
        PageNumber = 1, PageSize = 10
    };

    private sealed class Repository : IC24DebtPaymentRepository
    {
        public int Calls { get; private set; }
        public int HasCalls { get; private set; }
        public int LegacyCalls { get; private set; }
        public int PublishCalls { get; private set; }
        public IReadOnlyList<C24LegacyDetailRow> PublishedDetails { get; private set; } = [];
        public IReadOnlyList<C24LegacySummaryRow> PublishedSummaries { get; private set; } = [];
        public bool HasLegacy { get; init; }
        public C24LegacyResult? LegacyResult { get; init; }
        public Exception? PublishException { get; init; }
        public Exception? LegacyException { get; init; }
        public C24RepositoryResult Load(SearchReportCondition condition, CancellationToken cancellationToken = default)
        {
            Calls++;
            return new() { Candidates = [], Patients = [], ChargeItems = [], BillingRows = [] };
        }
        public bool HasLegacyResult(string source, DateOnly accountingDate,
            CancellationToken cancellationToken = default)
        {
            HasCalls++;
            return HasLegacy;
        }
        public C24LegacyResult LoadLegacy(string source, DateOnly startDate, DateOnly endDate,
            CancellationToken cancellationToken = default)
        {
            LegacyCalls++;
            if (LegacyException is not null) throw LegacyException;
            return LegacyResult ?? new C24LegacyResult(PublishedDetails, PublishedSummaries);
        }
        public void PublishLegacy(string source, DateOnly accountingDate,
            IReadOnlyList<C24LegacyDetailRow> details, IReadOnlyList<C24LegacySummaryRow> summaries,
            CancellationToken cancellationToken = default)
        {
            PublishCalls++;
            PublishedDetails = details;
            PublishedSummaries = summaries;
            if (PublishException is not null) throw PublishException;
        }
    }

    private sealed class Calculation : IC24DebtPaymentCalculationService
    {
        public Calculation(bool mismatch = false)
        {
            var details = Enumerable.Range(1, 25).Select(i => new C24DebtPaymentDetail
            {
                AccountingDate = new(2026, 9, 1), VisitDate = new(2026, 9, 1),
                RoomCategory = C24RoomCategory.Outpatient, MedicalRecordNo = $"M{i:00}", PatientName = "P",
                ChargeItemCode = "75", EventAmount = 1m,
                SourceKind = C24SourceKind.Drg, SourceBusinessKey = i.ToString()
            }).ToList();
            var legacyDetails = details.Select(x => new C24LegacyDetailRow(
                x.AccountingDate, x.VisitDate, "O", "門診", x.MedicalRecordNo, null, x.PatientName,
                null, null, null, null, "75", null, 1m, 0m, 1m, null, null, null, null, null, "0")).ToList();
            var legacySummaries = C24DebtPaymentCalculationService.SummarizeLegacy(legacyDetails).ToList();
            if (mismatch) legacySummaries[0] = legacySummaries[0] with { DebtAmount = 999m };
            Result = new C24CanonicalResult
            {
                Details = details,
                Summaries = [new(C24RoomCategory.Outpatient, 25m, 25, 0m, 0, 25m, 25)],
                LegacyDetails = legacyDetails, LegacySummaries = legacySummaries,
                RunId = "run", CorrelationId = "trace"
            };
        }
        public int Calls { get; private set; }
        public C24CanonicalResult Result { get; }
        public C24CanonicalResult Calculate(SearchReportCondition condition, C24RepositoryResult source,
            string? correlationId = null)
        {
            Calls++;
            return Result;
        }
    }
}
