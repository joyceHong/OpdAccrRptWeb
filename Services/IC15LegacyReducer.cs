using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public interface IC15LegacyReducer
{
    IReadOnlyList<C15WorkingRow> Reduce(IReadOnlyList<C15SourceRow> orderedRows);
}
