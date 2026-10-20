namespace OpdAccrRptWeb.Models;

[Flags]
public enum ReportAction
{
    None = 0,
    Query = 1,
    Excel = 2,
    Print = 4,
    Rebuild = 8
}
