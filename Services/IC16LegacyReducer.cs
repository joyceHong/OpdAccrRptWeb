using OpdAccrRptWeb.Models;

namespace OpdAccrRptWeb.Services;

public interface IC16LegacyReducer
{
    IReadOnlyList<C16ReportRow> Reduce(IReadOnlyList<C16SourceRow> orderedRows, C16Source source);
}
