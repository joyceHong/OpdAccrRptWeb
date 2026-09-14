using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C21AccountingSummaryReportViewModel
{
    [Description("群組代碼")] public string GroupCode { get; set; } = string.Empty;
    [Description("群組名稱")] public string GroupName { get; set; } = string.Empty;
    [Description("列序")] public int RowOrder { get; set; }
    [Description("列類型")] public string RowType { get; set; } = string.Empty;
    [Description("科目")] public string? BillingCode { get; set; }
    [Description("收費名稱")] public string BillingName { get; set; } = string.Empty;
    [Description("民眾")] public decimal SelfPayAmount { get; set; }
    [Description("健保")] public decimal InsuranceAmount { get; set; }
    [Description("健保未帶卡")] public decimal InsuranceWithoutCardAmount { get; set; }
}

public sealed class C21SourceAmount
{
    public C21SourceAmount()
    {
    }

    public C21SourceAmount(int roomType, string billingCode, string identityCode, decimal amount)
    {
        RoomType = roomType.ToString(System.Globalization.CultureInfo.InvariantCulture);
        BillingCode = billingCode;
        IdentityCode = identityCode;
        Amount = amount;
    }

    public string RoomType { get; set; } = string.Empty;
    public string BillingCode { get; set; } = string.Empty;
    public string IdentityCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public sealed record C21BillingItem(string Code, string Name);
