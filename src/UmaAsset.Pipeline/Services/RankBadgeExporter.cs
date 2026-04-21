namespace UmaAsset.Pipeline.Services;

public sealed class RankBadgeExporter
{
    private static readonly IReadOnlyList<string> BadgeLabels =
    [
        "G", "G+", "F", "F+", "E", "E+", "D", "D+", "C", "C+", "B", "B+", "A", "A+", "S", "S+", "SS", "SS+",
        .. ExpandFamily("UG", 9),
        .. ExpandFamily("UF", 9),
        .. ExpandFamily("UE", 9),
        .. ExpandFamily("UD", 9),
        .. ExpandFamily("UC", 9),
        .. ExpandFamily("UB", 9),
        .. ExpandFamily("UA", 9),
        .. ExpandFamily("US", 9),
    ];

    private readonly ManifestDatabase manifestDatabase;

    public RankBadgeExporter(ManifestDatabase manifestDatabase)
    {
        this.manifestDatabase = manifestDatabase;
    }

    public string Export(string outputDirectory, string atlasName = "rank_tex")
    {
        var atlasEntry = manifestDatabase.FindByName(atlasName)
            ?? throw new InvalidOperationException($"Could not find atlas entry '{atlasName}'.");

        var badgesDirectory = Path.Combine(outputDirectory, "ranks");
        Directory.CreateDirectory(badgesDirectory);

        var spriteMap = BuildSpriteNameMap();
        var results = new SpriteBundleExporter(manifestDatabase).ExportSprites(atlasEntry, badgesDirectory, spriteMap);
        var missing = spriteMap.Keys.Except(results.Select(static result => result.SpriteName), StringComparer.OrdinalIgnoreCase).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"Missing rank sprites: {string.Join(", ", missing)}");
        }

        return badgesDirectory;
    }

    private static IReadOnlyDictionary<string, string> BuildSpriteNameMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < BadgeLabels.Count; index++)
        {
            map[$"utx_txt_rank_{index:00}"] = BadgeLabels[index];
        }

        return map;
    }

    private static IEnumerable<string> ExpandFamily(string family, int suffixCount)
    {
        yield return family;
        for (var index = 1; index <= suffixCount; index++)
        {
            yield return $"{family}{index}";
        }
    }
}
