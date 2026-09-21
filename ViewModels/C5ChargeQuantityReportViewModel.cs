using System.ComponentModel;
using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C5ChargeQuantityReportViewModel
{
    [Description("診別")] public string RoomType { get; init; } = string.Empty;
    [Description("科別代碼")] public string SectionCode { get; init; } = string.Empty;
    [Description("科別名稱")] public string? SectionName { get; init; }
    [Description("批價碼")] public string ChargeCode { get; init; } = string.Empty;
    [Description("名稱")] public string ChargeName { get; init; } = string.Empty;
    [Description("日期")] public string ServiceDate { get; init; } = string.Empty;
    [Description("醫師代碼")] public string? DoctorId { get; init; }
    [Description("醫師姓名")] public string? DoctorName { get; init; }
    [Description("病歷號")] public string? MedicalRecordNo { get; init; }
    [Description("病患姓名")] public string? PatientName { get; init; }
    [Description("數量")] public decimal Quantity { get; init; }
    [Description("單價")] public decimal UnitPrice { get; init; }
    [Description("金額")] public decimal Amount { get; init; }
    public static C5ChargeQuantityReportViewModel From(C5ReportRow row) => new()
    {
        RoomType = row.RoomType, SectionCode = row.SectionCode, SectionName = row.SectionName,
        ChargeCode = row.ChargeCode, ChargeName = row.ChargeName, ServiceDate = row.ServiceDate,
        DoctorId = row.DoctorId, DoctorName = row.DoctorName, MedicalRecordNo = row.MedicalRecordNo,
        PatientName = row.PatientName, Quantity = row.Quantity, UnitPrice = row.UnitPrice, Amount = row.Amount
    };
}

public sealed record C5ReportResult(C5ValidatedRequest Request, IReadOnlyList<C5ReportRow> AllRows,
    ReportDataAndColumns<C5ChargeQuantityReportViewModel> Page, IReadOnlyList<C5QueryId> QueryIds);
