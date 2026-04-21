using System.Text.Json;

namespace UmaAsset.Pipeline.Services;

public static class SupportAssetManifestGenerator
{
    public static GeneratedSupportAssetManifest Build(string rootDirectory)
    {
        var manifest = new GeneratedSupportAssetManifest
        {
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
        };

        var supportRoot = Path.Combine(rootDirectory, "support");
        if (!Directory.Exists(supportRoot))
        {
            return manifest;
        }

        foreach (var file in Directory.GetFiles(supportRoot, "*.png", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(rootDirectory, file).Replace('\\', '/');
            var textureName = Path.GetFileNameWithoutExtension(file);
            var parsed = SupportIconPathParser.Parse(textureName);
            if (parsed is null)
            {
                continue;
            }

            if (!manifest.Supports.TryGetValue(parsed.SupportId, out var support))
            {
                support = new GeneratedSupportAssets
                {
                    SupportId = parsed.SupportId,
                };
                manifest.Supports[parsed.SupportId] = support;
            }

            if (!support.Families.TryGetValue(parsed.Family, out var items))
            {
                items = [];
                support.Families[parsed.Family] = items;
            }

            items.Add(new GeneratedAssetItem
            {
                VariantId = parsed.SupportId,
                TextureName = textureName,
                RelativePath = relativePath,
                FileName = Path.GetFileName(file),
            });
        }

        foreach (var support in manifest.Supports.Values)
        {
            foreach (var items in support.Families.Values)
            {
                items.Sort(static (a, b) => string.Compare(a.TextureName, b.TextureName, StringComparison.OrdinalIgnoreCase));
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
