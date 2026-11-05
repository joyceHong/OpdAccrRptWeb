using OpdAccrRptWeb.Repositories;
using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C11ReceivablesCollectionReportService(
    IC11ReceivablesCollectionRepository repository,
    TimeProvider timeProvider) : IC11ReceivablesCollectionReportService
{
    public async Task<C11ReceivablesCollectionReportViewModel> CreateAsync(
        SearchReportCondition condition,
        string generatedBy,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string source = condition.Source ?? C10Sources.OpdEr;
        string startDate = condition.StartDate?.Trim() ?? string.Empty;
        string endDate = condition.EndDate?.Trim() ?? string.Empty;

        IReadOnlyList<C11AggregateRow> baseRows =
            await repository.QueryOutstandingToEndAsync(source, endDate, cancellationToken);
        var buffer = baseRows.Select(CreateBaseRow).ToList();
        IReadOnlyList<C11AggregateRow> periodRows =
            await repository.QueryPeriodOutstandingAsync(source, startDate, endDate, cancellationToken);

        string lookupYear = startDate[..Math.Min(3, startDate.Length)];
        foreach (C11AggregateRow period in periodRows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            C11ReportRow? target = buffer.FirstOrDefault(row =>
                row.Year == lookupYear && row.RoomTypeName == period.RoomTypeName);
            float amount = LegacyC11NumberConverter.ToSingle(period.OutstandingAmount);
            if (target is null)
            {
                target = new C11ReportRow
                {
                    Year = period.Year,
                    RoomType = period.RoomType,
                    RoomTypeName = period.RoomTypeName,
                    OutstandingToEnd = amount
                };
                buffer.Add(target);
            }

            target.PeriodKey = period.Year;
            target.Within30DaysAmount = amount;
            target.Over30DaysAmount = LegacyC11NumberConverter.Subtract(target.OutstandingToEnd, amount);
            target.PeriodDebtAmount = amount;
            target.RecoveredAmount = 0f;
            target.AdjustmentAmount = 0f;
        }

        var groups = new List<C11ReportGroup>();
        foreach (var group in buffer.GroupBy(row => new { row.RoomType, row.RoomTypeName })
                     .OrderBy(group => group.Key.RoomTypeName, StringComparer.Ordinal))
        {
            List<C11ReportRow> rows = group.OrderBy(row => row.Year, StringComparer.Ordinal).ToList();
            List<C11AggregateRow> groupPeriodRows = periodRows
                .Where(row => row.RoomType == group.Key.RoomType && row.RoomTypeName == group.Key.RoomTypeName)
                .ToList();
            float periodAmount = 0f;
            foreach (C11AggregateRow row in groupPeriodRows)
                periodAmount += LegacyC11NumberConverter.ToSingle(row.OutstandingAmount);

            float patientCount = 0f;
            if (groupPeriodRows.Count > 0)
            {
                C11PatientCountRow? count = await repository.QueryPeriodPatientCountAsync(
                    source, startDate, endDate, group.Key.RoomTypeName, cancellationToken);
                patientCount = LegacyC11NumberConverter.ToSingle(count?.PatientCount ?? 0m);
                C11ReportRow? periodRow = rows.LastOrDefault(row => row.PeriodKey == groupPeriodRows[^1].Year);
                if (periodRow is not null) periodRow.PeriodPatientCount = patientCount;
            }

            groups.Add(new C11ReportGroup
            {
                RoomType = group.Key.RoomType,
                RoomTypeName = group.Key.RoomTypeName,
                Rows = rows,
                Totals = Sum(rows, patientCount, periodAmount)
            });
        }

        return new C11ReceivablesCollectionReportViewModel
        {
            Title = source == C10Sources.Inpatient
                ? "住院應收帳款催收款月報表"
                : "門急診應收帳款催收款月報表",
            StartDate = startDate,
            EndDate = endDate,
            GeneratedAt = timeProvider.GetLocalNow(),
            GeneratedBy = generatedBy,
            ProgramNo = "OpdAccRpt-06",
            ReportNo = "ReportOpdAcc-06",
            Groups = groups
        };
    }

    private static C11ReportRow CreateBaseRow(C11AggregateRow source)
    {
        float total = LegacyC11NumberConverter.ToSingle(source.OutstandingAmount);
        return new C11ReportRow
        {
            Year = source.Year,
            PeriodKey = source.Year,
            RoomType = source.RoomType,
            RoomTypeName = source.RoomTypeName,
            OutstandingToEnd = total,
            Over30DaysAmount = total
        };
    }

    private static C11ReportTotals Sum(IEnumerable<C11ReportRow> rows, float patientCount, float periodAmount)
    {
        float total = 0, within = 0, recovered = 0, adjustment = 0;
        foreach (C11ReportRow row in rows)
        {
            total += row.OutstandingToEnd;
            within += row.Within30DaysAmount;
            recovered += row.RecoveredAmount;
            adjustment += row.AdjustmentAmount;
        }
        return new C11ReportTotals
        {
            OutstandingToEnd = total,
            Within30DaysAmount = within,
            PeriodPatientCount = patientCount,
            PeriodDebtAmount = periodAmount,
            RecoveredAmount = recovered,
            AdjustmentAmount = adjustment
        };
    }
}
