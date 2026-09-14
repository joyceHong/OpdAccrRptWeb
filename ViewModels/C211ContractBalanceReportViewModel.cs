using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C211ContractBalanceReportViewModel
{
    [Description("合約身分")] public string ContractCode { get; init; } = string.Empty;
    [Description("病歷號")] public string MedicalRecordNo { get; init; } = string.Empty;
    [Description("就診日期")] public string VisitDate { get; init; } = string.Empty;
    [Description("一般身分合約金額")] public decimal SelfAmount { get; init; }
    [Description("健保身分合約金額")] public decimal ClaimAmount { get; init; }
}

public sealed record C211ContractSubtotal(string ContractCode, decimal SelfAmount, decimal ClaimAmount);

public sealed record C211ReportSummary(
    string Title,
    string ReportDate,
    string ProgramNo,
    string ReportNo,
    string ProcessDateTime,
    string EditUser,
    IReadOnlyList<C211ContractSubtotal> Groups,
    decimal SelfGrandTotal,
    decimal ClaimGrandTotal);
