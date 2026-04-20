namespace UmaAsset.Core.Models;

public sealed class GeneratedAssetManifest
{
    public string GeneratedAtUtc { get; set; } = string.Empty;

    public Dictionary<string, GeneratedCharacterAssets> Characters { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class GeneratedCharacterAssets
{
    public string CharacterId { get; set; } = string.Empty;

    public Dictionary<string, List<GeneratedAssetItem>> Families { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class GeneratedAssetItem
{
    public string VariantId { get; set; } = string.Empty;

    public string TextureName { get; set; } = string.Empty;

    public string RelativePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;
}
