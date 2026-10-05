using Microsoft.Extensions.Caching.Memory;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class MedicalRecordQueryServiceTests
{
    [Fact]
    public void NormalizeFilters_TrimsTruncatesUppercasesAndConvertsGregorianBirthday()
    {
        MedicalRecordQueryFilters filters = MedicalRecordQueryService.NormalizeFilters(new(
            " ab1234567890 ", " id ", new string('名', 12), new string('a', 65), " address2 ",
            " new-id ", "新姓名", "2026-10-05"));

        Assert.Equal("AB12345678", filters.MedicalRecordNo);
        Assert.Equal("id", filters.IdentityNumber);
        Assert.Equal(new string('名', 10), filters.Name);
        Assert.Equal(new string('a', 60), filters.Address1);
        Assert.Equal("address2", filters.Address2);
        Assert.Equal("new-id", filters.NewIdentityNumber);
        Assert.Equal("新姓名", filters.NewName);
        Assert.Equal("1151005", filters.NewBirthday);
    }

    [Fact]
    public async Task Query_RejectsUnrestrictedRequestBeforeRepository()
    {
        var repository = new FakeRepository();
        var service = new MedicalRecordQueryService(repository, new PassthroughReportTotalCountCache());

        ArgumentException exception = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.QueryAsync(new(null, null, null, null, null, null, null, null), CancellationToken.None));

        Assert.Equal("本資料庫可能很大不可使用自由查詢,請輸入資料再查", exception.Message);
        Assert.Equal(0, repository.CountCalls);
        Assert.Equal(0, repository.PageCalls);
    }

    [Fact]
    public async Task Query_ReusesCountAcrossPagesButExecutesEachPageQuery()
    {
        var repository = new FakeRepository { TotalCount = 35 };
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = new MedicalRecordQueryService(repository, new ReportTotalCountCache(memory));
        MedicalRecordQueryRequest first = new("ab123", null, null, null, null, null, null, null, 1, 10);
        MedicalRecordQueryRequest second = first with { PageNumber = 2, PageSize = 30 };

        MedicalRecordQueryPage firstPage = await service.QueryAsync(first, CancellationToken.None);
        MedicalRecordQueryPage secondPage = await service.QueryAsync(second, CancellationToken.None);

        Assert.Equal(1, repository.CountCalls);
        Assert.Equal(2, repository.PageCalls);
        Assert.Equal(0, repository.Offsets[0]);
        Assert.Equal(30, repository.Offsets[1]);
        Assert.Equal(4, firstPage.TotalPages);
        Assert.Equal(2, secondPage.TotalPages);
    }

    [Fact]
    public async Task Query_UsesDifferentCountEntriesForDifferentFilters()
    {
        var repository = new FakeRepository { TotalCount = 1 };
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = new MedicalRecordQueryService(repository, new ReportTotalCountCache(memory));

        await service.QueryAsync(new("AB123", null, null, null, null, null, null, null), CancellationToken.None);
        await service.QueryAsync(new("AB124", null, null, null, null, null, null, null), CancellationToken.None);

        Assert.Equal(2, repository.CountCalls);
        Assert.Equal(2, repository.PageCalls);
    }

    [Fact]
    public async Task Query_RetriesCountAfterCountFailureWithoutCachingFailure()
    {
        var repository = new FakeRepository { TotalCount = 1, CountFailuresRemaining = 1 };
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = new MedicalRecordQueryService(repository, new ReportTotalCountCache(memory));
        MedicalRecordQueryRequest request = new("AB123", null, null, null, null, null, null, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.QueryAsync(request, CancellationToken.None));
        MedicalRecordQueryPage result = await service.QueryAsync(request, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal(2, repository.CountCalls);
        Assert.Equal(1, repository.PageCalls);
    }

    [Fact]
    public async Task QueryDetail_RecalculatesMergedDebtAndMasksPhone()
    {
        var repository = new FakeRepository
        {
            Detail = DetailSource("02-12345678"),
            MergedNumbers = ["0000000001", "0000000002", "0000000001"],
            DebtTotal = 123.45m
        };
        var service = new MedicalRecordQueryService(repository, new PassthroughReportTotalCountCache());

        MedicalRecordDetail detail = await service.QueryDetailAsync(
            new("ab123"),
            CancellationToken.None);

        Assert.Equal("AB123", repository.DetailMedicalRecordNo);
        Assert.Equal(123.45m, detail.DebtTotal);
        Assert.Equal("02-1*******", detail.HomePhone);
        Assert.DoesNotContain("12345678", detail.HomePhone, StringComparison.Ordinal);
        Assert.Equal(["0000000001", "0000000002"], repository.DebtNumbers);
    }

    [Fact]
    public void MaskHomePhone_PreservesLegacyLengthAndEmptyValues()
    {
        Assert.Equal("02-1*******", MedicalRecordQueryService.MaskHomePhone("02-12345678"));
        Assert.Equal(string.Empty, MedicalRecordQueryService.MaskHomePhone(null));
        Assert.Equal(string.Empty, MedicalRecordQueryService.MaskHomePhone("   "));
    }

    private static MedicalRecordDetailSource DetailSource(string homePhone) => new(
        "AB123", "A123456789", "王小明", "0700101", "0", "1", "O", "1", "1", homePhone,
        "02-22222222", "1", "0", "220", "台北市", "221", "新北市", "台灣", "1", "1", "台北",
        "B123456789", "王新名", "1150101", "法名", "OLD123", "0", "0700101", 999m, "SPOUSE", "FATHER");

    private sealed class FakeRepository : IMedicalRecordQueryRepository
    {
        public int TotalCount { get; init; } = 1;
        public int CountCalls { get; private set; }
        public int PageCalls { get; private set; }
        public int CountFailuresRemaining { get; set; }
        public List<int> Offsets { get; } = [];
        public MedicalRecordDetailSource? Detail { get; init; }
        public string[] MergedNumbers { get; init; } = [];
        public decimal? DebtTotal { get; init; }
        public string? DetailMedicalRecordNo { get; private set; }
        public IReadOnlyList<string> DebtNumbers { get; private set; } = [];

        public int Count(MedicalRecordQueryFilters filters)
        {
            CountCalls++;
            if (CountFailuresRemaining > 0)
            {
                CountFailuresRemaining--;
                throw new InvalidOperationException("count failure");
            }
            return TotalCount;
        }

        public Task<IReadOnlyList<MedicalRecordSource>> QueryPageAsync(
            MedicalRecordQueryFilters filters,
            int offset,
            int pageSize,
            CancellationToken token)
        {
            PageCalls++;
            Offsets.Add(offset);
            return Task.FromResult<IReadOnlyList<MedicalRecordSource>>([
                new("AB123", "A123456789", "王小明", "0700101", "0", "1", "O", "1", "1", "02-12345678", "02-22222222", "1", "0")
            ]);
        }

        public Task<MedicalRecordDetailSource?> QueryDetailAsync(string medicalRecordNo, CancellationToken token)
        {
            DetailMedicalRecordNo = medicalRecordNo;
            return Task.FromResult(Detail);
        }

        public Task<IReadOnlyList<string>> QueryMergedMedicalRecordNumbersAsync(string medicalRecordNo, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<string>>(MergedNumbers);

        public Task<decimal?> QueryDebtTotalAsync(IReadOnlyCollection<string> medicalRecordNumbers, CancellationToken token)
        {
            DebtNumbers = medicalRecordNumbers.ToArray();
            return Task.FromResult(DebtTotal);
        }
    }
}
