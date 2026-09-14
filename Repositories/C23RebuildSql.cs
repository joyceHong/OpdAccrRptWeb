using OpdAccrRptWeb.ViewModels;

namespace OpdAccrRptWeb.Repositories;

internal static class C23RebuildSql
{
    internal static IReadOnlyList<string> GetCommands(string source, string accountingDate)
    {
        var outpatient = source == C23EncounterSources.Outpatient;
        if (!outpatient && source != C23EncounterSources.Inpatient)
            throw new ArgumentException("C23 來源僅接受門急診或住院。", nameof(source));

        var prefix = outpatient ? "Opd" : "Ipd";
        var commands = new List<string>
        {
            $"DELETE FROM {prefix}RecRpt_PFin2SumDM1 WHERE chDateFlag BETWEEN :dateStart AND :dateEnd"
        };

        var generalThreshold = outpatient ? "0980901" : "0980801";
        if (string.CompareOrdinal(accountingDate, generalThreshold) >= 0)
        {
            commands.AddRange(C23SourceSql.GeneralBalanceCommandsFor(source));
        }

        if (string.CompareOrdinal(accountingDate, "0991101") >= 0)
        {
            commands.AddRange(C23SourceSql.Balance42CommandsFor(source));
        }
        return commands;
    }
}
