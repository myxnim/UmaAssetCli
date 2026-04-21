using System.Text.Json;

namespace UmaAsset.Pipeline.Services;

public sealed class SplitUiIconCatalogWriter
{
    public string Write(string catalogsRoot, string catalogName, IEnumerable<UiIconCatalogEntry> entries)
    {
        Directory.CreateDirectory(catalogsRoot);

        var payload = entries
            .OrderBy(static entry => entry.Family, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var path = Path.Combine(catalogsRoot, $"{catalogName}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
        }));

        return path;
    }
}
