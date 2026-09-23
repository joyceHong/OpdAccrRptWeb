namespace OpdAccrRptWeb.Models;

public sealed record C9ReportRequest(
    DateOnly? StartDate,
    DateOnly? EndDate,
    int PageNumber = 1,
    int PageSize = 10)
{
    public C9ValidatedRequest Validate()
    {
        if (StartDate is null || StartDate.Value.Year < 1912)
            throw new ArgumentException("請輸入有效的 C9 起始日期。");
        if (EndDate is null || EndDate.Value.Year < 1912)
            throw new ArgumentException("請輸入有效的 C9 截止日期。");
        if (StartDate > EndDate)
            throw new ArgumentException("C9 起始日期不可晚於截止日期。");
        if (PageNumber < 1 || PageSize is not (10 or 30 or 50))
            throw new ArgumentException("C9 查詢或分頁條件不正確。");

        return new(StartDate.Value, EndDate.Value, PageNumber, PageSize);
    }

    public static string ToRoc(DateOnly value) => $"{value.Year - 1911:000}{value:MMdd}";

    public static C9ReportRequest Yesterday(TimeProvider timeProvider)
    {
        DateOnly yesterday = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime).AddDays(-1);
        return new(yesterday, yesterday);
    }
}

public sealed record C9ValidatedRequest(
    DateOnly StartDate,
    DateOnly EndDate,
    int PageNumber,
    int PageSize);

public sealed record C9SourceRow(
    string? VisitDate,
    string? MedicalRecordNo,
    string? PatientName,
    string? DoctorName,
    string? SectionName,
    string? ChargeUserId,
    string? ChargeCode,
    string? ChargeName,
    decimal? PayableAmountSource,
    decimal? DiscountAmountSource);

public sealed record C9ReportRow(
    string VisitDate,
    string MedicalRecordNo,
    string PatientName,
    string DoctorName,
    string SectionName,
    string ChargeUserId,
    string ChargeCode,
    string ChargeName,
    decimal DiscountAmount,
    decimal PayableAmount,
    decimal TotalAmount);
