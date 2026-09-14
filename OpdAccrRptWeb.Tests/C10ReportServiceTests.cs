using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C10ReportServiceTests
{
    [Fact]
    public async Task CompleteResult_IsAuditedBeforePaging()
    {
        var repository = new Repository("M1", visitCount: 2);
        var audit = new Audit();
        var calculation = new Calculation(rowCount: 25);
        ReportDataAndColumns<C10ReceivableDetailRow> result = await Service(
            repository,
            calculation,
            audit).ReportC10Async(Condition(page: 2));

        Assert.Equal(2, audit.Calls);
        Assert.Equal(25, calculation.LastSourceData!.Visits.Count + 23);
        Assert.Equal(10, result.Data!.Count);
        Assert.Equal(25, result.TotalCount);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task AuditFailure_DoesNotCalculateOrReturnPartialData()
    {
        var calculation = new Calculation(1);
        var audit = new Audit { Failure = new InvalidOperationException("audit unavailable") };

        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(
            new Repository("M1", 1),
            calculation,
            audit).ReportC10Async(Condition()));

        Assert.Equal(0, calculation.Calls);
    }

    [Fact]
    public async Task DefaultPolicy_FailsClosedWhenAuditWriterIsMissing()
    {
        await Assert.ThrowsAsync<C10AuditNotConfiguredException>(() => Service(
            new Repository("M1", 1),
            new Calculation(1),
            null).ReportC10Async(Condition()));
    }

    [Fact]
    public async Task ConcurrentRequests_DoNotShareRows()
    {
        var first = Service(new Repository("A", 1), new EchoCalculation(), new Audit());
        var second = Service(new Repository("B", 1), new EchoCalculation(), new Audit());

        ReportDataAndColumns<C10ReceivableDetailRow>[] results = await Task.WhenAll(
            first.ReportC10Async(Condition()),
            second.ReportC10Async(Condition()));

        Assert.Equal("A", Assert.Single(results[0].Data!).MedicalRecordNumber);
        Assert.Equal("B", Assert.Single(results[1].Data!).MedicalRecordNumber);
    }

    [Fact]
    public async Task Cancellation_IsPropagatedBeforeRepositoryAccess()
    {
        var repository = new Repository("M1", 1);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Service(
            repository,
            new Calculation(1),
            new Audit()).ReportC10Async(Condition(), cancellation.Token));

        Assert.Equal(0, repository.Calls);
    }

    [Fact]
    public async Task GeneralLog_DoesNotContainPatientIdentifiersOrContactData()
    {
        var logger = new CapturingLogger<ReportService>();
        SearchReportCondition condition = Condition();
        condition.MedicalRecordNo = "SECRET-MRN";

        await Service(new Repository("SECRET-MRN", 1), new EchoCalculation(), new Audit(), logger)
            .ReportC10Async(condition);

        Assert.All(logger.Entries, entry =>
        {
            Assert.DoesNotContain("SECRET-MRN", entry.Message, StringComparison.Ordinal);
            Assert.DoesNotContain(entry.Properties.Values,
                value => value?.ToString()?.Contains("SECRET-MRN", StringComparison.Ordinal) == true);
        });
    }

    private static ReportService Service(
        IC10ReceivableDetailRepository repository,
        IC10AmountCalculationService calculation,
        IC10PatientAccessAuditWriter? audit,
        ILogger<ReportService>? logger = null) => new(
        new FakeHealthCenterRepository(),
        new FakeReferralMemberRepository(),
        new FakeSafeNeedleRepository(),
        new PassthroughReportTotalCountCache(),
        logger ?? NullLogger<ReportService>.Instance,
        c10Repository: repository,
        c10CalculationService: calculation,
        c10AuditWriter: audit);

    private static SearchReportCondition Condition(int page = 1) => new()
    {
        ReportCode = "C10",
        StartDate = "2026-09-01",
        EndDate = "2026-09-01",
        Source = C10Sources.OpdEr,
        RoomScope = C10RoomScopes.All,
        PageNumber = page,
        PageSize = 10
    };

    private sealed class Repository(string medicalRecordNumber, int visitCount)
        : IC10ReceivableDetailRepository
    {
        public int Calls { get; private set; }

        public C10RepositoryResult Load(
            SearchReportCondition condition,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            return new C10RepositoryResult
            {
                Visits = Enumerable.Range(1, visitCount).Select(index => new C10DebtVisit(
                    new C10VisitKey("1150901", "080000", "A", index),
                    "1", "O", medicalRecordNumber, "P", "D", "S", null,
                    null, null, null, null, null, null, 0m)).ToList(),
                Charges = []
            };
        }
    }

    private sealed class Audit : IC10PatientAccessAuditWriter
    {
        public int Calls { get; private set; }
        public Exception? Failure { get; init; }

        public void Write(C10DebtVisit visit, CancellationToken cancellationToken = default)
        {
            Calls++;
            if (Failure is not null) throw Failure;
        }
    }

    private class Calculation(int rowCount) : IC10AmountCalculationService
    {
        public int Calls { get; private set; }
        public C10RepositoryResult? LastSourceData { get; private set; }

        public virtual IReadOnlyList<C10ReceivableDetailRow> Calculate(
            string source,
            C10RepositoryResult sourceData)
        {
            Calls++;
            LastSourceData = sourceData;
            return Enumerable.Range(1, rowCount).Select(index => new C10ReceivableDetailRow
            {
                VisitNumber = index,
                MedicalRecordNumber = sourceData.Visits[0].MedicalRecordNumber
            }).ToList();
        }
    }

    private sealed class EchoCalculation : Calculation
    {
        public EchoCalculation() : base(1) { }
    }
}
