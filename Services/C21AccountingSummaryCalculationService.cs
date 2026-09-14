using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C21AccountingSummaryCalculationService : IC21AccountingSummaryCalculationService
{
    private static readonly string[] IdentityCodes = ["01", "30", "35"];

    public IReadOnlyList<C21AccountingSummaryReportViewModel> Calculate(
        SearchReportCondition condition,
        IReadOnlyCollection<C21SourceAmount> sourceAmounts,
        IReadOnlyCollection<C21BillingItem> billingItems)
    {
        var groups = ResolveGroups(condition);
        var names = billingItems
            .GroupBy(item => NormalizeCode(item.Code), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First().Name, StringComparer.Ordinal);
        var result = new List<C21AccountingSummaryReportViewModel>();

        foreach (var group in groups)
        {
            var amounts = Aggregate(sourceAmounts, group.Rooms);
            AppendGroup(result, group.Code, group.Name, amounts, names, condition.EncounterSource == C21EncounterSources.Inpatient);
        }

        return result;
    }

    private static Dictionary<string, decimal[]> Aggregate(
        IEnumerable<C21SourceAmount> sourceAmounts,
        IReadOnlyCollection<int> rooms)
    {
        var result = new Dictionary<string, decimal[]>(StringComparer.Ordinal);
        foreach (var source in sourceAmounts.Where(row =>
                     int.TryParse(row.RoomType, out var roomType) && rooms.Contains(roomType)))
        {
            var code = NormalizeCode(source.BillingCode);
            var identityIndex = Array.IndexOf(IdentityCodes, source.IdentityCode);
            if (code.Length != 2 || identityIndex < 0)
            {
                continue;
            }

            if (!result.TryGetValue(code, out var values))
            {
                values = new decimal[3];
                result.Add(code, values);
            }
            values[identityIndex] += source.Amount;
        }
        return result;
    }

    private static void AppendGroup(
        ICollection<C21AccountingSummaryReportViewModel> result,
        string groupCode,
        string groupName,
        Dictionary<string, decimal[]> amounts,
        IReadOnlyDictionary<string, string> names,
        bool inpatient)
    {
        var rowOrder = 0;
        var sum1 = new decimal[3];
        var sum2 = new decimal[3];
        var sum3 = new decimal[3];
        var sum4 = new decimal[3];

        foreach (var code in amounts.Keys.OrderBy(ParseCode).ThenBy(code => code, StringComparer.Ordinal))
        {
            var raw = amounts[code];
            Accumulate(code, raw, inpatient, sum1, sum2, sum3, sum4);
            if (code == "75")
            {
                continue;
            }

            var displayed = code switch
            {
                "56" when inpatient => Add(amounts, "56", "57", "69", "59", "64"),
                "56" => Add(amounts, "56", "80"),
                "62" when inpatient => Add(amounts, "62", "51", "63"),
                _ => (decimal[])raw.Clone()
            };
            result.Add(Row(groupCode, groupName, ++rowOrder, "Detail", code,
                names.GetValueOrDefault(code, code), displayed));
        }

        if (amounts.TryGetValue("75", out var debt) && debt.Any(value => value != 0))
        {
            result.Add(Row(groupCode, groupName, ++rowOrder, "Detail", "75",
                names.GetValueOrDefault("75", "欠繳醫療費"), debt));
        }

        result.Add(Row(groupCode, groupName, ++rowOrder, "Blank", null, string.Empty, [0m, 0m, 0m]));

        result.Add(Row(groupCode, groupName, ++rowOrder, "Subtotal", null,
            groupCode == "5" ? "住院收入" : "合　　計", sum1));

        if (groupCode != "5")
        {
            var income = (decimal[])sum1.Clone();
            income[1] -= Get(amounts, inpatient ? "66" : "67")[1];
            result.Add(Row(groupCode, groupName, ++rowOrder, "Income", null,
                inpatient ? "沖轉應收帳款" : "門診收入", income));
        }

        var difference = new decimal[3];
        for (var index = 0; index < difference.Length; index++)
        {
            difference[index] = groupCode == "8" ? sum4[index] - sum3[index] : sum1[index] - sum2[index];
        }
        if (difference.Any(value => value != 0))
        {
            result.Add(Row(groupCode, groupName, ++rowOrder, "Difference", null, "差異數", difference));
        }
    }

    private static void Accumulate(
        string code,
        decimal[] value,
        bool inpatient,
        decimal[] sum1,
        decimal[] sum2,
        decimal[] sum3,
        decimal[] sum4)
    {
        var number = ParseCode(code);
        if (number is >= 1 and <= 50 && code != "49")
        {
            AddInto(sum1, value);
        }
        if (inpatient && code == "49")
        {
            sum2[1] += value[1];
            sum4[1] += value[1];
        }
        if (code is "51" or "56" or "61" or "75" or "63" or "62")
        {
            AddInto(sum2, value);
        }
        if (inpatient && code is "56" or "75")
        {
            SubtractFrom(sum2, value);
        }
        if (code is "66" or "67")
        {
            sum2[1] += value[1];
        }
        if (inpatient && code is "58" or "80" or "60" or "51" or "56" or "63" or "75" or "69")
        {
            AddInto(sum3, value);
        }
        if (!inpatient && code is "59" or "64" or "69")
        {
            SubtractFrom(sum2, value);
        }
        if (!inpatient && code == "60")
        {
            AddInto(sum2, value);
        }
        if (inpatient && code is "62" or "51" or "63")
        {
            AddInto(sum4, value);
        }
    }

    private static C21AccountingSummaryReportViewModel Row(
        string groupCode, string groupName, int order, string type,
        string? billingCode, string billingName, IReadOnlyList<decimal> values) => new()
        {
            GroupCode = groupCode,
            GroupName = groupName,
            RowOrder = order,
            RowType = type,
            BillingCode = billingCode,
            BillingName = billingName,
            SelfPayAmount = values[0],
            InsuranceAmount = values[1],
            InsuranceWithoutCardAmount = values[2]
        };

    private static decimal[] Add(IReadOnlyDictionary<string, decimal[]> amounts, params string[] codes)
    {
        var result = new decimal[3];
        foreach (var code in codes)
        {
            AddInto(result, Get(amounts, code));
        }
        return result;
    }

    private static decimal[] Get(IReadOnlyDictionary<string, decimal[]> amounts, string code) =>
        amounts.TryGetValue(code, out var value) ? value : [0m, 0m, 0m];

    private static void AddInto(decimal[] target, IReadOnlyList<decimal> source)
    {
        for (var index = 0; index < target.Length; index++) target[index] += source[index];
    }

    private static void SubtractFrom(decimal[] target, IReadOnlyList<decimal> source)
    {
        for (var index = 0; index < target.Length; index++) target[index] -= source[index];
    }

    private static string NormalizeCode(string value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim()[..Math.Min(2, value.Trim().Length)];

    private static int ParseCode(string code) => int.TryParse(code, out var value) ? value : int.MaxValue;

    private static IReadOnlyList<GroupDefinition> ResolveGroups(SearchReportCondition condition)
    {
        var scope = condition.AccountingScope ??
            (condition.EncounterSource == C21EncounterSources.Inpatient ? 4 : 0);
        return (condition.EncounterSource, scope) switch
        {
            (C21EncounterSources.Outpatient, 0) =>
            [new("1", "門急診合計", [0, 1]), new("2", "門診", [0]), new("3", "急診", [1])],
            (C21EncounterSources.Outpatient, 1) => [new("1", "門急診合計", [0, 1])],
            (C21EncounterSources.Outpatient, 2) => [new("2", "門診", [0])],
            (C21EncounterSources.Outpatient, 3) => [new("3", "急診", [1])],
            (C21EncounterSources.Inpatient, 4) =>
            [new("5", "住院總帳", [2, 3]), new("8", "出院總帳", [4, 5])],
            (C21EncounterSources.Inpatient, 5) => [new("5", "住院總帳", [2, 3])],
            (C21EncounterSources.Inpatient, 8) => [new("8", "出院總帳", [4, 5])],
            _ => throw new ArgumentException("C21 來源與帳務範圍不相容。")
        };
    }

    private sealed record GroupDefinition(string Code, string Name, IReadOnlyCollection<int> Rooms);
}
