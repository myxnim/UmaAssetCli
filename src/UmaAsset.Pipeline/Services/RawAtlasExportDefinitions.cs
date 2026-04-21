namespace UmaAsset.Pipeline.Services;

public static class RawAtlasExportDefinitions
{
    private static readonly string[] DefaultPresetNames = ["extras", "honor", "scenario"];

    private static readonly IReadOnlyDictionary<string, string[]> PresetAtlasNames =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["extras"] = ["photostudio_tex", "schedulebook_tex", "shop_tex"],
            ["honor"] = ["atlas_honor_common_tex", "atlas_honor_740000_tex", "atlas_honor_750000_tex"],
        };

    public static IReadOnlyList<string> SupportedPresets => DefaultPresetNames;

    public static IReadOnlyList<string> ResolveAtlasNames(
        ManifestDatabase manifest,
        IEnumerable<string>? requestedPresets = null,
        IEnumerable<string>? explicitAtlasNames = null)
    {
        var atlasNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var presets = requestedPresets?
            .Where(static preset => !string.IsNullOrWhiteSpace(preset))
            .Select(static preset => preset.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        if (presets.Length == 0)
        {
            presets = DefaultPresetNames;
        }

        foreach (var preset in presets)
        {
            if (string.Equals(preset, "scenario", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var entry in manifest.SearchBySubstring(["singlemodescenario"], 1000)
                             .Where(static entry => entry.BaseName.EndsWith("_tex", StringComparison.OrdinalIgnoreCase)))
                {
                    atlasNames.Add(entry.BaseName);
                }

                continue;
            }

            if (!PresetAtlasNames.TryGetValue(preset, out var names))
            {
                throw new ArgumentException(
                    $"Unsupported raw atlas preset '{preset}'. Supported: {string.Join(", ", SupportedPresets)}");
            }

            foreach (var name in names)
            {
                if (manifest.FindByName(name) is not null)
                {
                    atlasNames.Add(name);
                }
            }
        }

        if (explicitAtlasNames is not null)
        {
            foreach (var atlasName in explicitAtlasNames
                         .Where(static name => !string.IsNullOrWhiteSpace(name))
                         .Select(static name => name.Trim()))
            {
                atlasNames.Add(atlasName);
            }
        }

        return atlasNames
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
