using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Services;

public sealed class C10AmountCalculationService : IC10AmountCalculationService
{
    private const string DebtAccount = "欠繳記帳";
    private const string Recovery = "應收帳款收回";
    private const string BasicCopayment = "基本部份負擔";
    private static readonly HashSet<string> Subsidies = new(StringComparer.Ordinal)
    {
        "醫療補助(社服)",
        "醫療糾紛補助",
        "醫療補助"
    };
    private static readonly HashSet<string> HiddenInpatientItems = new(StringComparer.Ordinal)
    {
        DebtAccount,
        Recovery,
        "醫療補助(社服)",
        "醫療糾紛補助",
        "醫療補助"
    };

    public IReadOnlyList<C10ReceivableDetailRow> Calculate(
        string source,
        C10RepositoryResult sourceData)
    {
        ArgumentNullException.ThrowIfNull(sourceData);
        var charges = sourceData.Charges.ToLookup(charge => charge.VisitKey);
        var output = new List<C10ReceivableDetailRow>();
        foreach (C10DebtVisit visit in sourceData.Visits)
        {
            output.AddRange(CalculateVisit(source, visit, charges[visit.VisitKey]));
        }
        return output;
    }

    public static long RoundLegacy(decimal value) => checked((long)Math.Round(
        value,
        0,
        MidpointRounding.AwayFromZero));

    private static IEnumerable<C10ReceivableDetailRow> CalculateVisit(
        string source,
        C10DebtVisit visit,
        IEnumerable<C10ChargeAggregate> charges)
    {
        var items = new List<DisplayItem>();
        long paid = 0;
        long due = 0;
        long discount = 0;
        long debt = RoundLegacy(visit.DebtAmount);

        foreach (C10ChargeAggregate charge in charges)
        {
            decimal sub6 = charge.Sub6 ?? 0m;
            decimal sub3 = charge.Sub3 ?? 0m;
            decimal sub1 = charge.Sub1 ?? 0m;
            decimal sub25 = charge.Sub25 ?? 0m;
            bool subsidy = Subsidies.Contains(charge.ChargeItemName);

            if (source == C10Sources.OpdEr)
            {
                if (sub6 != 0m)
                {
                    items.Add(new DisplayItem(charge.ChargeItemName, sub6, 0m));
                    due = checked(due + RoundLegacy(sub6));
                }
            }
            else if (ShouldDisplayInpatient(charge.ChargeItemName, sub6, sub3, sub1, sub25))
            {
                decimal insurance = charge.ChargeItemName == BasicCopayment ? 0m : sub25;
                items.Add(new DisplayItem(charge.ChargeItemName, sub1 + sub3, insurance));
                due = checked(due + RoundLegacy(sub1) + RoundLegacy(sub3));
            }

            paid = charge.ChargeItemName switch
            {
                DebtAccount => checked(paid - RoundLegacy(sub6)),
                _ when subsidy => checked(paid - RoundLegacy(sub1)),
                _ => checked(paid + RoundLegacy(sub1))
            };
            discount = subsidy
                ? checked(discount + RoundLegacy(sub3 + sub1))
                : checked(discount + RoundLegacy(sub3));
        }

        if (checked(debt - due + paid + discount) != 0)
        {
            items.Add(new DisplayItem("其他", checked(debt - due + paid), 0m));
            due = checked(debt + paid);
        }

        for (var index = 0; index < items.Count; index += 3)
        {
            DisplayItem? first = items.ElementAtOrDefault(index);
            DisplayItem? second = items.ElementAtOrDefault(index + 1);
            DisplayItem? third = items.ElementAtOrDefault(index + 2);
            yield return MapRow(visit, paid, due, discount, debt, first, second, third);
        }
    }

    private static bool ShouldDisplayInpatient(
        string name,
        decimal sub6,
        decimal sub3,
        decimal sub1,
        decimal sub25)
    {
        if (HiddenInpatientItems.Contains(name)) return false;
        if (sub6 == sub3 && sub3 == -sub1 && sub6 != 0m) return false;
        return sub1 != 0m || sub3 != 0m || sub25 != 0m;
    }

    private static C10ReceivableDetailRow MapRow(
        C10DebtVisit visit,
        long paid,
        long due,
        long discount,
        long debt,
        DisplayItem? first,
        DisplayItem? second,
        DisplayItem? third) => new()
    {
        VisitDate = visit.VisitKey.VisitDate,
        VisitTime = visit.VisitKey.VisitTime,
        VisitRoom = visit.VisitKey.VisitRoom,
        VisitNumber = visit.VisitKey.VisitNumber,
        RoomTypeName = visit.RoomType switch
        {
            "E" => "急診",
            "I" => "住院",
            _ => "門診"
        },
        DischargeDate = visit.DischargeDate,
        DepartmentName = visit.DepartmentName,
        MedicalRecordNumber = visit.MedicalRecordNumber,
        PatientName = visit.PatientName,
        AdmissionSequence = visit.AdmissionSequence,
        DoctorName = visit.DoctorName,
        HomePhone = visit.HomePhone,
        Address1 = visit.Address1,
        Address2 = visit.Address2,
        ContactName = visit.ContactName,
        ContactRelation = visit.ContactRelation,
        ContactPhone = visit.ContactPhone,
        PaidAmount = paid,
        AmountDue = due,
        DiscountAmount = discount,
        DebtAmount = debt,
        ItemName1 = first?.Name,
        SelfPayAmount1 = ToNullableAmount(first?.SelfPay),
        InsuranceAmount1 = ToNullableAmount(first?.Insurance),
        ItemName2 = second?.Name,
        SelfPayAmount2 = ToNullableAmount(second?.SelfPay),
        InsuranceAmount2 = ToNullableAmount(second?.Insurance),
        ItemName3 = third?.Name,
        SelfPayAmount3 = ToNullableAmount(third?.SelfPay),
        InsuranceAmount3 = ToNullableAmount(third?.Insurance)
    };

    private static long? ToNullableAmount(decimal? value) =>
        value.HasValue ? RoundLegacy(value.Value) : null;

    private sealed record DisplayItem(string Name, decimal SelfPay, decimal Insurance);
}
