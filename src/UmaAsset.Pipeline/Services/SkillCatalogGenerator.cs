using System.Text.Json;

namespace UmaAsset.Pipeline.Services;

public static class SkillCatalogGenerator
{
    public static GeneratedSkillCatalog Build(
        IReadOnlyList<MasterSkillRecord> skillRecords,
        string rootDirectory)
    {
        var pathLookup = BuildPathLookup(rootDirectory);
        var catalog = new GeneratedSkillCatalog
        {
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            Skills = skillRecords
                .OrderBy(static skill => skill.SkillId)
                .Select(skill => new GeneratedSkillCatalogEntry
                {
                    SkillId = skill.SkillId,
                    IconId = skill.IconId,
                    NameJa = skill.NameJa,
                    TextureName = $"utx_ico_skill_{skill.IconId}",
                    RelativePath = pathLookup.GetValueOrDefault(skill.IconId),
                })
                .ToList(),
        };

        return catalog;
    }

    public static string Write(
        IReadOnlyList<MasterSkillRecord> skillRecords,
        string rootDirectory,
        string outputFile)
    {
        var catalog = Build(skillRecords, rootDirectory);
        var json = JsonSerializer.Serialize(catalog, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        var fullOutputPath = Path.GetFullPath(outputFile);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        File.WriteAllText(fullOutputPath, json);
        return fullOutputPath;
    }

    private static Dictionary<int, string> BuildPathLookup(string rootDirectory)
    {
        var lookup = new Dictionary<int, string>();
        var skillRoot = Path.Combine(rootDirectory, "skill-icons");
        if (!Directory.Exists(skillRoot))
        {
            return lookup;
        }

        foreach (var file in Directory.GetFiles(skillRoot, "*.png", SearchOption.AllDirectories))
        {
            var textureName = Path.GetFileNameWithoutExtension(file);
            var iconId = SkillIconPathParser.ParseIconId(textureName);
            if (!int.TryParse(iconId, out var parsedIconId))
            {
                continue;
            }

            lookup[parsedIconId] = Path.GetRelativePath(rootDirectory, file).Replace('\\', '/');
        }

        return lookup;
    }
}
