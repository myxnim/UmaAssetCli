namespace UmaAsset.Core.Models;

public sealed class GeneratedSupportAssetManifest
{
    public string GeneratedAtUtc { get; set; } = string.Empty;

    public Dictionary<string, GeneratedSupportAssets> Supports { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class GeneratedSupportAssets
{
    public string SupportId { get; set; } = string.Empty;

    public Dictionary<string, List<GeneratedAssetItem>> Families { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
