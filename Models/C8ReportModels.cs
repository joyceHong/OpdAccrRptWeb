namespace OpdAccrRptWeb.Models;

public sealed record C8ReportRequest(
    DateOnly? StartDate,
    DateOnly? EndDate,
    int PageNumber = 1,
    int PageSize = 10)
{
    public C8ValidatedRequest Validate()
    {
        if (StartDate is null || StartDate.Value.Year < 1912)
            throw new ArgumentException("請輸入有效的 C8 起始日期。");
        if (EndDate is null || EndDate.Value.Year < 1912)
            throw new ArgumentException("請輸入有效的 C8 截止日期。");
        if (StartDate > EndDate)
            throw new ArgumentException("C8 起始日期不可晚於截止日期。");
        if (PageNumber < 1 || PageSize is not (10 or 30 or 50))
            throw new ArgumentException("C8 查詢或分頁條件不正確。");

        return new(StartDate.Value, EndDate.Value, ToRoc(StartDate.Value), ToRoc(EndDate.Value),
            PageNumber, PageSize);
    }

    public static string ToRoc(DateOnly value) => $"{value.Year - 1911:000}{value:MMdd}";
    public static C8ReportRequest Yesterday(TimeProvider timeProvider) =>
        new(DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime).AddDays(-1),
            DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime).AddDays(-1));
}

public sealed record C8ValidatedRequest(DateOnly StartDate, DateOnly EndDate,
    string RocStartDate, string RocEndDate, int PageNumber, int PageSize);

public sealed record C8SourceRow(string? SectionRoomContext, string? VisitDate,
    string? LegacySectionCode, string? MedicalRecordNo, string? IdentityCode,
    string? ChargeCode, string? ChargeName, decimal? InsuranceUnitPrice,
    decimal? SelfPayUnitPrice, decimal? Quantity, decimal? InsuranceAmount,
    decimal? SelfPayAmount, string? ChargeUserId);

public sealed record C8ReportRow(string VisitDate, string MedicalRecordNo,
    string SectionCode, string IdentityCode, string ChargeUserId, string ChargeCode,
    string ChargeName, decimal InsuranceUnitPrice, decimal SelfPayUnitPrice,
    decimal Quantity, decimal InsuranceAmount, decimal SelfPayAmount);
