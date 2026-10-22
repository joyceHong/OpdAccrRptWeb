namespace OpdAccrRptWeb.Models;

public sealed class PermissionGrantEditRequest
{
    public int TargetType { get; set; }
    public string Target { get; set; } = string.Empty;
    public string ReportCode { get; set; } = string.Empty;
    public bool Query { get; set; } = true;
    public bool Excel { get; set; } = true;
    public bool Print { get; set; } = true;
    public bool Rebuild { get; set; }
    public bool LineBot { get; set; }
}
