using Microsoft.Extensions.Logging.Abstractions;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C24DebtPaymentCalculationServiceTests
{
    private readonly C24DebtPaymentCalculationService _service =
        new(NullLogger<C24DebtPaymentCalculationService>.Instance);

    [Fact]
    public void GeneralCancellation_PreservesAmountDueAndDecimalPrecision()
    {
        var candidate = Candidate(querySub6: 100.25m, querySub3: 5.50m, dcDate: "20260901");
        var result = _service.Calculate(Accounting(), Data([candidate], [Patient()]));
        var detail = Assert.Single(result.Details);
        Assert.Equal(-100.25m, detail.EventAmount);
        Assert.Equal(-5.50m, detail.DiscountAmount);
        Assert.Equal(100.25m, detail.AmountDue);
    }

    [Theory]
    [InlineData(false, -80.50)]
    [InlineData(true, 80.50)]
    public void Acc69_UsesApprovedDecimalFormula(bool cancelled, decimal expected)
    {
        var candidate = Candidate(kind: C24SourceKind.Acc69, sourceSub1: 80.50m,
            dcDate: cancelled ? "20260901" : null, chargeCode: "69");
        var detail = Assert.Single(_service.Calculate(Accounting(), Data([candidate], [Patient()])).Details);
        Assert.Equal(expected, detail.EventAmount);
        Assert.Equal(-80.50m, detail.AmountDue);
    }

    [Fact]
    public void SameDayCreationCancellationAndExcludedMrns_AreRemoved()
    {
        Assert.Empty(_service.Calculate(Accounting(), Data(
            [Candidate(iDate: "1150901", dcDate: "1150901")], [Patient()])).Details);
        Assert.Empty(_service.Calculate(Accounting(), Data(
            [Candidate()], [Patient(mrn: "C36979")])).Details);
    }

    [Fact]
    public void PostEnrichmentFilterAndDepartmentMapping_AreApplied()
    {
        var condition = Accounting();
        condition.RoomScope = C24RoomScopes.Emergency;
        var detail = Assert.Single(_service.Calculate(condition, Data(
            [Candidate()], [Patient(room: "E", department: "0201")])).Details);
        Assert.Equal(C24RoomCategory.Emergency, detail.RoomCategory);
        Assert.Equal("11910", detail.DepartmentCode);
    }

    [Fact]
    public void MissingAndDuplicatePatient_HaveAtomicBehavior()
    {
        var missing = _service.Calculate(Accounting(), Data([Candidate()], []));
        Assert.Empty(missing.Details);
        Assert.Equal(1, missing.RejectionCount);
        Assert.Throws<C24EnrichmentIntegrityException>(() => _service.Calculate(Accounting(),
            Data([Candidate()], [Patient(), Patient()])));
    }

    [Fact]
    public void MissingChargeName_RetainsCodeAndCountsWarning()
    {
        var result = _service.Calculate(Accounting(), Data([Candidate(chargeCode: "75")], [Patient()]));
        Assert.Equal("75", Assert.Single(result.Details).ChargeItemCode);
        Assert.Null(Assert.Single(result.Details).ChargeItemName);
        Assert.Equal(1, result.WarningCount);
    }

    [Fact]
    public void BillingInpatient_HasCorrectSummaryAndFiltersBackedOutRows()
    {
        var condition = Accounting();
        condition.Mode = C24Modes.Billing;
        condition.Source = C24Sources.Inpatient;
        var rows = new[] { Billing(100m), Billing(50m), Billing(20m, "Y"), Billing(0m) };
        var summary = Assert.Single(_service.Calculate(condition, Data(billing: rows)).Summaries);
        Assert.Equal(C24RoomCategory.Inpatient, summary.RoomCategory);
        Assert.Equal(150m, summary.DebtAmount);
        Assert.Equal(2, summary.DebtCount);
        Assert.Equal(150m, summary.OutstandingAmount);
    }

    [Fact]
    public void MixedSummary_IgnoresNullBucketAndUsesSignedPayment()
    {
        var details = new[] { Detail("75", 100), Detail("10", 40), Detail("69", -60), Detail(null, 20) };
        var summary = Assert.Single(C24DebtPaymentCalculationService.Summarize(details, C24Modes.Accounting));
        Assert.Equal((140m, 2, 60m, 1, 80m, 1),
            (summary.DebtAmount, summary.DebtCount, summary.PaymentAmount, summary.PaymentCount,
                summary.OutstandingAmount, summary.OutstandingCount));
    }

    [Fact]
    public void FilteredResponse_PreservesCompleteLegacyPublicationAndDownstreamFields()
    {
        var condition = Accounting();
        condition.RoomScope = C24RoomScopes.Emergency;
        condition.MedicalRecordNo = "E1";
        var candidates = new[]
        {
            Candidate(visitKey: "VE", businessKey: "KE", createdBy: "USER1"),
            Candidate(visitKey: "VO", businessKey: "KO", createdBy: "USER2")
        };
        var patients = new[]
        {
            Patient("VE", "E1", "E"), Patient("VO", "O1", "O")
        };

        var result = _service.Calculate(condition, Data(candidates, patients));

        Assert.Equal("E1", Assert.Single(result.Details).MedicalRecordNo);
        Assert.Equal(2, result.LegacyDetails.Count);
        var outpatient = Assert.Single(result.LegacyDetails, x => x.MedicalRecordNo == "O1");
        Assert.Equal(new DateOnly(2026, 9, 1), outpatient.VisitDate);
        Assert.Equal("USER2", outpatient.CreatedBy);
        Assert.Equal("0", outpatient.DischargeFlag);
    }

    [Theory]
    [InlineData("OpdEr", "E", "急診")]
    [InlineData("Inpatient", "I", "住院")]
    public void LegacySummary_UsesExactlyTheCompletePublishedDetails(
        string source, string expectedRoomType, string expectedRoomName)
    {
        var condition = Accounting();
        condition.Source = source;
        var candidates = new[]
        {
            Candidate(visitKey: "V1", businessKey: "K1", querySub6: 100m, chargeCode: "75"),
            Candidate(visitKey: "V2", businessKey: "K2", querySub6: 40m, chargeCode: "10"),
            Candidate(visitKey: "V3", businessKey: "K3", kind: C24SourceKind.Acc69,
                sourceSub1: 60m, chargeCode: "69")
        };
        var room = source == C24Sources.Inpatient ? "I" : "E";
        var patients = new[] { Patient("V1", "A1", room), Patient("V2", "A2", room), Patient("V3", "A3", room) };

        var result = _service.Calculate(condition, Data(candidates, patients));
        var summary = Assert.Single(result.LegacySummaries);

        Assert.Equal(expectedRoomType, summary.RoomType);
        Assert.Equal(expectedRoomName, summary.RoomTypeName);
        Assert.Equal((140m, 2m, 60m, 1m, 80m, 1m),
            (summary.DebtAmount, summary.DebtCount, summary.PaymentAmount, summary.PaymentCount,
                summary.OutstandingAmount, summary.OutstandingCount));
    }

    [Fact]
    public void PhoneMask_TrimsAndUsesAtMostTenCharacters()
    {
        Assert.Equal("0912******", C24DebtPaymentCalculationService.MaskPhone("091234567899"));
        Assert.Equal("1234", C24DebtPaymentCalculationService.MaskPhone("1234"));
    }

    private static SearchReportCondition Accounting() => new()
    {
        ReportCode = "C24", StartDate = "2026-09-01", EndDate = "2026-09-01",
        Source = C24Sources.OpdEr, Mode = C24Modes.Accounting, RoomScope = C24RoomScopes.All
    };
    private static C24Candidate Candidate(decimal querySub6 = 10m, decimal querySub3 = 1m,
        decimal sourceSub1 = 0m, string? iDate = "1150901", string? dcDate = null,
        string? chargeCode = "75", C24SourceKind kind = C24SourceKind.Drg,
        string visitKey = "V1", string businessKey = "K1", string? createdBy = null) =>
        new(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 1), visitKey, businessKey, null, null,
            null, chargeCode, iDate, dcDate, querySub6, querySub3, sourceSub1, kind, createdBy);
    private static C24PatientEnrichment Patient(string visitKey = "V1", string mrn = "A1",
        string room = "O", string department = "0101") =>
        new(visitKey, mrn, "Patient", "0912345678", room, department, "P", "C");
    private static C24BillingRow Billing(decimal amount, string? back = null) =>
        new(new DateOnly(2026, 9, 1), "V", "A1", "Patient", "0912345678", "I", null, null, null,
            amount, back, Guid.NewGuid().ToString("N"));
    private static C24RepositoryResult Data(IReadOnlyList<C24Candidate>? candidates = null,
        IReadOnlyList<C24PatientEnrichment>? patients = null, IReadOnlyList<C24BillingRow>? billing = null) => new()
        { Candidates = candidates ?? [], Patients = patients ?? [], ChargeItems = [], BillingRows = billing ?? [] };
    private static C24DebtPaymentDetail Detail(string? code, decimal amount) => new()
    {
        AccountingDate = new(2026, 9, 1), VisitDate = new(2026, 9, 1), RoomCategory = C24RoomCategory.Outpatient,
        MedicalRecordNo = "A", PatientName = "P", ChargeItemCode = code, EventAmount = amount,
        SourceKind = C24SourceKind.Drg
    };
}
