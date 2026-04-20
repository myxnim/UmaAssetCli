using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace UmaAsset.External.GameTora.Models;

internal sealed class GameToraSkillRaw
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("enname")]
    public string? EnName { get; init; }

    [JsonPropertyName("name_en")]
    public string? NameEn { get; init; }

    [JsonPropertyName("jpname")]
    public string? JpName { get; init; }

    [JsonPropertyName("name_ko")]
    public string? NameKo { get; init; }

    [JsonPropertyName("name_tw")]
    public string? NameTw { get; init; }

    [JsonPropertyName("desc_en")]
    public string? DescEn { get; init; }

    [JsonPropertyName("jpdesc")]
    public string? JpDesc { get; init; }

    [JsonPropertyName("desc_ko")]
    public string? DescKo { get; init; }

    [JsonPropertyName("desc_tw")]
    public string? DescTw { get; init; }

    [JsonPropertyName("rarity")]
    public int Rarity { get; init; }

    [JsonPropertyName("iconid")]
    public int? IconId { get; init; }

    [JsonPropertyName("cost")]
    public int? Cost { get; init; }

    [JsonPropertyName("activation")]
    public int? Activation { get; init; }

    [JsonPropertyName("char")]
    public JsonNode? CharacterIds { get; init; }

    [JsonPropertyName("type")]
    public string[]? Types { get; init; }

    [JsonPropertyName("condition_groups")]
    public JsonNode? ConditionGroups { get; init; }

    [JsonPropertyName("gene_version")]
    public JsonNode? GeneVersion { get; init; }

    [JsonPropertyName("loc")]
    public JsonNode? LocalizedData { get; init; }

    [JsonPropertyName("parent_skills")]
    public JsonNode? ParentSkillIds { get; init; }

    [JsonPropertyName("evo_cond")]
    public JsonNode? EvolutionConditions { get; init; }

    [JsonPropertyName("pre_evo")]
    public GameToraPreEvolutionRaw? PreEvolution { get; init; }
}

internal sealed class GameToraPreEvolutionRaw
{
    [JsonPropertyName("card_id")]
    public int CardId { get; init; }

    [JsonPropertyName("old")]
    public int OldSkillId { get; init; }
}

internal sealed class GameToraCharacterCardRaw
{
    [JsonPropertyName("card_id")]
    public int CardId { get; init; }

    [JsonPropertyName("char_id")]
    public int CharId { get; init; }

    [JsonPropertyName("costume")]
    public int Costume { get; init; }

    [JsonPropertyName("url_name")]
    public string? UrlName { get; init; }

    [JsonPropertyName("name_en")]
    public string? NameEn { get; init; }

    [JsonPropertyName("name_jp")]
    public string? NameJp { get; init; }

    [JsonPropertyName("name_ko")]
    public string? NameKo { get; init; }

    [JsonPropertyName("name_tw")]
    public string? NameTw { get; init; }

    [JsonPropertyName("title_en_gl")]
    public string? TitleEnGlobal { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("title_jp")]
    public string? TitleJp { get; init; }

    [JsonPropertyName("title_ko")]
    public string? TitleKo { get; init; }

    [JsonPropertyName("title_tw")]
    public string? TitleTw { get; init; }

    [JsonPropertyName("rarity")]
    public int Rarity { get; init; }

    [JsonPropertyName("obtained")]
    public string? Obtained { get; init; }

    [JsonPropertyName("version")]
    public string? Version { get; init; }

    [JsonPropertyName("release")]
    public string? ReleaseJp { get; init; }

    [JsonPropertyName("release_en")]
    public string? ReleaseGlobal { get; init; }

    [JsonPropertyName("release_ko")]
    public string? ReleaseKo { get; init; }

    [JsonPropertyName("release_zh_tw")]
    public string? ReleaseTw { get; init; }

    [JsonPropertyName("base_stats")]
    public int[]? BaseStats { get; init; }

    [JsonPropertyName("four_star_stats")]
    public int[]? FourStarStats { get; init; }

    [JsonPropertyName("five_star_stats")]
    public int[]? FiveStarStats { get; init; }

    [JsonPropertyName("stat_bonus")]
    public int[]? StatBonuses { get; init; }

    [JsonPropertyName("aptitude")]
    public string[]? Aptitudes { get; init; }

    [JsonPropertyName("skills_unique")]
    public JsonNode? SkillsUnique { get; init; }

    [JsonPropertyName("skills_innate")]
    public JsonNode? SkillsInnate { get; init; }

    [JsonPropertyName("skills_awakening")]
    public JsonNode? SkillsAwakening { get; init; }

    [JsonPropertyName("skills_event")]
    public JsonNode? SkillsEvent { get; init; }

    [JsonPropertyName("skills_evo")]
    public GameToraCharacterEvolutionRaw[]? SkillsEvolution { get; init; }
}

internal sealed class GameToraCharacterEvolutionRaw
{
    [JsonPropertyName("old")]
    public int OldSkillId { get; init; }

    [JsonPropertyName("new")]
    public int NewSkillId { get; init; }
}

internal sealed class GameToraSupportCardRaw
{
    [JsonPropertyName("support_id")]
    public int SupportId { get; init; }

    [JsonPropertyName("url_name")]
    public string? UrlName { get; init; }

    [JsonPropertyName("char_id")]
    public int CharId { get; init; }

    [JsonPropertyName("char_name")]
    public string? CharName { get; init; }

    [JsonPropertyName("name_jp")]
    public string? NameJp { get; init; }

    [JsonPropertyName("name_ko")]
    public string? NameKo { get; init; }

    [JsonPropertyName("name_tw")]
    public string? NameTw { get; init; }

    [JsonPropertyName("rarity")]
    public int Rarity { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("obtained")]
    public string? Obtained { get; init; }

    [JsonPropertyName("release")]
    public string? ReleaseJp { get; init; }

    [JsonPropertyName("release_en")]
    public string? ReleaseGlobal { get; init; }

    [JsonPropertyName("release_ko")]
    public string? ReleaseKo { get; init; }

    [JsonPropertyName("release_zh_tw")]
    public string? ReleaseTw { get; init; }

    [JsonPropertyName("effects")]
    public int[][]? Effects { get; init; }

    [JsonPropertyName("event_skills")]
    public JsonNode? EventSkillIds { get; init; }

    [JsonPropertyName("hints")]
    public GameToraSupportHintsRaw? Hints { get; init; }
}

internal sealed class GameToraSupportHintsRaw
{
    [JsonPropertyName("hint_skills")]
    public JsonNode? HintSkillIds { get; init; }

    [JsonPropertyName("hint_others")]
    public GameToraHintOtherRaw[]? HintOthers { get; init; }
}

internal sealed class GameToraHintOtherRaw
{
    [JsonPropertyName("hint_type")]
    public int HintType { get; init; }

    [JsonPropertyName("hint_value")]
    public int HintValue { get; init; }
}

internal sealed class GameToraSupportEffectDefinitionRaw
{
    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("name_en")]
    public string? NameEn { get; init; }

    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("symbol")]
    public string? Symbol { get; init; }
}
