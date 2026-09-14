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

            C11PatientCountRow? patientCount = await repository.QueryPeriodPatientCountAsync(
                source, startDate, endDate, period.RoomTypeName, cancellationToken);
            target.PeriodKey = period.Year;
            target.Within30DaysAmount = amount;
            target.Over30DaysAmount = LegacyC11NumberConverter.Subtract(target.OutstandingToEnd, amount);
            target.PeriodDebtAmount = amount;
            target.PeriodPatientCount = LegacyC11NumberConverter.ToSingle(patientCount?.PatientCount ?? 0m);
            target.RecoveredAmount = 0f;
            target.AdjustmentAmount = 0f;
        }

        IReadOnlyList<C11ReportGroup> groups = buffer
            .GroupBy(row => new { row.RoomType, row.RoomTypeName })
            .OrderBy(group => group.Key.RoomTypeName, StringComparer.Ordinal)
            .Select(group =>
            {
                List<C11ReportRow> rows = group.OrderBy(row => row.Year, StringComparer.Ordinal).ToList();
                return new C11ReportGroup
                {
                    RoomType = group.Key.RoomType,
                    RoomTypeName = group.Key.RoomTypeName,
                    Rows = rows,
                    Totals = Sum(rows)
                };
            }).ToList();

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

    private static C11ReportTotals Sum(IEnumerable<C11ReportRow> rows)
    {
        float total = 0, within = 0, patients = 0, debt = 0, recovered = 0, adjustment = 0;
        foreach (C11ReportRow row in rows)
        {
            total += row.OutstandingToEnd;
            within += row.Within30DaysAmount;
            patients += row.PeriodPatientCount;
            debt += row.PeriodDebtAmount;
            recovered += row.RecoveredAmount;
            adjustment += row.AdjustmentAmount;
        }
        return new C11ReportTotals
        {
            OutstandingToEnd = total,
            Within30DaysAmount = within,
            PeriodPatientCount = patients,
            PeriodDebtAmount = debt,
            RecoveredAmount = recovered,
            AdjustmentAmount = adjustment
        };
    }
}
