namespace OpdAccrRptWeb.ViewModels;

public sealed record C22CashierCashPreviewViewModel(
    string StartDate,
    string EndDate,
    string EncounterSource,
    DateTimeOffset PrintedAt,
    string GeneratedBy,
    IReadOnlyList<CashierCashReportViewModel> Rows)
{
    public static decimal DueAmount(CashierCashReportViewModel row) =>
        row.CashAmount + row.SupplementaryCashAmount + row.DebitCardAmount
        + row.CheckAmount + row.SocialServiceAmount + row.VoucherAmount
        + row.MedicalDisputeSubsidyAmount + row.CreditCardAmount
        + row.StoredValueCardAmount + row.RevitalizationVoucherAmount
        + row.InsurancePaymentAmount;
}
