using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class CashierCashReportViewModel
{
    [Description("日期")] public string? CashierDate { get; set; }
    [Description("櫃員代碼")] public string? CashierUserId { get; set; }
    [Description("櫃員姓名")] public string? CashierUserName { get; set; }
    [Description("診別／櫃別")] public string? CounterName { get; set; }
    [Description("現金收入")] public decimal CashAmount { get; set; }
    [Description("補繳現金")] public decimal SupplementaryCashAmount { get; set; }
    [Description("支票")] public decimal CheckAmount { get; set; }
    [Description("社服補助")] public decimal SocialServiceAmount { get; set; }
    [Description("金融卡")] public decimal DebitCardAmount { get; set; }
    [Description("醫療糾紛補助")] public decimal MedicalDisputeSubsidyAmount { get; set; }
    [Description("信用卡（含醫指付）")] public decimal CreditCardAmount { get; set; }
    [Description("消費券")] public decimal VoucherAmount { get; set; }
    [Description("儲值卡（HappyCash）")] public decimal StoredValueCardAmount { get; set; }
    [Description("振興券")] public decimal RevitalizationVoucherAmount { get; set; }
    [Description("保險支付")] public decimal InsurancePaymentAmount { get; set; }
    [Description("保險公司")] public string? InsurancePaymentNote { get; set; }
    [Description("病人銀行帳戶扣款")] public decimal PatientBankDebitAmount { get; set; }
}

public static class CashierCashSortTypes
{
    public const string Cashier = "Cashier";
    public const string Encounter = "Encounter";
    public static bool IsSupported(string? value) => value is Cashier or Encounter;
}
