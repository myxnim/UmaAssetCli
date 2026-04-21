using System.Text.Json;

namespace UmaAsset.Pipeline.Services;

public sealed class RawAtlasIndexWriter
{
    public string Write(string regionRoot, IEnumerable<RawAtlasIndexEntry> entries)
    {
        var catalogsRoot = Path.Combine(regionRoot, "catalogs");
        Directory.CreateDirectory(catalogsRoot);

        var path = Path.Combine(catalogsRoot, "raw-atlas-index.json");
        var payload = entries
            .OrderBy(static entry => entry.Atlas, StringComparer.OrdinalIgnoreCase)
            .ThenBy(static entry => entry.SpriteName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        File.WriteAllText(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            WriteIndented = true,
        }));

        return path;
    }
}
