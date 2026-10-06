using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.Services;

namespace OpdAccrRptWeb.Tests;

public sealed class RegistrationQueryServiceTests
{
    [Fact]
    public async Task NormalizeFilters_ConvertsDatesUppercasesAndPadsCombinedMedicalRecord()
    {
        var repository = new FakeRepository();
        var organizationUnits = new FakeOrganizationUnits
        {
            Mapping = new(OrganizationUnitSource.Section, "01", "N01", "內科", true)
        };
        var service = Create(repository, organizationUnits, "0");

        RegistrationQueryFilters filters = await service.NormalizeFiltersAsync(new(
            "registered", "2026-10-05", " 12345 ", " a123456789 ",
            "2026-01-02", "01｜內科", "N01｜內科", "0101", "1", "12", "D01｜陳醫師"));

        Assert.Equal("1151005", filters.RegDate);
        Assert.Equal("1150102", filters.BirthDate);
        Assert.Equal("0000012345", filters.MedicalRecordNo);
        Assert.Equal("A123456789", filters.PatientId);
        Assert.Equal("01", filters.SectionNo);
        Assert.Equal(12, filters.RegistrationNo);
        Assert.Equal("D01", filters.DoctorNo);
        Assert.Empty(repository.MergeRequests);
    }

    [Fact]
    public async Task NormalizeFilters_UsesEveryDistinctMergedNumberAndFallsBackToOriginal()
    {
        var repository = new FakeRepository { MergedNumbers = ["AB123", "AB124", "AB123"] };
        var service = Create(repository);

        RegistrationQueryFilters merged = await service.NormalizeFiltersAsync(new(
            "registered", "2026-10-05", "AB123", null, null, null, null, null, null, null, null));

        Assert.Equal(["AB123", "AB124"], merged.MedicalRecordNumbers);
        Assert.Equal(["AB123"], repository.MergeRequests);

        repository.MergedNumbers = [];
        RegistrationQueryFilters fallback = await service.NormalizeFiltersAsync(new(
            "registered", "2026-10-05", "AB123", null, null, null, null, null, null, null, null));

        Assert.Equal(["AB123"], fallback.MedicalRecordNumbers);
    }

    [Fact]
    public async Task Query_ReusesCountAcrossPagesButQueriesEachPage()
    {
        var repository = new FakeRepository
        {
            TotalCount = 35,
            Rows = [Source()]
        };
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = Create(repository, cache: new ReportTotalCountCache(memory));
        RegistrationQueryRequest first = Request(pageNumber: 1, pageSize: 10);
        RegistrationQueryRequest second = first with { PageNumber = 2, PageSize = 30 };

        RegistrationQueryResult firstPage = await service.QueryAsync(first, CancellationToken.None);
        RegistrationQueryResult secondPage = await service.QueryAsync(second, CancellationToken.None);

        Assert.Equal(1, repository.CountCalls);
        Assert.Equal(2, repository.PageCalls);
        Assert.Equal([(0, 10), (30, 30)], repository.PageRequests);
        Assert.Equal(4, firstPage.TotalPages);
        Assert.Equal(2, secondPage.TotalPages);
    }

    [Fact]
    public async Task Query_CountFailureIsNotCached()
    {
        var repository = new FakeRepository
        {
            TotalCount = 1,
            CountFailuresRemaining = 1,
            Rows = [Source()]
        };
        using var memory = new MemoryCache(new MemoryCacheOptions());
        var service = Create(repository, cache: new ReportTotalCountCache(memory));
        RegistrationQueryRequest request = Request();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.QueryAsync(request, CancellationToken.None));
        RegistrationQueryResult result = await service.QueryAsync(request, CancellationToken.None);

        Assert.Equal(2, repository.CountCalls);
        Assert.Equal(1, repository.PageCalls);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Query_SummaryMapsIndependentCountsAndMissingRoomNumbers()
    {
        var repository = new FakeRepository
        {
            Summary = new(12, 5, 6, [2, 4, 6, 8, 10, 12, 14], 0, 0, true)
        };
        var service = Create(repository);

        RegistrationQueryResult result = await service.QueryAsync(new(
            "report", "2026-10-05", null, null, null, null, null, "0101", "1", null, null),
            CancellationToken.None);

        Assert.Empty(result.Rows);
        Assert.Equal(12, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
        Assert.NotNull(result.Summary);
        Assert.Equal(5, result.Summary!.SeenCount);
        Assert.Equal(6, result.Summary.UnseenCount);
        Assert.Equal(7, result.Summary.CancelledCount);
        Assert.Equal([2, 4, 6, 8, 10, 12, 14], result.Summary.CancelledNumbers);
        Assert.Equal(0, result.Summary.CurrentNumber);
        Assert.True(result.Summary.RoomNumberDataMissing);
        Assert.Equal(1, repository.SummaryCalls);
    }

    [Fact]
    public async Task Query_MapsLegacyDisplayFormulasAndCancelledSelectionIdentity()
    {
        var repository = new FakeRepository
        {
            TotalCount = 1,
            Rows = [Source(
                registrationType: "17",
                registrationDate: "1151005",
                registrationNo: 7,
                cancelledFlag: "1",
                digitalStatus: "3",
                firstVisitFlag: "0",
                quoteDisplayFlag: "Q",
                emergencyReturnFlag: "1")]
        };
        var service = Create(repository);

        RegistrationQueryResult result = await service.QueryAsync(Request(), CancellationToken.None);
        RegistrationRow row = Assert.Single(result.Rows);

        Assert.Equal("診間預約", row.RegistrationTypeLabel);
        Assert.Equal("115/10/05", row.RegistrationDate);
        Assert.Equal("DC", row.StateLabel);
        Assert.Equal("Price", row.PricingLabel);
        Assert.Equal("Y", row.EmergencyReturnLabel);
        Assert.True(row.IsCancelled);
        Assert.Contains("1151005|1|0101|7", row.RegistrationKey, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NormalizeFilters_RejectsIncompleteSummaryAndInvalidRegistrationNumber()
    {
        var repository = new FakeRepository();
        var service = Create(repository);

        ArgumentException summaryError = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.QueryAsync(new("summary", "2026-10-05", null, null, null, null, null, "", "1", null, null),
                CancellationToken.None));
        Assert.Contains("報診", summaryError.Message, StringComparison.Ordinal);

        ArgumentException registrationError = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.QueryAsync(new("registered", "2026-10-05", null, null, null, null, null, null, null, "abc", null),
                CancellationToken.None));
        Assert.Contains("序號", registrationError.Message, StringComparison.Ordinal);
        Assert.Equal(0, repository.CountCalls);
    }

    private static RegistrationQueryService Create(
        FakeRepository repository,
        FakeOrganizationUnits? organizationUnits = null,
        string? regFlg = null,
        IReportTotalCountCache? cache = null) =>
        new(
            repository,
            cache ?? new PassthroughReportTotalCountCache(),
            organizationUnits ?? new FakeOrganizationUnits(),
            Options.Create(new RegistrationQueryOptions { RegFlg = regFlg }));

    private static RegistrationQueryRequest Request(int pageNumber = 1, int pageSize = 10) => new(
        "registered", "2026-10-05", null, null, null, null, null, "0101", "1", null, null,
        pageNumber, pageSize);

    private static RegistrationSource Source(
        string registrationType = "1",
        string registrationDate = "1151005",
        int registrationNo = 1,
        string cancelledFlag = "0",
        string digitalStatus = "0",
        string firstVisitFlag = "1",
        string quoteDisplayFlag = "",
        string emergencyReturnFlag = "") => new(
        registrationType, registrationDate, "1", registrationNo, "0101", "AB123", "王小明",
        "A123456789", "0800101", "1", "01", "內科", "D01", "陳醫師", "F1", "F2", "P",
        "S1", "一診", "USER", "1151005", "DCUSER", "1151005", firstVisitFlag,
        digitalStatus, cancelledFlag, quoteDisplayFlag, emergencyReturnFlag);

    private sealed class FakeRepository : IRegistrationQueryRepository
    {
        public int TotalCount { get; set; }
        public int CountCalls { get; private set; }
        public int PageCalls { get; private set; }
        public int SummaryCalls { get; private set; }
        public int CountFailuresRemaining { get; set; }
        public IReadOnlyList<string> MergedNumbers { get; set; } = [];
        public List<string> MergeRequests { get; } = [];
        public List<(int Offset, int PageSize)> PageRequests { get; } = [];
        public IReadOnlyList<RegistrationSource> Rows { get; set; } = [];
        public RegistrationSummarySource Summary { get; set; } = new(0, 0, 0, [], 0, 0, false);

        public int Count(RegistrationQueryFilters filters)
        {
            CountCalls++;
            if (CountFailuresRemaining > 0)
            {
                CountFailuresRemaining--;
                throw new InvalidOperationException("count failure");
            }

            return TotalCount;
        }

        public Task<IReadOnlyList<RegistrationSource>> QueryPageAsync(
            RegistrationQueryFilters filters, int offset, int pageSize, CancellationToken token)
        {
            PageCalls++;
            PageRequests.Add((offset, pageSize));
            return Task.FromResult(Rows);
        }

        public Task<IReadOnlyList<string>> QueryMergedMedicalRecordNumbersAsync(
            string medicalRecordNo, CancellationToken token)
        {
            MergeRequests.Add(medicalRecordNo);
            return Task.FromResult(MergedNumbers);
        }

        public Task<RegistrationSummarySource> QuerySummaryAsync(
            string registrationDate, string time, string room, CancellationToken token)
        {
            SummaryCalls++;
            return Task.FromResult(Summary);
        }

        public Task<IReadOnlyList<RegistrationDoctorOption>> SearchDoctorsAsync(
            string query, CancellationToken token) =>
            Task.FromResult<IReadOnlyList<RegistrationDoctorOption>>([]);
    }

    private sealed class FakeOrganizationUnits : IOrganizationUnitCodeService
    {
        public OrganizationUnitMapping? Mapping { get; init; }

        public Task<OrganizationUnitMapping?> ResolveLegacyCodeAsync(
            string newCode, bool activePlaceOnly, CancellationToken cancellationToken = default) =>
            Task.FromResult(Mapping);

        public Task<OrganizationUnitMapping?> ResolveNewCodeAsync(
            string legacyCode, string roomType, OrganizationUnitMappingScope scope,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Mapping);

        public Task<IReadOnlyList<OrganizationUnitMapping>> SearchAsync(
            string query, bool includeSections, bool includePlaces, bool activePlaceOnly,
            int limit = 20, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<OrganizationUnitMapping>>(Mapping is null ? [] : [Mapping]);
    }
}
