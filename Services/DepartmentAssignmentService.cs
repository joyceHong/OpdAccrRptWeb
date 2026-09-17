using OpdAccrRptWeb.Models;
using OpdAccrRptWeb.Repositories;

namespace OpdAccrRptWeb.Services;

public sealed class DepartmentAssignmentService(ISectionMappingRepository mappings)
{
    private static readonly IReadOnlyDictionary<string, DepartmentAssignment> Systems =
        new Dictionary<string, DepartmentAssignment>(StringComparer.Ordinal)
        {
            ["56"] = A("十三G病房", "15137"), ["53"] = A("傷造口科", "15WD1"),
            ["52"] = A("胸腔內科", "0207"), ["51"] = A("牙科部", "0270"),
            ["39"] = A("復健科", "0450"), ["38"] = A("門診護理站_1", "15OPD"),
            ["46"] = A("門診護理站", "15012"), ["43"] = A("血液透析室3樓", "15HD3"),
            ["36"] = A("血液透析室4樓", "15HD4"), ["42"] = A("血液透析病房", "15015"),
            ["37"] = A("腹膜透析室", "1013P"), ["7"] = A("麻醉科", "12600"),
            ["5"] = A("手術室", "15030"), ["14"] = A("超音波暨內視鏡中心", "15710"),
            ["18"] = A("美容中心", "12120"), ["19"] = A("心導管室", "10511"),
            ["16"] = A("影像醫學科", "14010"), ["27"] = A("影像醫學科", "14011"),
            ["31"] = A("產房", "15014"), ["41"] = A("藥劑部", "12700")
        };

    public async Task<DepartmentAssignment> AssignAsync(CareSource source, C3MovementRow row,
        CancellationToken cancellationToken = default)
    {
        string room = row.Room.Trim(), station = row.Station.Trim(), system = row.SystemCode.Trim();
        if (source == CareSource.I) return await InpatientAsync(station, cancellationToken);
        string diagnose = room == "0000" ? "急診" : "門診";
        if (station.Length > 0)
        {
            SectionMapping stationMap = await mappings.TranslateAsync(station, diagnose == "急診" ? "E" : "", cancellationToken);
            return new(diagnose, stationMap.DisplayName, stationMap.NewCode);
        }
        if (diagnose == "急診")
        {
            if (Systems.TryGetValue(system, out DepartmentAssignment? sys)) return sys with { Diagnose = diagnose };
            return new(diagnose, "急診護理站", "15011");
        }
        if (system != "5" && Systems.TryGetValue(system, out DepartmentAssignment? outpatientSys))
            return outpatientSys with { Diagnose = diagnose };
        DepartmentAssignment? special = OutpatientSpecial(row);
        if (special is not null) return special with { Diagnose = diagnose };
        SectionMapping? location = await mappings.FindLocationAsync(row.SectionCode.Trim(), cancellationToken);
        if (location is null) return new(diagnose, string.Empty, string.Empty);
        SectionMapping translated = await mappings.TranslateAsync(location.NewCode, cancellationToken: cancellationToken);
        return new(diagnose, location.DisplayName, translated.NewCode.Length == 0 ? location.NewCode : translated.NewCode);
    }

    private async Task<DepartmentAssignment> InpatientAsync(string station, CancellationToken token)
    {
        SectionMapping? direct = await mappings.FindPlaceAsync(station, token)
            ?? await mappings.FindSectionAsync(station, token);
        if (direct is not null)
        {
            SectionMapping translated = await mappings.TranslateAsync(station, cancellationToken: token);
            return new("住院", direct.DisplayName, translated.NewCode);
        }
        if (IsSpecialStation(station))
        {
            SectionMapping special = await mappings.TranslateAsync(station, cancellationToken: token);
            return new("住院", special.DisplayName, special.NewCode);
        }
        return new("住院", "全院", "19999");
    }

    private static DepartmentAssignment? OutpatientSpecial(C3MovementRow row)
    {
        string sec = row.SectionCode.Trim(), room = row.Room.Trim(), system = row.SystemCode.Trim();
        if (sec == "0212*" && room.StartsWith("3J", StringComparison.Ordinal)) return A("血液透析室3樓", "15HD3");
        if (sec == "0212*" && room.StartsWith("4J", StringComparison.Ordinal)) return A("血液透析室4樓", "15HD4");
        if (sec == "0212*" && (room.StartsWith("5J", StringComparison.Ordinal) || room.StartsWith("5F", StringComparison.Ordinal))) return A("血液透析病房", "15015");
        if (sec == "0205A") return A("腹膜透析室", "1013P");
        if (room == "4F7") return A("產房", "15014");
        if (room == "20A") return A("耳鼻喉科", "12020");
        if (room == "3F1" || room.StartsWith("OP_", StringComparison.Ordinal)) return system == "7" ? A("麻醉科", "12600") : A("手術室", "15030");
        if (room.StartsWith("7F", StringComparison.Ordinal)) return A("超音波暨內視鏡中心", "15710");
        if (room.StartsWith("4F8", StringComparison.Ordinal) && room != "4F8") return A("美容中心", "12120");
        if (room.StartsWith("3F5", StringComparison.Ordinal)) return A("心導管室", "10511");
        if (string.CompareOrdinal(row.RunDate, "1061201") >= 0 && room != "0000"
            && row.ChargeCode is "DS0.5" or "DS1I" or "PN32G4BD" or "PN31GBD") return A("藥品調劑科", "12710");
        if (room == "5D103" || sec == "0283E") return A("傷造口科", "15WD1");
        return null;
    }

    private static bool IsSpecialStation(string station) => SectionMappingRepository.Constants.ContainsKey(station)
        || station.StartsWith("1OR", StringComparison.Ordinal)
        || station.StartsWith("1CV", StringComparison.Ordinal)
        || station.StartsWith("1XA", StringComparison.Ordinal);

    private static DepartmentAssignment A(string name, string code) => new(string.Empty, name, code);
}
