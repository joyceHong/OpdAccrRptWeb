using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public sealed record C212Query(DateOnly EndDate);

public sealed class C212BoneBankBalanceReportViewModel
{
    [Description("日期")] public string AccountingDate { get; init; } = string.Empty;
    [Description("病歷號")] public string MedicalRecordNo { get; init; } = string.Empty;
    [Description("姓名")] public string PatientName { get; init; } = string.Empty;
    [Description("金額")] public decimal Amount { get; init; }
}

public enum C212DataStatus
{
    Unknown,
    Complete,
    Incomplete
}

public sealed record C212ReportRow(
    Repositories.C212RowKind Kind,
    string AccountingDateRoc,
    string MedicalRecordNo,
    string PatientName,
    decimal RawOracleAmount,
    decimal Amount);

public sealed record C212ReportResult(
    DateOnly AsOfDate,
    string MonthFirstDayRoc,
    IReadOnlyList<C212ReportRow> Rows,
    decimal TotalAmount,
    C212DataStatus DataStatus,
    DateTimeOffset AuditedAtUtc,
    string ReportProcessDateTimeRoc,
    string GeneratedBy,
    string CorrelationId);

public sealed record C212ReportSummary(
    string Title,
    string ReportDate,
    string ProgramNo,
    string ReportNo,
    string ProcessDateTime,
    string EditUser,
    decimal TotalAmount,
    C212DataStatus DataStatus,
    string DataStatusMessage,
    string CorrelationId);
