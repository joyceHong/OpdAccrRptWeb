using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed class OrganizationUnitCodeService(
    IOrganizationUnitMappingRepository repository) : IOrganizationUnitCodeService
{
    private static readonly IReadOnlyDictionary<string, OrganizationUnitMapping> FixedMappings =
        BuildFixedMappings();

    public async Task<OrganizationUnitMapping?> ResolveLegacyCodeAsync(
        string newCode,
        bool activePlaceOnly,
        CancellationToken cancellationToken = default)
    {
        string code = newCode.Trim().ToUpperInvariant();
        if (code.Length == 0) return null;

        IReadOnlyList<OrganizationUnitMapping> databaseMappings =
            await repository.FindByNewCodeAsync(code, activePlaceOnly, cancellationToken);
        var candidates = databaseMappings
            .Where(mapping => !string.IsNullOrWhiteSpace(mapping.LegacyCode))
            .ToList();
        if (FixedMappings.TryGetValue(code, out OrganizationUnitMapping? fixedMapping))
            candidates.Add(fixedMapping);

        string[] legacyCodes = candidates.Select(mapping => mapping.LegacyCode)
            .Distinct(StringComparer.Ordinal).ToArray();
        if (legacyCodes.Length > 1)
            throw new OrganizationUnitMappingAmbiguousException(code);
        return legacyCodes.Length == 0
            ? null
            : candidates.First(mapping => mapping.LegacyCode == legacyCodes[0]);
    }

    private static IReadOnlyDictionary<string, OrganizationUnitMapping> BuildFixedMappings()
    {
        var values = new Dictionary<string, OrganizationUnitMapping>(StringComparer.Ordinal);
        void Add(string code, string name) => values[code] =
            new(OrganizationUnitSource.Fixed, code, code, name, true);

        Add("15HD3", "血液透析室3樓"); Add("15HD4", "血液透析室4樓");
        Add("1013P", "腹膜透析室"); Add("150MI", "內科ICU-10床");
        Add("15OPD", "門診護理站_1"); Add("1OR4F", "四樓手術室");
        Add("12710", "藥品調劑科"); Add("1510M", "骨髓移植病房");
        Add("15WD1", "傷造口科");
        for (int i = 1; i <= 27; i++) Add($"1OR{i:00}", $"三樓手術室_{i:00}");
        for (int i = 1; i <= 4; i++)
        {
            Add($"1CV{i:00}", $"心導管室-{i:00}");
            Add($"1XA{i:00}", $"影像醫學科-放射組{i:00}");
        }
        for (int i = 1; i <= 20; i++) Add($"1ED{i:00}", $"1ED{i:00}");
        return values;
    }
}
