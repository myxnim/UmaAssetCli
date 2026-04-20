using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace UmaAsset.External.GameTora.Services;

public sealed class GameToraCatalogSync
{
    private const string DefaultBaseUrl = "https://gametora.com";
    private const string UserAgent = "Mozilla/5.0 (compatible; umaassetcli/1.0)";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private readonly HttpClient httpClient;
    private readonly string cacheDirectory;
    private readonly bool noFetch;

    public GameToraCatalogSync(string cacheDirectory, bool noFetch)
    {
        this.cacheDirectory = cacheDirectory;
        this.noFetch = noFetch;
        httpClient = new HttpClient
        {
            BaseAddress = new Uri(DefaultBaseUrl),
        };
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.UserAgent.Add(ProductInfoHeaderValue.Parse("umaassetcli/1.0"));
        httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", UserAgent);
    }

    public async Task<GameToraSyncArtifacts> SyncAsync(string outputDirectory, bool includeSupports, string server, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(outputDirectory);
        Directory.CreateDirectory(cacheDirectory);

        var manifestUrl = $"{DefaultBaseUrl}/data/manifests/umamusume.json";
        var manifest = await FetchJsonAsync<Dictionary<string, string>>(manifestUrl, cancellationToken)
            ?? throw new InvalidOperationException("Could not load GameTora manifest.");

        var timestamp = DateTimeOffset.UtcNow;
        var version = timestamp.ToString("yyyy-MM-dd");
        var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var characterCardsUrl = BuildManifestUrl(manifest, "character-cards");
        var skillsUrl = BuildManifestUrl(manifest, "skills");
        if (characterCardsUrl is null || skillsUrl is null)
        {
            throw new InvalidOperationException("Manifest is missing required GameTora keys.");
        }

        var characterCards = await FetchJsonAsync<List<GameToraCharacterCardRaw>>(characterCardsUrl, cancellationToken) ?? [];
        var skills = await FetchJsonAsync<List<GameToraSkillRaw>>(skillsUrl, cancellationToken) ?? [];
        sources["character-cards"] = characterCardsUrl;
        sources["skills"] = skillsUrl;

        var characterCatalog = BuildCharacterCatalog(characterCards, version, timestamp);
        var skillCatalog = BuildSkillCatalog(skills, version, timestamp);

        string? supportCatalogPath = null;
        GameToraSupportCatalog? supportCatalog = null;
        if (includeSupports)
        {
            var supportCardsUrl = BuildManifestUrl(manifest, "support-cards");
            var supportEffectsUrl = BuildManifestUrl(manifest, "support_effects");
            if (supportCardsUrl is null || supportEffectsUrl is null)
            {
                throw new InvalidOperationException("Manifest is missing support catalog keys.");
            }

            var supportCards = await FetchJsonAsync<List<GameToraSupportCardRaw>>(supportCardsUrl, cancellationToken) ?? [];
            var supportEffects = await FetchJsonAsync<List<GameToraSupportEffectDefinitionRaw>>(supportEffectsUrl, cancellationToken) ?? [];
            sources["support-cards"] = supportCardsUrl;
            sources["support_effects"] = supportEffectsUrl;

            supportCatalog = BuildSupportCatalog(supportCards, supportEffects, version, timestamp);
            supportCatalogPath = Path.Combine(outputDirectory, "gametora-support-catalog.json");
            await WriteJsonAsync(supportCatalogPath, supportCatalog, cancellationToken);
        }

        var characterCatalogPath = Path.Combine(outputDirectory, "gametora-character-catalog.json");
        var skillCatalogPath = Path.Combine(outputDirectory, "gametora-skill-catalog.json");
        var metadataPath = Path.Combine(outputDirectory, "gametora-sync-metadata.json");

        await WriteJsonAsync(characterCatalogPath, characterCatalog, cancellationToken);
        await WriteJsonAsync(skillCatalogPath, skillCatalog, cancellationToken);

        var metadata = new GameToraSyncMetadata
        {
            Server = server,
            GeneratedAtUtc = timestamp.ToString("O"),
            ManifestUrl = manifestUrl,
            CacheDirectory = cacheDirectory,
            Sources = sources,
            Counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                ["characters"] = characterCatalog.Characters.Count,
                ["skills"] = skillCatalog.Skills.Count,
                ["supports"] = supportCatalog?.Supports.Count ?? 0,
            },
        };

        await WriteJsonAsync(metadataPath, metadata, cancellationToken);

        return new GameToraSyncArtifacts
        {
            CharacterCatalogPath = characterCatalogPath,
            SkillCatalogPath = skillCatalogPath,
            SupportCatalogPath = supportCatalogPath,
            MetadataPath = metadataPath,
            CharacterCount = characterCatalog.Characters.Count,
            SkillCount = skillCatalog.Skills.Count,
            SupportCount = supportCatalog?.Supports.Count ?? 0,
        };
    }

    private static string? BuildManifestUrl(IReadOnlyDictionary<string, string> manifest, string key)
    {
        return manifest.TryGetValue(key, out var hash) && !string.IsNullOrWhiteSpace(hash)
            ? $"{DefaultBaseUrl}/data/umamusume/{key}.{hash}.json"
            : null;
    }

    private async Task<T?> FetchJsonAsync<T>(string url, CancellationToken cancellationToken)
    {
        var cachePath = GetCachePath(url);

        if (noFetch)
        {
            if (!File.Exists(cachePath))
            {
                throw new FileNotFoundException($"Cache miss for {url}", cachePath);
            }

            await using var cacheStream = File.OpenRead(cachePath);
            return await JsonSerializer.DeserializeAsync<T>(cacheStream, JsonOptions, cancellationToken);
        }

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            await using var memory = new MemoryStream();
            await responseStream.CopyToAsync(memory, cancellationToken);
            memory.Position = 0;

            await File.WriteAllBytesAsync(cachePath, memory.ToArray(), cancellationToken);
            memory.Position = 0;
            return await JsonSerializer.DeserializeAsync<T>(memory, JsonOptions, cancellationToken);
        }
        catch when (File.Exists(cachePath))
        {
            await using var cacheStream = File.OpenRead(cachePath);
            return await JsonSerializer.DeserializeAsync<T>(cacheStream, JsonOptions, cancellationToken);
        }
    }

    private string GetCachePath(string url)
    {
        var slug = url
            .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase);
        slug = string.Concat(slug.Select(ch => char.IsLetterOrDigit(ch) || ch is '.' or '-' ? ch : '_'));
        if (!slug.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            slug += ".json";
        }

        return Path.Combine(cacheDirectory, slug);
    }

    private static async Task WriteJsonAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, JsonOptions, cancellationToken);
        await stream.WriteAsync("\n"u8.ToArray(), cancellationToken);
    }

    private static GameToraCharacterCatalog BuildCharacterCatalog(IEnumerable<GameToraCharacterCardRaw> cards, string version, DateTimeOffset timestamp)
    {
        var entries = cards
            .OrderBy(static card => card.CardId)
            .Select(static card => new GameToraCharacterEntry
            {
                CardId = card.CardId,
                CharId = card.CharId,
                CostumeId = card.Costume,
                Slug = card.UrlName ?? card.CardId.ToString(),
                Name = card.NameEn ?? card.NameJp ?? card.UrlName ?? card.CardId.ToString(),
                NameJp = card.NameJp,
                NameKo = card.NameKo,
                NameTw = card.NameTw,
                Title = card.TitleEnGlobal ?? card.Title,
                TitleJp = card.TitleJp,
                TitleKo = card.TitleKo,
                TitleTw = card.TitleTw,
                Rarity = card.Rarity,
                Obtained = card.Obtained,
                VersionTag = card.Version,
                Release = new GameToraReleaseInfo
                {
                    Jp = card.ReleaseJp,
                    Global = card.ReleaseGlobal,
                    Korea = card.ReleaseKo,
                    Taiwan = card.ReleaseTw,
                },
                BaseStats = card.BaseStats ?? [],
                FourStarStats = card.FourStarStats ?? [],
                FiveStarStats = card.FiveStarStats ?? [],
                StatBonuses = card.StatBonuses ?? [],
                Aptitudes = card.Aptitudes ?? [],
                Skills = new GameToraCharacterSkillRefs
                {
                    Unique = ParseIntArray(card.SkillsUnique),
                    Innate = ParseIntArray(card.SkillsInnate),
                    Awakening = ParseIntArray(card.SkillsAwakening),
                    Event = ParseIntArray(card.SkillsEvent),
                    Evolutions = (card.SkillsEvolution ?? [])
                        .Select(static evo => new GameToraSkillEvolutionRef
                        {
                            OldSkillId = evo.OldSkillId,
                            NewSkillId = evo.NewSkillId,
                        })
                        .ToArray(),
                },
            })
            .ToArray();

        return new GameToraCharacterCatalog
        {
            Id = "gametora-character-catalog",
            Name = "GameTora Character Catalog",
            Version = version,
            Source = "GameTora",
            ImportedAt = timestamp.ToString("O"),
            Characters = entries,
        };
    }

    private static GameToraSkillCatalog BuildSkillCatalog(IEnumerable<GameToraSkillRaw> skills, string version, DateTimeOffset timestamp)
    {
        var entries = skills
            .OrderBy(static skill => skill.Id)
            .Select(static skill => new GameToraSkillEntry
            {
                Id = skill.Id,
                Name = FirstNonEmpty(skill.EnName, skill.NameEn, skill.JpName) ?? skill.Id.ToString(),
                NameJp = skill.JpName,
                NameKo = skill.NameKo,
                NameTw = skill.NameTw,
                DescriptionEn = skill.DescEn,
                DescriptionJp = skill.JpDesc,
                DescriptionKo = skill.DescKo,
                DescriptionTw = skill.DescTw,
                Rarity = skill.Rarity,
                IconId = skill.IconId,
                Cost = skill.Cost,
                Activation = skill.Activation,
                CharacterIds = ParseIntArray(skill.CharacterIds),
                Tags = skill.Types ?? [],
                ConditionGroups = skill.ConditionGroups?.DeepClone(),
                GeneVersion = skill.GeneVersion?.DeepClone(),
                LocalizedData = skill.LocalizedData?.DeepClone(),
                ParentSkillIds = ParseIntArray(skill.ParentSkillIds),
                EvolutionConditions = skill.EvolutionConditions?.DeepClone(),
                PreEvolution = skill.PreEvolution is null
                    ? null
                    : new GameToraPreEvolutionRef
                    {
                        CardId = skill.PreEvolution.CardId,
                        OldSkillId = skill.PreEvolution.OldSkillId,
                    },
            })
            .ToArray();

        return new GameToraSkillCatalog
        {
            Id = "gametora-skill-catalog",
            Name = "GameTora Skill Catalog",
            Version = version,
            Source = "GameTora",
            ImportedAt = timestamp.ToString("O"),
            Skills = entries,
        };
    }

    private static GameToraSupportCatalog BuildSupportCatalog(
        IEnumerable<GameToraSupportCardRaw> supportCards,
        IEnumerable<GameToraSupportEffectDefinitionRaw> supportEffects,
        string version,
        DateTimeOffset timestamp)
    {
        var effectLookup = supportEffects.ToDictionary(
            static effect => effect.Id,
            static effect => effect,
            EqualityComparer<int>.Default);

        var entries = supportCards
            .OrderBy(static support => support.SupportId)
            .Select(support => new GameToraSupportEntry
            {
                SupportId = support.SupportId,
                Slug = support.UrlName ?? support.SupportId.ToString(),
                Name = $"{support.CharName ?? support.NameJp ?? support.SupportId.ToString()} ({MapSupportRarity(support.Rarity)})",
                NameJp = support.NameJp,
                NameKo = support.NameKo,
                NameTw = support.NameTw,
                CharacterId = support.CharId,
                CharacterName = support.CharName,
                Rarity = support.Rarity,
                Type = support.Type,
                Obtained = support.Obtained,
                Release = new GameToraReleaseInfo
                {
                    Jp = support.ReleaseJp,
                    Global = support.ReleaseGlobal,
                    Korea = support.ReleaseKo,
                    Taiwan = support.ReleaseTw,
                },
                Effects = ParseSupportEffects(support.Effects, effectLookup),
                EventSkillIds = ParseIntArray(support.EventSkillIds),
                HintSkillIds = ParseIntArray(support.Hints?.HintSkillIds),
                HintEffects = (support.Hints?.HintOthers ?? [])
                    .Select(static hint => new GameToraSupportHintEffectEntry
                    {
                        HintType = hint.HintType,
                        HintValue = hint.HintValue,
                    })
                    .ToArray(),
            })
            .ToArray();

        return new GameToraSupportCatalog
        {
            Id = "gametora-support-catalog",
            Name = "GameTora Support Catalog",
            Version = version,
            Source = "GameTora",
            ImportedAt = timestamp.ToString("O"),
            Supports = entries,
        };
    }

    private static IReadOnlyList<GameToraSupportEffectEntry> ParseSupportEffects(
        IReadOnlyList<int[]>? rawEffects,
        IReadOnlyDictionary<int, GameToraSupportEffectDefinitionRaw> definitions)
    {
        if (rawEffects is null || rawEffects.Count == 0)
        {
            return [];
        }

        return rawEffects
            .Where(static effect => effect.Length > 1)
            .Select(effect =>
            {
                var effectId = effect[0];
                definitions.TryGetValue(effectId, out var definition);
                return new GameToraSupportEffectEntry
                {
                    Id = effectId,
                    Name = definition?.NameEn ?? definition?.Name ?? $"Effect #{effectId}",
                    Symbol = definition?.Symbol,
                    Values = effect.Skip(1).Select(static value => value < 0 ? 0 : value).ToArray(),
                };
            })
            .ToArray();
    }

    private static string MapSupportRarity(int rarity)
    {
        return rarity switch
        {
            1 => "R",
            2 => "SR",
            3 => "SSR",
            _ => rarity.ToString(),
        };
    }

    private static int[] ParseIntArray(JsonNode? node)
    {
        if (node is not JsonArray array)
        {
            return [];
        }

        var result = new List<int>(array.Count);
        foreach (var item in array)
        {
            if (item is not JsonValue valueNode)
            {
                continue;
            }

            if (valueNode.TryGetValue<int>(out var intValue))
            {
                result.Add(intValue);
                continue;
            }

            if (valueNode.TryGetValue<string>(out var stringValue) && int.TryParse(stringValue, out intValue))
            {
                result.Add(intValue);
            }
        }

        return result.ToArray();
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value));
    }
}

public sealed class GameToraSyncArtifacts
{
    public required string CharacterCatalogPath { get; init; }

    public required string SkillCatalogPath { get; init; }

    public string? SupportCatalogPath { get; init; }

    public required string MetadataPath { get; init; }

    public required int CharacterCount { get; init; }

    public required int SkillCount { get; init; }

    public required int SupportCount { get; init; }
}
