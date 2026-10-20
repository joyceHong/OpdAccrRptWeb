namespace OpdAccrRptWeb.Models;

public sealed class PermissionDeleteTargetRequest
{
    public int TargetType { get; set; }
    public string Target { get; set; } = string.Empty;
    public Dictionary<string, int> ExpectedMasks { get; set; } = [];
}
