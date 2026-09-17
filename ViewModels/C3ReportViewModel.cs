using System.ComponentModel;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.ViewModels;

public sealed class C3ReportViewModel
{
    [Description("診別")] public string Diagnose { get; init; } = string.Empty;
    [Description("領用部門")] public string? Dispensary { get; init; }
    [Description("部門代碼")] public string? Section { get; init; }
    [Description("批價碼")] public string ChargeCode { get; init; } = string.Empty;
    [Description("材料碼")] public string MaterialCode { get; init; } = string.Empty;
    [Description("材料名稱")] public string MaterialName { get; init; } = string.Empty;
    [Description("庫別")] public string InventoryType { get; init; } = string.Empty;
    [Description("數量")] public decimal TotalSum { get; init; }
    [Description("醫師代碼")] public string? DctNo { get; init; }
    [Description("病歷號")] public string? MrNo { get; init; }
    [Description("病患姓名")] public string? PName { get; init; }
    [Description("報表日")] public string? Op1Date { get; init; }
    [Description("醫師姓名")] public string? DrName { get; init; }
    [Description("自費")] public string? SPay { get; init; }

    public static C3ReportViewModel From(C3ReportRow row) => new()
    {
        Diagnose = row.Diagnose, Dispensary = row.Dispensary, Section = row.Section,
        ChargeCode = row.ChargeCode, MaterialCode = row.MaterialCode, MaterialName = row.MaterialName,
        InventoryType = row.InventoryType, TotalSum = row.TotalSum, DctNo = row.DctNo,
        MrNo = row.MrNo, PName = row.PName, Op1Date = row.Op1Date, DrName = row.DrName, SPay = row.SPay
    };
}

public sealed record C3ReportResult(C3ValidatedRequest Request, IReadOnlyList<C3ReportRow> AllRows,
    ReportDataAndColumns<C3ReportViewModel> Page);

public sealed record C3PreviewViewModel(string Title, string DateB, string DateE, string UserId,
    string Today, ReportDetailType DetailType, IReadOnlyList<C3ReportRow> Rows);
