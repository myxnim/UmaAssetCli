using System.Text.Json.Nodes;

namespace UmaAsset.External.GameTora.Models;

public sealed class GameToraSyncMetadata
{
    public required string Server { get; init; }

    public required string GeneratedAtUtc { get; init; }

    public required string ManifestUrl { get; init; }

    public required string CacheDirectory { get; init; }

    public required Dictionary<string, string> Sources { get; init; }

    public required Dictionary<string, int> Counts { get; init; }
}

public sealed class GameToraCharacterCatalog
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Version { get; init; }

    public required string Source { get; init; }

    public required string ImportedAt { get; init; }

    public required IReadOnlyList<GameToraCharacterEntry> Characters { get; init; }
}

public sealed class GameToraCharacterEntry
{
    public required int CardId { get; init; }

    public required int CharId { get; init; }

    public required int CostumeId { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public string? NameJp { get; init; }

    public string? NameKo { get; init; }

    public string? NameTw { get; init; }

    public string? Title { get; init; }

    public string? TitleJp { get; init; }

    public string? TitleKo { get; init; }

    public string? TitleTw { get; init; }

    public required int Rarity { get; init; }

    public string? Obtained { get; init; }

    public string? VersionTag { get; init; }

    public required GameToraReleaseInfo Release { get; init; }

    public required int[] BaseStats { get; init; }

    public required int[] FourStarStats { get; init; }

    public required int[] FiveStarStats { get; init; }

    public required int[] StatBonuses { get; init; }

    public required string[] Aptitudes { get; init; }

    public required GameToraCharacterSkillRefs Skills { get; init; }
}

public sealed class GameToraCharacterSkillRefs
{
    public required int[] Unique { get; init; }

    public required int[] Innate { get; init; }

    public required int[] Awakening { get; init; }

    public required int[] Event { get; init; }

    public required IReadOnlyList<GameToraSkillEvolutionRef> Evolutions { get; init; }
}

public sealed class GameToraSkillEvolutionRef
{
    public required int OldSkillId { get; init; }

    public required int NewSkillId { get; init; }
}

public sealed class GameToraReleaseInfo
{
    public string? Jp { get; init; }

    public string? Global { get; init; }

    public string? Korea { get; init; }

    public string? Taiwan { get; init; }
}

public sealed class GameToraSkillCatalog
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Version { get; init; }

    public required string Source { get; init; }

    public required string ImportedAt { get; init; }

    public required IReadOnlyList<GameToraSkillEntry> Skills { get; init; }
}

public sealed class GameToraSkillEntry
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public string? NameJp { get; init; }

    public string? NameKo { get; init; }

    public string? NameTw { get; init; }

    public string? DescriptionEn { get; init; }

    public string? DescriptionJp { get; init; }

    public string? DescriptionKo { get; init; }

    public string? DescriptionTw { get; init; }

    public required int Rarity { get; init; }

    public int? IconId { get; init; }

    public int? Cost { get; init; }

    public int? Activation { get; init; }

    public required int[] CharacterIds { get; init; }

    public required string[] Tags { get; init; }

    public JsonNode? ConditionGroups { get; init; }

    public JsonNode? GeneVersion { get; init; }

    public JsonNode? LocalizedData { get; init; }

    public int[]? ParentSkillIds { get; init; }

    public JsonNode? EvolutionConditions { get; init; }

    public GameToraPreEvolutionRef? PreEvolution { get; init; }
}

public sealed class GameToraPreEvolutionRef
{
    public required int CardId { get; init; }

    public required int OldSkillId { get; init; }
}

public sealed class GameToraSupportCatalog
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Version { get; init; }

    public required string Source { get; init; }

    public required string ImportedAt { get; init; }

    public required IReadOnlyList<GameToraSupportEntry> Supports { get; init; }
}

public sealed class GameToraSupportEntry
{
    public required int SupportId { get; init; }

    public required string Slug { get; init; }

    public required string Name { get; init; }

    public string? NameJp { get; init; }

    public string? NameKo { get; init; }

    public string? NameTw { get; init; }

    public required int CharacterId { get; init; }

    public string? CharacterName { get; init; }

    public required int Rarity { get; init; }

    public string? Type { get; init; }

    public string? Obtained { get; init; }

    public required GameToraReleaseInfo Release { get; init; }

    public required IReadOnlyList<GameToraSupportEffectEntry> Effects { get; init; }

    public required int[] EventSkillIds { get; init; }

    public required int[] HintSkillIds { get; init; }

    public required IReadOnlyList<GameToraSupportHintEffectEntry> HintEffects { get; init; }
}

public sealed class GameToraSupportEffectEntry
{
    public required int Id { get; init; }

    public required string Name { get; init; }

    public string? Symbol { get; init; }

    public required int[] Values { get; init; }
}

public sealed class GameToraSupportHintEffectEntry
{
    public required int HintType { get; init; }

    public required int HintValue { get; init; }
}
