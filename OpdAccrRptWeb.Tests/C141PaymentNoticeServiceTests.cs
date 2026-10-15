using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Tests;

public sealed class C141PaymentNoticeServiceTests
{
    [Fact]
    public void Calculate_ResetsTemporaryAmountForEachCaseAndKeepsItOutOfTotalsAThroughG()
    {
        var firstKey = new C141AdmissionKey("1150101", "1", "A12", 1m);
        var secondKey = new C141AdmissionKey("1150101", "1", "A12", 2m);
        C141PaymentNoticeDocument first = C141PaymentNoticeService.Calculate(
            Source(firstKey, 150m,
            [
                Category("02", 1_000m, 200m, 4m, 1m),
                Category("06", 100m, 100m, 1m, 0m),
                Category("35", 50m, 50m, 0m, 0m)
            ], 50.25m, 10.6m, 80m, true));
        C141PaymentNoticeDocument second = C141PaymentNoticeService.Calculate(
            Source(secondKey, null, [Category("02", 1_000m, 200m, 4m, 1m)], null, null));

        Assert.Equal(350m, first.Categories.Single(line => line.DepartmentCode == "02").PatientAmount);
        Assert.Equal(0m, first.Categories.Single(line => line.DepartmentCode == "06").PatientAmount);
        Assert.Equal(0m, first.Categories.Single(line => line.DepartmentCode == "35").PatientAmount);
        Assert.Equal(200m, second.Categories.Single(line => line.DepartmentCode == "02").PatientAmount);
        Assert.Equal(1_580m, first.Totals.TotalAmountA);
        Assert.Equal(1_150m, first.Totals.InsuranceAmountB);
        Assert.Equal(1m, first.Totals.SubsidyAmountC);
        Assert.Equal(5m, first.Totals.PreferentialAmountD);
        Assert.Equal(424m, first.Totals.AmountDueE);
        Assert.Equal(61.25m, first.Totals.PrepaidAmountF);
        Assert.Equal(362.75m, first.Totals.RemainingAmountG);
        Assert.Equal("尚未讀取健保卡", first.Note1);
        Assert.Equal("繳費請攜卡", first.Note2);
        Assert.Contains("14日", first.Note3);
        Assert.Null(second.Note3);
    }

    [Fact]
    public void Calculate_RoundsEachCategoryBeforeAddingAmounts()
    {
        var key = new C141AdmissionKey("1150101", "1", "A12", 1m);
        C141PaymentNoticeDocument notice = C141PaymentNoticeService.Calculate(Source(
            key,
            null,
            [Category("01", 0.6m, 0m, 0.6m, 0.6m), Category("02", 0.6m, 0m, 0.6m, 0.6m)],
            null,
            null));

        Assert.Equal(2m, notice.Totals.InsuranceAmountB);
        Assert.Equal(2m, notice.Totals.SubsidyAmountC);
        Assert.Equal(2m, notice.Totals.PreferentialAmountD);
        Assert.Equal(2m, notice.Totals.TotalAmountA);
    }

    [Fact]
    public void Calculate_GeneralAdmissionRoundsEachBandAndAddsNoticeLine()
    {
        var key = new C141AdmissionKey("1150101", "1", "A12", 1m);
        var data = new C141PartialPaymentData(new DateOnly(2026, 2, 1),
            [new("000", "1150101", "1150131", false, null)], 0, 0, 999m);
        var repository = new FakeRepository([Source(key, null, [], null, null)], data,
            (begin, end, _) => begin.Day == 1 ? 6m : 3m);

        C141PaymentNoticeDocument notice = new C141PaymentNoticeService(repository).Calculate(key)!;

        Assert.True(notice.PartialPaymentCalculationAvailable);
        Assert.Equal(2m, notice.Totals.TotalAmountA);
        Assert.Equal(2m, notice.Categories.Single(line => line.DepartmentCode == "49").PatientAmount);
        Assert.Equal(2, repository.Periods.Count);
        Assert.Equal(new DateOnly(2026, 1, 30), repository.Periods[0].End);
        Assert.Equal(new DateOnly(2026, 1, 31), repository.Periods[1].Begin);
    }

    [Fact]
    public void Calculate_DayCareUsesFiveAndTenPercentBandsWithCombinedCap()
    {
        var key = new C141AdmissionKey("1150101", "1", "A12", 2m);
        C141PaymentNoticeSource source = Source(key, null, [], null, null) with
        {
            Basic = Source(key, null, [], null, null).Basic with { BedNumber = "01P01" }
        };
        var data = new C141PartialPaymentData(new DateOnly(2026, 2, 1),
            [new("000", "1150101", "1150131", false, null)], 0, 0, 1m);
        var repository = new FakeRepository([source], data, (_, _, isDayCare) => isDayCare ? 20m : 0m);

        C141PaymentNoticeDocument notice = new C141PaymentNoticeService(repository).Calculate(key)!;

        Assert.Equal(1m, notice.Categories.Single(line => line.DepartmentCode == "49").PatientAmount);
        Assert.All(repository.Periods, period => Assert.True(period.IsDayCare));
    }

    [Fact]
    public void Calculate_GeneralAdmissionExemptsFirstThirtyDaysOf999Stay()
    {
        var key = new C141AdmissionKey("1150101", "1", "A12", 3m);
        var data = new C141PartialPaymentData(new DateOnly(2026, 2, 1),
            [new("999", "1150101", "1150131", false, null)], 0, 0, 999m);
        var repository = new FakeRepository([Source(key, null, [], null, null)], data,
            (_, _, _) => 10m);

        C141PaymentNoticeDocument notice = new C141PaymentNoticeService(repository).Calculate(key)!;

        Assert.Equal(1m, notice.Totals.TotalAmountA);
        Assert.Single(repository.Periods);
        Assert.Equal(new DateOnly(2026, 1, 31), repository.Periods[0].Begin);
    }

    [Fact]
    public void Calculate_ReadmissionDaysAdvanceGeneralAdmissionRate()
    {
        var key = new C141AdmissionKey("1150101", "1", "A12", 4m);
        var data = new C141PartialPaymentData(new DateOnly(2026, 1, 2),
            [new("000", "1150101", "1150102", false, null)], 29, 0, 999m);
        var repository = new FakeRepository([Source(key, null, [], null, null)], data,
            (_, _, _) => 6m);

        C141PaymentNoticeDocument notice = new C141PaymentNoticeService(repository).Calculate(key)!;

        Assert.Equal(2m, notice.Totals.TotalAmountA);
        Assert.NotNull(notice.Note3);
        Assert.Equal(2, repository.Periods.Count);
    }

    [Fact]
    public void Calculate_DayCareBuildsBandsFromFirstAdmissionAfterSummingStayLengths()
    {
        var key = new C141AdmissionKey("1150101", "1", "A12", 5m);
        C141PaymentNoticeSource source = Source(key, null, [], null, null) with
        {
            Basic = Source(key, null, [], null, null).Basic with { BedNumber = "01P01" }
        };
        var data = new C141PartialPaymentData(new DateOnly(2026, 2, 1),
            [new("000", "1150101", "1150102", false, null),
             new("000", "1150110", "1150111", false, null)], 0, 0, 999m);
        var repository = new FakeRepository([source], data, (_, _, _) => 20m);

        new C141PaymentNoticeService(repository).Calculate(key);

        Assert.Single(repository.Periods);
        Assert.Equal(new DateOnly(2026, 1, 1), repository.Periods[0].Begin);
        Assert.Equal(new DateOnly(2026, 1, 3), repository.Periods[0].End);
    }

    [Fact]
    public void Calculate_SameDayGeneralStayUsesLegacyTenPercentBranch()
    {
        var key = new C141AdmissionKey("1150101", "1", "A12", 6m);
        var data = new C141PartialPaymentData(new DateOnly(2026, 1, 1),
            [new("999", "1150101", "1150101", false, null)], 0, 0, 999m);
        var repository = new FakeRepository([Source(key, null, [], null, null)], data,
            (_, _, _) => 10m);

        C141PaymentNoticeDocument notice = new C141PaymentNoticeService(repository).Calculate(key)!;

        Assert.Equal(1m, notice.Totals.TotalAmountA);
        Assert.Single(repository.Periods);
    }

    [Fact]
    public void Calculate_GeneralAdmissionRestoresOnlyFinalDischargeDay()
    {
        var key = new C141AdmissionKey("1150101", "1", "A12", 7m);
        var data = new C141PartialPaymentData(new DateOnly(2026, 1, 11),
            [new("000", "1150101", "1150103", false, null),
             new("000", "1150110", "1150111", false, null)], 0, 0, 999m);
        var repository = new FakeRepository([Source(key, null, [], null, null)], data,
            (_, _, _) => 10m);

        new C141PaymentNoticeService(repository).Calculate(key);

        Assert.Equal(2, repository.Periods.Count);
        Assert.Equal(new DateOnly(2026, 1, 2), repository.Periods[0].End);
        Assert.Equal(new DateOnly(2026, 1, 11), repository.Periods[1].End);
    }

    [Fact]
    public void PartialPaymentSqlUsesDistinctLegacyFiltersForDayCareAndGeneralAdmissions()
    {
        string general = C141PaymentNoticeSql.PartialPeriodAmount(false);
        string dayCare = C141PaymentNoticeSql.PartialPeriodAmount(true);

        Assert.Contains("'0','1','4','6','7'", general, StringComparison.Ordinal);
        Assert.Contains("'I','D','N'", general, StringComparison.Ordinal);
        Assert.Contains("'0','1','4'", dayCare, StringComparison.Ordinal);
        Assert.Contains("'I','D'", dayCare, StringComparison.Ordinal);
        Assert.DoesNotContain("'6','7'", dayCare, StringComparison.Ordinal);
    }

    [Fact]
    public void PaymentNoticeSql_PreservesLegacyCategoryAndReceiptConditions()
    {
        Assert.Contains("A.chop4Dct IN ('06','35')", C141PaymentNoticeSql.TemporaryAmount2, StringComparison.Ordinal);
        Assert.Contains("SUM(A.rlop4AMT2)", C141PaymentNoticeSql.TemporaryAmount2, StringComparison.Ordinal);
        Assert.Contains("A.chOp3SPay IN ('0','1','4')", C141PaymentNoticeSql.Categories, StringComparison.Ordinal);
        Assert.Contains("A.chOp4SPay IN ('0','1','4')", C141PaymentNoticeSql.Categories, StringComparison.Ordinal);
        Assert.Contains("SUBSTR(A.chOp4Dct,1,2) <> '49'", C141PaymentNoticeSql.Categories, StringComparison.Ordinal);
        Assert.Contains("SUM(T.intOp2Acc1 + T.intOp2Acc2 + T.intOp2Acc3 + T.intOp2Acc4 + T.intItemI3)", C141PaymentNoticeSql.PayTotal, StringComparison.Ordinal);
        Assert.Contains("chOp4OrdNo LIKE '643DIS%'", C141PaymentNoticeSql.DiscountAmount, StringComparison.Ordinal);
    }

    [Fact]
    public void PaymentNoticeWriteSql_PreservesBeforeAndAfterDocumentOrder()
    {
        Assert.Contains("SET CHLISTDATE=:serverDate7,vchAPayDate=:serverDate7", C141PaymentNoticeSql.UpdateBeforeDocument, StringComparison.Ordinal);
        Assert.Contains("INSERT INTO IpdTranTbl", C141PaymentNoticeSql.InsertNoticeTransaction, StringComparison.Ordinal);
        Assert.Contains("'列印繳款通知單'", C141PaymentNoticeSql.InsertNoticeTransaction, StringComparison.Ordinal);
        Assert.Contains("SET vchAPayDate=:entryDate7", C141PaymentNoticeSql.UpdateAfterDocument, StringComparison.Ordinal);
    }

    private static C141PaymentNoticeSource Source(
        C141AdmissionKey key,
        decimal? temporaryAmount2,
        IReadOnlyList<C141NoticeCategory> categories,
        decimal? payTotal,
        decimal? discountAmount,
        decimal partialPaymentAmount = 0m,
        bool isFourteenDayReadmission = false) =>
        new(key,
            new("C36979", "測試", "01", "內科", "A12", "1150101", "000", "30", "1", "醫師", "1", null),
            categories,
            temporaryAmount2,
            payTotal,
            discountAmount,
            partialPaymentAmount,
            isFourteenDayReadmission);

    private static C141NoticeCategory Category(
        string code,
        decimal amount1,
        decimal amount2,
        decimal subsidy3,
        decimal subsidy5) =>
        new(code, code, amount1, amount2, 0m, 0m, subsidy3, 0m, subsidy5, 0m);

    private sealed class FakeRepository(
        IReadOnlyList<C141PaymentNoticeSource> sources,
        C141PartialPaymentData? partialData = null,
        Func<DateOnly, DateOnly, bool, decimal>? periodAmount = null)
        : IC141PaymentNoticeRepository
    {
        private int _index;

        public int LoadCalls { get; private set; }
        public List<(DateOnly Begin, DateOnly End, bool IsDayCare)> Periods { get; } = [];

        public DateTimeOffset GetOracleNow(CancellationToken cancellationToken) =>
            new(2026, 10, 13, 8, 30, 0, TimeSpan.FromHours(8));

        public void RecordBeforeDocument(
            C141PaymentNoticeDocument document,
            string userId,
            CancellationToken cancellationToken)
        {
        }

        public void RecordAfterDocument(
            C141AdmissionKey key,
            string entryDateRoc,
            CancellationToken cancellationToken)
        {
        }

        public C141PaymentNoticeSource? LoadNoticeSource(
            C141AdmissionKey key,
            CancellationToken cancellationToken)
        {
            LoadCalls++;
            C141PaymentNoticeSource source = sources[_index++];
            return source.Key == key ? source : null;
        }

        public C141PartialPaymentData LoadPartialPaymentData(
            C141AdmissionKey key, string medicalRecordNumber, bool isDayCare,
            CancellationToken cancellationToken) => partialData ?? throw new NotImplementedException();

        public decimal GetPeriodInsuranceAmount(
            C141AdmissionKey key, DateOnly begin, DateOnly end, bool isDayCare,
            CancellationToken cancellationToken)
        {
            Periods.Add((begin, end, isDayCare));
            return periodAmount?.Invoke(begin, end, isDayCare) ?? 0m;
        }
    }
}
