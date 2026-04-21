namespace UmaAsset.Core.Models;

public sealed class GeneratedSkillCatalog
{
    public string GeneratedAtUtc { get; set; } = string.Empty;

    public List<GeneratedSkillCatalogEntry> Skills { get; set; } = [];
}

public sealed class GeneratedSkillCatalogEntry
{
    public int SkillId { get; set; }

    public int IconId { get; set; }

    public string? NameJa { get; set; }

    public string TextureName { get; set; } = string.Empty;

    public string? RelativePath { get; set; }
}
