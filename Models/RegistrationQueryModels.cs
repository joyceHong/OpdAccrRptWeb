namespace OpdAccrRptWeb.Models;

public enum RegistrationQueryMode
{
    Registered,
    Cancelled,
    Unseen,
    Seen,
    Unpriced,
    Summary
}

public sealed record RegistrationQueryRequest(
    string? Mode,
    string? RegDate,
    string? MedicalRecordNo,
    string? PatientId,
    string? BirthDate,
    string? SectionNo,
    string? NewSectionNo,
    string? Room,
    string? Time,
    string? RegistrationNo,
    string? DoctorNo,
    int PageNumber = 1,
    int PageSize = 10);

public sealed record RegistrationQueryFilters(
    RegistrationQueryMode Mode,
    string RegDate,
    string MedicalRecordNo,
    string PatientId,
    string BirthDate,
    string SectionNo,
    string Room,
    string Time,
    int? RegistrationNo,
    string DoctorNo,
    IReadOnlyList<string> MedicalRecordNumbers)
{
    public bool HasAnyCondition =>
        RegDate.Length > 0 ||
        MedicalRecordNo.Length > 0 ||
        PatientId.Length > 0 ||
        BirthDate.Length > 0 ||
        SectionNo.Length > 0 ||
        Room.Length > 0 ||
        Time.Length > 0 ||
        RegistrationNo.HasValue ||
        DoctorNo.Length > 0;

    public bool IsSummary => Mode == RegistrationQueryMode.Summary;
}

public sealed record RegistrationQueryResult(
    string Mode,
    IReadOnlyList<RegistrationRow> Rows,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages,
    RegistrationSummary? Summary);

public sealed record RegistrationRow(
    string RegistrationKey,
    string RegistrationTypeLabel,
    string RegistrationDate,
    string RawRegistrationDate,
    string Time,
    int RegistrationNo,
    string Room,
    string MedicalRecordNo,
    string PatientName,
    string PatientId,
    string BirthDate,
    string Sex,
    string SectionCode,
    string SectionName,
    string DoctorCode,
    string DoctorName,
    string Financial1,
    string Financial2,
    string PaymentType,
    string Sequence,
    string RoomAddress,
    string CreatedBy,
    string CreatedDate,
    string CancelledBy,
    string CancelledDate,
    string StateLabel,
    string PricingLabel,
    string EmergencyReturnLabel,
    bool IsCancelled);

public sealed record RegistrationSummary(
    string RegistrationDate,
    string Time,
    string Room,
    int TotalCount,
    int SeenCount,
    int UnseenCount,
    int CancelledCount,
    IReadOnlyList<int> CancelledNumbers,
    int CurrentNumber,
    int PrebookNumber,
    bool RoomNumberDataMissing);

public sealed record RegistrationSource(
    string? RegistrationType,
    string? RegistrationDate,
    string? Time,
    int? RegistrationNo,
    string? Room,
    string? MedicalRecordNo,
    string? PatientName,
    string? PatientId,
    string? BirthDate,
    string? Sex,
    string? SectionCode,
    string? SectionName,
    string? DoctorCode,
    string? DoctorName,
    string? Financial1,
    string? Financial2,
    string? PaymentType,
    string? Sequence,
    string? RoomAddress,
    string? CreatedBy,
    string? CreatedDate,
    string? CancelledBy,
    string? CancelledDate,
    string? FirstVisitFlag,
    string? DigitalStatus,
    string? CancelledFlag,
    string? QuoteDisplayFlag,
    string? EmergencyReturnFlag);

public sealed record RegistrationSummarySource(
    int TotalCount,
    int SeenCount,
    int UnseenCount,
    IReadOnlyList<int> CancelledNumbers,
    int CurrentNumber,
    int PrebookNumber,
    bool RoomNumberDataMissing);

public sealed record RegistrationSectionOption(
    string LegacyCode,
    string NewCode,
    string Name);

public sealed record RegistrationDoctorOption(
    string Code,
    string Name);

public sealed class RegistrationQueryOptions
{
    public const string SectionName = "RegistrationQuery";

    public string? RegFlg { get; set; }

    public bool UsesZeroPaddedMedicalRecordNumber =>
        string.Equals(RegFlg?.Trim(), "0", StringComparison.Ordinal);
}
