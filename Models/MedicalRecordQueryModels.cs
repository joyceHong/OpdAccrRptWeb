namespace OpdAccrRptWeb.Models;

public sealed record MedicalRecordQueryRequest(
    string? MedicalRecordNo,
    string? IdentityNumber,
    string? Name,
    string? Address1,
    string? Address2,
    string? NewIdentityNumber,
    string? NewName,
    string? NewBirthday,
    int PageNumber = 1,
    int PageSize = 10);

public sealed record MedicalRecordDetailRequest(string? MedicalRecordNo);

public sealed record MedicalRecordQueryFilters(
    string MedicalRecordNo,
    string IdentityNumber,
    string Name,
    string Address1,
    string Address2,
    string NewIdentityNumber,
    string NewName,
    string NewBirthday)
{
    public bool HasAnyCondition =>
        MedicalRecordNo.Length > 0 ||
        IdentityNumber.Length > 0 ||
        Name.Length > 0 ||
        Address1.Length > 0 ||
        Address2.Length > 0 ||
        NewIdentityNumber.Length > 0 ||
        NewName.Length > 0 ||
        NewBirthday.Length > 0;
}

public sealed record MedicalRecordQueryPage(
    IReadOnlyList<MedicalRecordRow> Rows,
    int TotalCount,
    int PageNumber,
    int PageSize,
    int TotalPages);

public sealed record MedicalRecordRow(
    string MedicalRecordNo,
    string IdentityNumber,
    string Name,
    string Birthday,
    string MaritalStatus,
    string Sex,
    string BloodType,
    string InsuranceType,
    string DiscountType,
    string HomePhone,
    string OfficePhone,
    string SecretLevel,
    string PatientCondition);

public sealed record MedicalRecordDetail(
    string MedicalRecordNo,
    string IdentityNumber,
    string Name,
    string Birthday,
    string MaritalStatus,
    string Sex,
    string BloodType,
    string InsuranceType,
    string DiscountType,
    string HomePhone,
    string OfficePhone,
    string SecretLevel,
    string PatientCondition,
    string ZipCode1,
    string Address1,
    string ZipCode2,
    string Address2,
    string BirthPlace,
    string Race,
    string Language,
    string NativePlace,
    string NewIdentityNumber,
    string NewName,
    string NewBirthday,
    string SpecialName,
    string OldMedicalRecordNo,
    string SocialServiceStatus,
    string FirstVisitDate,
    decimal DebtTotal,
    string SpouseId,
    string FatherId);

public sealed record MedicalRecordSource(
    string? MedicalRecordNo,
    string? IdentityNumber,
    string? Name,
    string? Birthday,
    string? MaritalStatus,
    string? Sex,
    string? BloodType,
    string? InsuranceType,
    string? DiscountType,
    string? HomePhone,
    string? OfficePhone,
    string? SecretLevel,
    string? PatientCondition);

public sealed record MedicalRecordDetailSource(
    string? MedicalRecordNo,
    string? IdentityNumber,
    string? Name,
    string? Birthday,
    string? MaritalStatus,
    string? Sex,
    string? BloodType,
    string? InsuranceType,
    string? DiscountType,
    string? HomePhone,
    string? OfficePhone,
    string? SecretLevel,
    string? PatientCondition,
    string? ZipCode1,
    string? Address1,
    string? ZipCode2,
    string? Address2,
    string? BirthPlace,
    string? Race,
    string? Language,
    string? NativePlace,
    string? NewIdentityNumber,
    string? NewName,
    string? NewBirthday,
    string? SpecialName,
    string? OldMedicalRecordNo,
    string? SocialServiceStatus,
    string? FirstVisitDate,
    decimal? MainDebt,
    string? SpouseId,
    string? FatherId);
