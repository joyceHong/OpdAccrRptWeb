using System.Globalization;
using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public sealed class C16LegacyReducer : IC16LegacyReducer
{
    public IReadOnlyList<C16ReportRow> Reduce(IReadOnlyList<C16SourceRow> orderedRows, C16Source source)
    {
        var output = new List<C16ReportRow>();
        C16VisitKey? previous = null;
        C16ReportRow? current = null;
        foreach (C16SourceRow sourceRow in orderedRows.OrderBy(row => row.SourceOrdinal))
        {
            if (!IsSupportedOrder(sourceRow.OrderCode)) continue;
            C16VisitKey key = C16VisitKey.From(sourceRow);
            if (previous is null || key != previous.Value)
            {
                current = CreateRow(sourceRow, source, output.Count);
                output.Add(current);
            }
            ApplyMoney(current!, sourceRow);
            previous = key;
        }
        return output;
    }

    private static bool IsSupportedOrder(string? value)
    {
        string order = Trim(value);
        return order is "1-25-99" or "1-49-99" or "1-50-99"
            || order.Length == 5 && order.StartsWith("49-U", StringComparison.Ordinal);
    }

    private static C16ReportRow CreateRow(C16SourceRow row, C16Source source, int ordinal) => new()
    {
        EncounterOrdinal = ordinal,
        PatientName = Trim(row.PatientName),
        PatientId = Trim(row.PatientId),
        BirthDate = Trim(row.BirthDate),
        VisitDate = Trim(row.VisitDate),
        DischargeDate = source == C16Source.Inpatient ? Trim(row.DischargeDate) : string.Empty,
        Days = source == C16Source.Inpatient ? CalculateDays(row.VisitDate, row.DischargeDate) : 0,
        SectionName = source == C16Source.OutpatientEmergency ? Trim(row.SectionName) : string.Empty,
        Diagnosis = source == C16Source.Inpatient
            ? Trim(row.FirstInpatientDiagnosis) is { Length: > 0 } diagnosis ? diagnosis : Trim(row.BasicDiagnosis)
            : Trim(row.BasicDiagnosis),
        SubsidyType = Classify(Trim(row.PFin1), Trim(row.PFin2)),
        RoomType = source == C16Source.OutpatientEmergency && Trim(row.VisitRoom) == "0000" ? "E" : "R"
    };

    private static void ApplyMoney(C16ReportRow target, C16SourceRow row)
    {
        string order = Trim(row.OrderCode);
        switch (order)
        {
            case "1-25-99": target.Rl25 += row.Sub5 ?? 0m; break;
            case "1-49-99": target.Rl49 += row.Sub5 ?? 0m; break;
            case "1-50-99": target.Rl50 += row.Sub5 ?? 0m; break;
            default:
                if (order.Length == 5 && order.StartsWith("49-U", StringComparison.Ordinal))
                {
                    decimal amount = row.Sub2 ?? 0m;
                    target.Rl49 += amount;
                    target.Rl49Drug -= amount;
                }
                break;
        }
    }

    internal static string Classify(string pFin1, string pFin2) => pFin2 switch
    {
        "107" => string.Empty,
        "104" when pFin1 == "30" => "A",
        "105" when pFin1 == "30" => "B",
        "106" when pFin1 == "30" => "C",
        "137" when pFin1 == "30" => "F",
        "139" when pFin1 == "30" => "G",
        "104" => "D",
        "105" or "106" => "E",
        _ => "X"
    };

    private static int CalculateDays(string admission, string discharge)
    {
        if (!TryParseRoc(Trim(admission), out DateOnly start) || !TryParseRoc(Trim(discharge), out DateOnly end))
            return 0;
        return end.DayNumber - start.DayNumber;
    }

    private static bool TryParseRoc(string value, out DateOnly date)
    {
        date = default;
        if (value.Length < 7 || !int.TryParse(value[..3], out int year)) return false;
        return DateOnly.TryParseExact($"{year + 1911:0000}{value.Substring(3, 4)}", "yyyyMMdd",
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }

    private static string Trim(string? value) => value?.Trim() ?? string.Empty;
}
