namespace OpdAccrRptWeb.Models;

public sealed class C141LineBotOptions
{
    public const string SectionName = "C141:LineBot";

    public string Endpoint { get; set; } =
        "https://webap.femh.org.tw:9015/EXTLINEBOTSERVICE/ExtLinebotWebService.asmx";
    public string SenderId { get; set; } = "BID2";
    public string PaymentTemplateNumber { get; set; } = "20200708000006";
    public string CardReminderTemplateNumber { get; set; } = "20200923000026";
    public int TimeoutSeconds { get; set; } = 20;
    public bool Enabled { get; set; }
}
