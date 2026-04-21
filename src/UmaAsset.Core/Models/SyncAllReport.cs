namespace UmaAsset.Core.Models;

public sealed class SyncAllReport
{
    public string GeneratedAtUtc { get; set; } = string.Empty;

    public List<SyncAllRegionReport> Regions { get; set; } = [];

    public SyncAllCrossRegionReport? CrossRegion { get; set; }
}

public sealed class SyncAllRegionReport
{
    public string Region { get; set; } = string.Empty;

    public string UmaDir { get; set; } = string.Empty;

    public int CharacterCount { get; set; }

    public int SupportCount { get; set; }

    public int SkillCount { get; set; }

    public int UiCatalogCount { get; set; }

    public int RawAtlasCount { get; set; }

    public int RawAtlasSpriteCount { get; set; }

    public string CharacterManifestPath { get; set; } = string.Empty;

    public string SupportManifestPath { get; set; } = string.Empty;

    public string SkillCatalogPath { get; set; } = string.Empty;
}

public sealed class SyncAllCrossRegionReport
{
    public int SharedCharacterCount { get; set; }

    public int SharedSupportCount { get; set; }

    public int SharedSkillCount { get; set; }

    public int GlobalOnlyCharacterCount { get; set; }

    public int JapanOnlyCharacterCount { get; set; }

    public int GlobalOnlySupportCount { get; set; }

    public int JapanOnlySupportCount { get; set; }

    public int GlobalOnlySkillCount { get; set; }

    public int JapanOnlySkillCount { get; set; }
}
