namespace OpdAccrRptWeb.Models;

public sealed record ReportPermissionGrant(
    string ReportCode,
    int TargetType,
    string Target,
    int ActionMask);

public sealed record ReportPermissionUser(string UserId, string Sector);
