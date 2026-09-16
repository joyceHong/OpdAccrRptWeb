using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public sealed class C15LegacyReducer : IC15LegacyReducer
{
    public IReadOnlyList<C15WorkingRow> Reduce(IReadOnlyList<C15SourceRow> orderedRows)
    {
        var output = new List<C15WorkingRow>();
        C15VisitKey? previousKey = null;
        C15WorkingRow? current = null;

        foreach (C15SourceRow source in orderedRows)
        {
            C15VisitKey key = C15VisitKey.From(source);
            if (previousKey is null || key != previousKey.Value)
            {
                current = new C15WorkingRow { EncounterOrdinal = output.Count };
                output.Add(current);
            }

            current!.VisitDate = source.VisitDate.Trim();
            current.MedicalRecordNumber = source.MedicalRecordNumber.Trim();
            current.PatientName = source.PatientName.Trim();
            string returnDate = source.ReturnDate.Trim();
            current.ReturnDate = returnDate[..Math.Min(7, returnDate.Length)];

            string orderCode = source.OrderCode.Trim();
            switch (orderCode)
            {
                case "696-001":
                case "696-008":
                    current.Rl001 = source.Amount;
                    break;
                case "696-002":
                case "696-007":
                    current.Rl002 = source.Amount;
                    break;
                case "696-003":
                    current.Rl003 = source.Amount;
                    break;
                case "696-004":
                    current.Rl004 = source.Amount;
                    break;
                default:
                    throw new C15UnexpectedOrderCodeException();
            }

            current.Type = orderCode is "696-001" or "696-002" or "696-003" or "696-004"
                ? "1"
                : "2";
            previousKey = key;
        }

        return output;
    }
}

public sealed class C15UnexpectedOrderCodeException()
    : InvalidOperationException("C15 source row contains an unsupported order code.");
