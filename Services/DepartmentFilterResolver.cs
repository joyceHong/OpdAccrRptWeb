using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed class DepartmentFilterResolver(ISectionMappingRepository mappings)
{
    private static readonly IReadOnlyDictionary<string, DepartmentFilterMode> Fixed =
        new Dictionary<string, DepartmentFilterMode>(StringComparer.Ordinal)
        {
            ["15137"] = DepartmentFilterMode.Sys56, ["15WD1"] = DepartmentFilterMode.Sys53Wound,
            ["0207"] = DepartmentFilterMode.Sys52, ["0270"] = DepartmentFilterMode.Sys51,
            ["0450"] = DepartmentFilterMode.Sys39, ["15OPD"] = DepartmentFilterMode.Sys38,
            ["15HD3"] = DepartmentFilterMode.Hd3, ["15HD4"] = DepartmentFilterMode.Hd4,
            ["0552"] = DepartmentFilterMode.Hd5, ["1013P"] = DepartmentFilterMode.Pd,
            ["0511"] = DepartmentFilterMode.Er, ["0330"] = DepartmentFilterMode.Anesthesia,
            ["0532"] = DepartmentFilterMode.OperatingRoom, ["0520"] = DepartmentFilterMode.Endoscopy,
            ["0296"] = DepartmentFilterMode.Beauty, ["0545"] = DepartmentFilterMode.Cath,
            ["0340"] = DepartmentFilterMode.Radiology, ["0340A"] = DepartmentFilterMode.RadiologyTechnology,
            ["14011"] = DepartmentFilterMode.RadiologyTechnology, ["0250"] = DepartmentFilterMode.Ent,
            ["0542"] = DepartmentFilterMode.Delivery, ["0410"] = DepartmentFilterMode.Pharmacy,
            ["12710"] = DepartmentFilterMode.Dispensing, ["0512"] = DepartmentFilterMode.GeneralLocation
        };

    public static string? NormalizeDepartmentCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        string value = code.Trim().ToUpperInvariant();
        return value;
    }

    public async Task<DepartmentFilterMode> ResolveAsync(string? code,
        CancellationToken cancellationToken = default)
    {
        string? normalized = NormalizeDepartmentCode(code);
        if (normalized is null) return DepartmentFilterMode.None;
        string value = normalized;
        if (value == "0575" || value == "1OR4F" || IsOperatingRoomStation(value))
            return DepartmentFilterMode.Station;
        if (Fixed.TryGetValue(value, out DepartmentFilterMode mode)) return mode;
        if (await mappings.LocationExistsAsync(value, cancellationToken))
            return DepartmentFilterMode.GeneralLocation;
        throw new ArgumentException("查無此科別。");
    }

    private static bool IsOperatingRoomStation(string value) => value.Length == 5
        && value.StartsWith("1OR", StringComparison.Ordinal)
        && int.TryParse(value.AsSpan(3), out int number) && number is >= 1 and <= 27;
}
