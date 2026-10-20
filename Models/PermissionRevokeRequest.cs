namespace OpdAccrRptWeb.Models;

public sealed class PermissionRevokeRequest
{
    public string ReportCode { get; set; } = string.Empty;
    public int TargetType { get; set; }
    public string Target { get; set; } = string.Empty;
    public int ExpectedMask { get; set; }
}
