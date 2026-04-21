using System.Text.Json;

namespace UmaAsset.Pipeline.Services;

public sealed class UiIconCatalogWriter
{
    public string Write(string outputRoot, IEnumerable<UiIconCatalogEntry> entries)
    {
        Directory.CreateDirectory(outputRoot);

        var payload = entries
            .OrderBy(static entry => entry.Family, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var path = Path.Combine(outputRoot, "ui-icon-catalog.json");
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
        };

        File.WriteAllText(path, JsonSerializer.Serialize(payload, options));
        return path;
    }
}
