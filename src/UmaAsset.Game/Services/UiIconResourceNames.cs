namespace UmaAsset.Game.Services;

public static class UiIconResourceNames
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> FriendlyNames =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["motivation"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["utx_ico_motivation_l_00"] = "AWFUL",
                ["utx_ico_motivation_l_01"] = "BAD",
                ["utx_ico_motivation_l_02"] = "NORMAL",
                ["utx_ico_motivation_l_03"] = "GOOD",
                ["utx_ico_motivation_l_04"] = "GREAT",
            },
            ["charastatus"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["utx_ico_charastatus_l_00"] = "SPEED",
                ["utx_ico_charastatus_l_01"] = "STAMINA",
                ["utx_ico_charastatus_l_02"] = "POWER",
                ["utx_ico_charastatus_l_03"] = "GUTS",
                ["utx_ico_charastatus_l_04"] = "WIT",
            },
        };

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> Families =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["motivation"] =
            [
                "utx_ico_motivation_l_00",
                "utx_ico_motivation_l_01",
                "utx_ico_motivation_l_02",
                "utx_ico_motivation_l_03",
                "utx_ico_motivation_l_04",
            ],
            ["charastatus"] =
            [
                "utx_ico_charastatus_l_00",
                "utx_ico_charastatus_l_01",
                "utx_ico_charastatus_l_02",
                "utx_ico_charastatus_l_03",
                "utx_ico_charastatus_l_04",
            ],
        };

    public static IReadOnlyList<string> ExpandFamilies(IEnumerable<string> families)
    {
        var requested = families
            .Where(static family => !string.IsNullOrWhiteSpace(family))
            .Select(static family => family.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requested.Length == 0)
        {
            return [];
        }

        var resources = new List<string>();
        foreach (var family in requested)
        {
            if (Families.TryGetValue(family, out var names))
            {
                resources.AddRange(names);
            }
        }

        return resources
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static IReadOnlyList<string> SupportedFamilies => Families.Keys.OrderBy(static key => key, StringComparer.OrdinalIgnoreCase).ToArray();

    public static IReadOnlyDictionary<string, string> GetFriendlyNames(string family)
    {
        return FriendlyNames.TryGetValue(family, out var names)
            ? names
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
