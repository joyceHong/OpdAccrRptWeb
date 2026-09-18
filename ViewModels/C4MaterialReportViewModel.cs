using System.ComponentModel;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C4MaterialReportViewModel
{
    [Description("序號")] public int Id { get; init; }
    [Description("類別")] public string Type { get; init; } = string.Empty;
    [Description("材料碼")] public string? MaterialCode { get; init; }
    [Description("健保碼")] public string? ClaimCode { get; init; }
    [Description("品名")] public string? OrderName { get; init; }
    [Description("單位")] public string? Unit { get; init; }
    [Description("批價碼")] public string? ChargeCode { get; init; }
    [Description("數量")] public decimal Total { get; init; }
    [Description("科別／部門")] public string? SectionCode { get; init; }

    public static C4MaterialReportViewModel From(C4MaterialReportRow row) => new()
    {
        Id = row.Id, Type = row.Type, MaterialCode = row.MaterialCode, ClaimCode = row.ClaimCode,
        OrderName = row.OrderName, Unit = row.Unit, ChargeCode = row.ChargeCode,
        Total = row.Total, SectionCode = row.SectionCode
    };
}

public sealed record C4MaterialReportResult(C4ValidatedRequest Request,
    IReadOnlyList<C4MaterialReportRow> AllRows,
    ReportDataAndColumns<C4MaterialReportViewModel> Page);

public sealed record C4MaterialPreviewViewModel(C4ReportMetadata Metadata,
    IReadOnlyList<C4MaterialReportRow> Rows);
