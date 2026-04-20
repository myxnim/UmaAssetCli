using System.Text.Json;

namespace UmaAsset.Pipeline.Services;

public static class AssetManifestGenerator
{
    public static GeneratedAssetManifest Build(string rootDirectory)
    {
        var manifest = new GeneratedAssetManifest
        {
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
        };

        var characterRoot = Path.Combine(rootDirectory, "character");
        if (!Directory.Exists(characterRoot))
        {
            return manifest;
        }

        foreach (var file in Directory.GetFiles(characterRoot, "*.png", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(rootDirectory, file).Replace('\\', '/');
            var textureName = Path.GetFileNameWithoutExtension(file);
            var parsed = CharaIconPathParser.Parse(textureName);
            if (parsed is null)
            {
                continue;
            }

            if (!manifest.Characters.TryGetValue(parsed.CharacterId, out var character))
            {
                character = new GeneratedCharacterAssets
                {
                    CharacterId = parsed.CharacterId,
                };
                manifest.Characters[parsed.CharacterId] = character;
            }

            if (!character.Families.TryGetValue(parsed.Family, out var items))
            {
                items = [];
                character.Families[parsed.Family] = items;
            }

            items.Add(new GeneratedAssetItem
            {
                VariantId = parsed.VariantId,
                TextureName = parsed.TextureName,
                RelativePath = relativePath,
                FileName = Path.GetFileName(file),
            });
        }

        foreach (var character in manifest.Characters.Values)
        {
            foreach (var items in character.Families.Values)
            {
                items.Sort(static (a, b) =>
                {
                    var variantCompare = string.Compare(a.VariantId, b.VariantId, StringComparison.OrdinalIgnoreCase);
                    return variantCompare != 0
                        ? variantCompare
                        : string.Compare(a.TextureName, b.TextureName, StringComparison.OrdinalIgnoreCase);
                });
            }
        }

        return manifest;
    }

    public static string Write(string rootDirectory, string outputFile)
    {
        var manifest = Build(rootDirectory);
        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        var fullOutputPath = Path.GetFullPath(outputFile);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        File.WriteAllText(fullOutputPath, json);
        return fullOutputPath;
    }
}
