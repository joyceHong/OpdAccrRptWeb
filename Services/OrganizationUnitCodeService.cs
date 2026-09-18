using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed class OrganizationUnitCodeService(
    IOrganizationUnitMappingRepository repository) : IOrganizationUnitCodeService
{
    private static readonly IReadOnlyDictionary<string, string> EmergencyMappings =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["0201"] = "11910", ["0281"] = "11920", ["0220"] = "11930",
            ["0221"] = "11930", ["0230"] = "11309"
        };
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

    public async Task<OrganizationUnitMapping?> ResolveNewCodeAsync(
        string legacyCode, string roomType, OrganizationUnitMappingScope scope,
        CancellationToken cancellationToken = default)
    {
        string code = Normalize(legacyCode);
        if (code.Length == 0) return null;
        if (string.Equals(roomType.Trim(), "E", StringComparison.OrdinalIgnoreCase)
            && EmergencyMappings.TryGetValue(code, out string? emergencyCode))
            return new(OrganizationUnitSource.Fixed, code, emergencyCode, string.Empty, true);

        IReadOnlyList<OrganizationUnitMapping> candidates =
            await repository.FindByLegacyCodeAsync(code, scope, cancellationToken);
        string[] newCodes = candidates.Select(item => Normalize(item.NewCode))
            .Where(item => item.Length > 0).Distinct(StringComparer.Ordinal).ToArray();
        if (newCodes.Length > 1) throw new OrganizationUnitLegacyMappingAmbiguousException(code);
        return newCodes.Length == 0 ? null : candidates.First(item => Normalize(item.NewCode) == newCodes[0]);
    }

    public async Task<IReadOnlyList<OrganizationUnitMapping>> SearchAsync(
        string query, bool includeSections, bool includePlaces, bool activePlaceOnly, int limit = 20,
        CancellationToken cancellationToken = default)
    {
        if (!includeSections && !includePlaces) return [];
        if (limit is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(limit));
        string normalized = Normalize(query);
        if (normalized.Length == 0) return [];
        IReadOnlyList<OrganizationUnitMapping> values = await repository.SearchAsync(normalized,
            includeSections, includePlaces, activePlaceOnly, limit, cancellationToken);
        return values.GroupBy(item => Normalize(item.LegacyCode), StringComparer.Ordinal)
            .Select(group => group.OrderBy(item => item.Source == OrganizationUnitSource.Section ? 0 : 1).First())
            .Take(limit).ToArray();
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

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
