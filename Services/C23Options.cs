namespace OpdAccrRptWeb.Services;

public sealed class C23Options
{
    public const string SectionName = "C23";

    public string? CurrentUserId { get; set; }

    public bool RebuildEnabled { get; set; }
}
