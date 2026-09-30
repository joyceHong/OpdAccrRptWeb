namespace OpdAccrRptWeb.Models;

public sealed record OpdPriceVisitRequest(string? MedicalRecordNo, DateOnly? VisitDate,
    string? SectionCode, bool ShowDc = false, bool ShowExtendedCode = false,
    int PageNumber = 1, int PageSize = 10);
public sealed record OpdPriceDetailRequest(string? VisitToken, bool ShowDc = false,
    bool ShowExtendedCode = false);
public sealed record OpdPriceVisitKey(string VisitDate, string VisitTime, string Room,
    decimal RegistrationNo, string MedicalRecordNo);
public sealed record OpdPriceReceiptKey(string VisitDate, string VisitTime, string Room,
    decimal RegistrationNo, decimal ReceiptSequence, string MedicalRecordNo);
public sealed record OpdPriceVisitSource(OpdPriceVisitKey Key, string SectionCode,
    string InsuranceSequence, bool IsCancelled, string DoctorName, string PatientName);
public sealed record OpdPriceVisitRow(string VisitDate, string VisitTime, string Room,
    decimal RegistrationNo, string SectionCode, string InsuranceSequence,
    string CancellationLabel, string VisitToken);
public sealed record OpdPriceVisitPage(IReadOnlyList<OpdPriceVisitRow> Rows, int TotalCount,
    int PageNumber, int PageSize, int TotalPages);
public sealed record OpdPricePatient(string MedicalRecordNo, string Name, string Birthday,
    string IdentityNumber);
public sealed record OpdPriceEncounter(string VisitDate, string VisitTime, string Room,
    decimal RegistrationNo, string SectionCode, string DoctorName, string IdentityType,
    string DiscountType, string InsuranceSequence, string Copayment,
    IReadOnlyList<string> Diagnoses);
public sealed record OpdPriceChargeSource(bool IsDrug, string Code, string ExtendedCode,
    string Name, decimal Quantity, decimal DaysOrPercent, string SelfPayCode,
    decimal InsurancePrice, decimal SelfPayPrice, decimal InsuranceAmount,
    decimal SelfPayAmount, string Status, string Project, string InputUser,
    string PriceUser, string DeleteUser, IReadOnlyList<decimal> SubAmounts,
    string PrescriptionDate, string ReceiptSequence);
public sealed record OpdPriceChargeRow(string Category, string Code, string Name,
    decimal Quantity, decimal DaysOrPercent, string PaymentLabel, decimal UnitPrice,
    decimal Amount, string Status, string InputUser, string PriceUser, string DeleteUser,
    IReadOnlyList<decimal> SubAmounts, string PrescriptionDate, string ReceiptSequence);
public sealed record OpdPriceReceiptSource(OpdPriceReceiptKey Key, string ReceiptNo,
    string CheckoutDate, string CheckoutUser, decimal Receivable, decimal Cash,
    decimal Check, decimal Card, decimal OnAccount, decimal SocialService,
    string DcDate, string DcUser, string Status);
public sealed record OpdPriceReceiptRow(string ReceiptNo, string CheckoutDate,
    string CheckoutUser, decimal Receivable, decimal Cash, decimal Check, decimal Card,
    decimal OnAccount, decimal SocialService, string DcDate, string DcUser,
    string? ReceiptToken);
public sealed record OpdPriceDetail(OpdPricePatient Patient, OpdPriceEncounter Encounter,
    IReadOnlyList<OpdPriceChargeRow> Charges, IReadOnlyList<OpdPriceReceiptRow> Receipts);
public sealed record OpdPriceSectionOption(string Code, string Name);

public sealed record OpdReceiptHeader(string ReceiptNo, string MedicalRecordNo,
    string PatientName, string IdentityNumber, string VisitDate, string SectionName,
    string IdentityName, string DiscountName, string InsuranceSequence,
    string DoctorName, string DoctorNo, string CopaymentName);
public sealed record OpdReceiptChargeAggregate(string ChargeCode, decimal Sub1,
    decimal Sub3, decimal Sub5, decimal InsuranceAmount, decimal SelfPayAmount);
public sealed record OpdReceiptItem(string Code, string Name, decimal InsuranceAmount,
    decimal SelfPayAmount);
public sealed record OpdReceiptPreview(OpdReceiptHeader Header,
    IReadOnlyList<IReadOnlyList<OpdReceiptItem>> ItemRows, decimal Total,
    decimal Insurance, decimal SelfPay, decimal Discount, decimal Subsidy,
    decimal Collected, decimal Balance, string ChineseCollected, string RoomType);
