namespace OpdAccrRptWeb.ViewModels;

public sealed class C11ReceivablesCollectionReportViewModel
{
    public string Title { get; init; } = string.Empty;
    public string StartDate { get; init; } = string.Empty;
    public string EndDate { get; init; } = string.Empty;
    public DateTimeOffset GeneratedAt { get; init; }
    public string GeneratedBy { get; init; } = string.Empty;
    public string ProgramNo { get; init; } = "C11";
    public string ReportNo { get; init; } = "C11";
    public IReadOnlyList<C11ReportGroup> Groups { get; init; } = [];
}

public sealed class C11ReportGroup
{
    public string RoomType { get; init; } = string.Empty;
    public string RoomTypeName { get; init; } = string.Empty;
    public IReadOnlyList<C11ReportRow> Rows { get; init; } = [];
    public C11ReportTotals Totals { get; init; } = new();
}

public sealed class C11ReportRow
{
    public string Year { get; set; } = string.Empty;
    public string PeriodKey { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string RoomTypeName { get; set; } = string.Empty;
    public float OutstandingToEnd { get; set; }
    public float Within30DaysAmount { get; set; }
    public float Over30DaysAmount { get; set; }
    public float PeriodDebtAmount { get; set; }
    public float PeriodPatientCount { get; set; }
    public float RecoveredAmount { get; set; }
    public float AdjustmentAmount { get; set; }
}

public sealed class C11ReportTotals
{
    public float OutstandingToEnd { get; init; }
    public float Within30DaysAmount { get; init; }
    public float PeriodPatientCount { get; init; }
    public float PeriodDebtAmount { get; init; }
    public float RecoveredAmount { get; init; }
    public float AdjustmentAmount { get; init; }
}
