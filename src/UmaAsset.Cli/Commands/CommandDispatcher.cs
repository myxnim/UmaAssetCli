using System.Text.Json;
using UmaAsset.Cli.Output;

namespace UmaAsset.Cli.Commands;

public sealed class CommandDispatcher
{
    public Task<int> RunAsync(string[] args)
    {
        if (args.Length == 0 || IsHelp(args[0]))
        {
            PrintHelp();
            return Task.FromResult(0);
        }

        try
        {
            return Task.FromResult(args[0].ToLowerInvariant() switch
            {
                "detect" => RunDetect(),
                "sync-gametora" => RunSyncGameTora(args[1..]),
                "lookup" => RunLookup(args[1..]),
                "search" => RunSearch(args[1..]),
                "inspect-bundle" => RunInspectBundle(args[1..]),
                "dump-asset" => RunDumpAsset(args[1..]),
                "stage" => RunStage(args[1..], treatIdsAsCharaIcons: false),
                "stage-chara-icons" => RunStage(args[1..], treatIdsAsCharaIcons: true),
                "extract-textures" => RunExtractTextures(args[1..], treatIdsAsCharaIcons: false),
                "extract-sprites" => RunExtractSprites(args[1..]),
                "extract-chara-icons" => RunExtractTextures(args[1..], treatIdsAsCharaIcons: true),
                "extract-support-icons" => RunExtractSupportIcons(args[1..]),
                "extract-skill-icons" => RunExtractSkillIcons(args[1..]),
                "extract-ui-icons" => RunExtractUiIcons(args[1..]),
                "extract-raw-atlases" => RunExtractRawAtlases(args[1..]),
                "sync-all" => RunSyncAll(args[1..]),
                "export-rank-badges" => RunExportRankBadges(args[1..]),
                "generate-manifest" => RunGenerateManifest(args[1..]),
                _ => Fail($"Unknown command '{args[0]}'."),
            });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex}");
            return Task.FromResult(1);
        }
    }

    private static int RunDetect()
    {
        var installs = UmaInstallLocator.DetectAll();
        if (installs.Count == 0)
        {
            Console.WriteLine("No installs detected.");
            return 1;
        }

        foreach (var install in installs)
        {
            Console.WriteLine($"{install.Name}: {install.Path}");
        }

        return 0;
    }

    private static int RunSyncGameTora(string[] args)
    {
        var options = ParseOptions(args);
        var output = GetSingle(options, "--output")
            ?? Path.Combine(Environment.CurrentDirectory, "out", "gametora");
        var cacheDirectory = GetSingle(options, "--cache-dir")
            ?? Path.Combine(Environment.CurrentDirectory, ".cache_gametora");
        var server = GetSingle(options, "--server") ?? "global";
        var includeSupports = options.ContainsKey("--include-supports");
        var noFetch = options.ContainsKey("--no-fetch");

        var sync = new GameToraCatalogSync(cacheDirectory, noFetch);
        var artifacts = sync.SyncAsync(output, includeSupports, server).GetAwaiter().GetResult();

        Console.WriteLine($"Character catalog -> {artifacts.CharacterCatalogPath}");
        Console.WriteLine($"Skill catalog -> {artifacts.SkillCatalogPath}");
        if (!string.IsNullOrWhiteSpace(artifacts.SupportCatalogPath))
        {
            Console.WriteLine($"Support catalog -> {artifacts.SupportCatalogPath}");
        }
        Console.WriteLine($"Metadata -> {artifacts.MetadataPath}");
        Console.WriteLine();
        Console.WriteLine($"Characters: {artifacts.CharacterCount}");
        Console.WriteLine($"Skills: {artifacts.SkillCount}");
        Console.WriteLine($"Supports: {artifacts.SupportCount}");
        return 0;
    }

    private static int RunLookup(string[] args)
    {
        var options = ParseOptions(args);
        var install = UmaInstallLocator.Resolve(GetSingle(options, "--uma-dir"));
        var manifest = new ManifestDatabase(install.Path);

        var names = GatherValues(options, "--name");
        names.AddRange(GatherValues(options, "--base-name"));
        if (names.Count == 0)
        {
            return Fail("lookup expects at least one --name or --base-name value.");
        }

        var entries = manifest.FindMany(names)
            .OrderBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (entries.Length == 0)
        {
            Console.WriteLine("No manifest entries matched.");
            return 1;
        }

        if (options.ContainsKey("--json"))
        {
            Console.WriteLine(JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        foreach (var entry in entries)
        {
            Console.WriteLine(entry.Name);
            Console.WriteLine($"  Hash: {entry.HashName}");
            Console.WriteLine($"  File: {manifest.GetDataFilePath(entry)}");
            Console.WriteLine($"  Encrypted: {(entry.EncryptionKey != 0 ? "yes" : "no")}");
        }

        return 0;
    }

    private static int RunSearch(string[] args)
    {
        var options = ParseOptions(args);
        var install = UmaInstallLocator.Resolve(GetSingle(options, "--uma-dir"));
        var manifest = new ManifestDatabase(install.Path);

        var patterns = GatherValues(options, "--contains");
        if (patterns.Count == 0)
        {
            return Fail("search expects at least one --contains value.");
        }

        var limit = ParseInt(GetSingle(options, "--limit"), fallback: 200);
        var entries = manifest.SearchBySubstring(patterns, limit);
        if (entries.Count == 0)
        {
            Console.WriteLine("No manifest entries matched.");
            return 1;
        }

        if (options.ContainsKey("--json"))
        {
            Console.WriteLine(JsonSerializer.Serialize(entries, new JsonSerializerOptions { WriteIndented = true }));
            return 0;
        }

        foreach (var entry in entries)
        {
            Console.WriteLine(entry.Name);
        }

        return 0;
    }

    private static int RunInspectBundle(string[] args)
    {
        var options = ParseOptions(args);
        var install = UmaInstallLocator.Resolve(GetSingle(options, "--uma-dir"));
        var manifest = new ManifestDatabase(install.Path);
        var names = GatherValues(options, "--name");
        names.AddRange(GatherValues(options, "--base-name"));
        if (names.Count == 0)
        {
            return Fail("inspect-bundle expects at least one --name or --base-name value.");
        }

        var entries = manifest.FindMany(names).ToArray();
        if (entries.Length == 0)
        {
            Console.WriteLine("No manifest entries matched.");
            return 1;
        }

        var inspector = new BundleAssetInspector(manifest);
        foreach (var entry in entries.OrderBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase))
        {
            var assets = inspector.Inspect(entry);
            Console.WriteLine($"[{entry.Name}]");

            foreach (var asset in assets.OrderBy(static asset => asset.AssetsFileName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(static asset => asset.TypeName, StringComparer.OrdinalIgnoreCase)
                         .ThenBy(static asset => asset.Name, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine($"{asset.AssetsFileName} | {asset.TypeName} | {asset.Name} | {asset.PathId}");
            }

            Console.WriteLine();
        }

        return 0;
    }

    private static int RunDumpAsset(string[] args)
    {
        var options = ParseOptions(args);
        var install = UmaInstallLocator.Resolve(GetSingle(options, "--uma-dir"));
        var manifest = new ManifestDatabase(install.Path);
        var names = GatherValues(options, "--name");
        names.AddRange(GatherValues(options, "--base-name"));
        if (names.Count == 0)
        {
            return Fail("dump-asset expects at least one --name or --base-name value.");
        }

        var assetName = GetSingle(options, "--asset-name");
        if (string.IsNullOrWhiteSpace(assetName))
        {
            return Fail("dump-asset expects --asset-name <name>.");
        }

        var entry = manifest.FindMany(names).FirstOrDefault();
        if (entry is null)
        {
            Console.WriteLine("No manifest entries matched.");
            return 1;
        }

        var dump = new BundleAssetFieldDumper(manifest).Dump(entry, assetName);
        Console.WriteLine(dump);
        return 0;
    }

    private static int RunStage(string[] args, bool treatIdsAsCharaIcons)
    {
        var options = ParseOptions(args);
        var install = UmaInstallLocator.Resolve(GetSingle(options, "--uma-dir"));
        var manifest = new ManifestDatabase(install.Path);
        var exporter = new AssetExporter(manifest);

        var output = GetSingle(options, "--output")
            ?? Path.Combine(Environment.CurrentDirectory, "out", "staged-assets");
        var decrypt = options.ContainsKey("--decrypt");
        var flatten = options.ContainsKey("--flatten");

        var requestedNames = GatherValues(options, "--name");
        requestedNames.AddRange(GatherValues(options, "--base-name"));

        if (treatIdsAsCharaIcons)
        {
            var plate = ParseInt(GetSingle(options, "--plate"), fallback: 2);
            var ids = GatherValues(options, "--ids");
            var families = GatherValues(options, "--family");
            if (ids.Count == 0)
            {
                return Fail("stage-chara-icons expects at least one value after --ids.");
            }

            requestedNames.AddRange(CharaIconResourceNames.FromIds(ids, plate, families));
        }

        if (requestedNames.Count == 0)
        {
            return Fail("stage expects --name/--base-name values, or --ids for stage-chara-icons.");
        }

        var entries = manifest.FindMany(requestedNames).ToArray();
        var foundNames = entries
            .SelectMany(entry => new[] { entry.Name, entry.BaseName })
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var missingNames = requestedNames
            .Where(name => !foundNames.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var entry in entries.OrderBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase))
        {
            var written = exporter.Export(entry, output, decrypt, flatten);
            Console.WriteLine($"{entry.BaseName} -> {written}");
        }

        if (missingNames.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Missing entries:");
            foreach (var missing in missingNames)
            {
                Console.WriteLine($"  {missing}");
            }
        }

        return entries.Length == 0 ? 1 : 0;
    }

    private static int RunExtractTextures(string[] args, bool treatIdsAsCharaIcons)
    {
        var options = ParseOptions(args);
        var install = UmaInstallLocator.Resolve(GetSingle(options, "--uma-dir"));
        var manifest = new ManifestDatabase(install.Path);
        var exporter = new TextureBundleExporter(manifest);

        var output = GetSingle(options, "--output")
            ?? Path.Combine(Environment.CurrentDirectory, "out", "textures");
        var exactTextureNames = GatherValues(options, "--texture-name");

        var requestedNames = GatherValues(options, "--name");
        requestedNames.AddRange(GatherValues(options, "--base-name"));

        if (treatIdsAsCharaIcons)
        {
            var plate = ParseInt(GetSingle(options, "--plate"), fallback: 2);
            var ids = GatherValues(options, "--ids");
            var families = GatherValues(options, "--family");
            if (ids.Count == 0)
            {
                return Fail("extract-chara-icons expects at least one value after --ids.");
            }

            requestedNames.AddRange(CharaIconResourceNames.FromIds(ids, plate, families));
        }

        if (requestedNames.Count == 0)
        {
            return Fail("extract-textures expects --name/--base-name values, or --ids for extract-chara-icons.");
        }

        var entries = manifest.FindMany(requestedNames).ToArray();
        if (entries.Length == 0)
        {
            Console.WriteLine("No manifest entries matched.");
            return 1;
        }

        foreach (var entry in entries.OrderBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase))
        {
            var results = exporter.ExportTextures(entry, output, exactTextureNames);
            if (results.Count == 0)
            {
                Console.WriteLine($"{entry.BaseName} -> no matching Texture2D assets");
                continue;
            }

            foreach (var result in results)
            {
                Console.WriteLine($"{entry.BaseName}:{result.TextureName} -> {result.OutputPath}");
            }
        }

        return 0;
    }

    private static int RunExtractSprites(string[] args)
    {
        var options = ParseOptions(args);
        var install = UmaInstallLocator.Resolve(GetSingle(options, "--uma-dir"));
        var manifest = new ManifestDatabase(install.Path);
        var exporter = new SpriteBundleExporter(manifest);

        var output = GetSingle(options, "--output")
            ?? Path.Combine(Environment.CurrentDirectory, "out", "sprites");
        var spriteNames = GatherValues(options, "--sprite-name");
        var requestedNames = GatherValues(options, "--name");
        requestedNames.AddRange(GatherValues(options, "--base-name"));

        if (requestedNames.Count == 0)
        {
            return Fail("extract-sprites expects at least one --name or --base-name value.");
        }

        var entries = manifest.FindMany(requestedNames).ToArray();
        if (entries.Length == 0)
        {
            Console.WriteLine("No manifest entries matched.");
            return 1;
        }

        IReadOnlyDictionary<string, string>? spriteMap = null;
        if (spriteNames.Count > 0)
        {
            spriteMap = spriteNames.ToDictionary(
                static spriteName => spriteName,
                static spriteName => spriteName,
                StringComparer.OrdinalIgnoreCase);
        }

        foreach (var entry in entries.OrderBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase))
        {
            var entryOutput = Path.Combine(output, entry.BaseName);
            var results = exporter.ExportSprites(entry, entryOutput, spriteMap);
            if (results.Count == 0)
            {
                Console.WriteLine($"{entry.BaseName} -> no matching Sprite assets");
                continue;
            }

            foreach (var result in results)
            {
                Console.WriteLine($"{entry.BaseName}:{result.SpriteName} -> {result.OutputPath}");
            }
        }

        return 0;
    }

    private static int RunGenerateManifest(string[] args)
    {
        var options = ParseOptions(args);
        var input = GetSingle(options, "--input")
            ?? Path.Combine(Environment.CurrentDirectory, "out", "organized");
        var output = GetSingle(options, "--output")
            ?? Path.Combine(input, "character-icons.json");

        var written = AssetManifestGenerator.Write(input, output);
        Console.WriteLine(written);
        return 0;
    }

    private static int RunExtractSupportIcons(string[] args)
    {
        var options = ParseOptions(args);
        var install = UmaInstallLocator.Resolve(GetSingle(options, "--uma-dir"));
        var manifest = new ManifestDatabase(install.Path);
        var exporter = new TextureBundleExporter(manifest);

        var ids = GatherValues(options, "--ids");
        if (ids.Count == 0)
        {
            return Fail("extract-support-icons expects at least one value after --ids.");
        }

        var output = GetSingle(options, "--output")
            ?? Path.Combine(Environment.CurrentDirectory, "out", "support-icons");
        var resourceNames = SupportIconResourceNames.FromIds(ids);
        var entries = manifest.FindMany(resourceNames).ToArray();
        var missing = resourceNames.Except(entries.Select(static entry => entry.BaseName), StringComparer.OrdinalIgnoreCase).ToArray();

        foreach (var entry in entries.OrderBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase))
        {
            var results = exporter.ExportTextures(entry, output, [entry.BaseName]);
            foreach (var result in results)
            {
                Console.WriteLine($"{entry.BaseName}:{result.TextureName} -> {result.OutputPath}");
            }
        }

        if (missing.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Missing entries:");
            foreach (var item in missing)
            {
                Console.WriteLine($"  {item}");
            }
        }

        return entries.Length == 0 ? 1 : 0;
    }

    private static int RunExtractSkillIcons(string[] args)
    {
        var options = ParseOptions(args);
        var install = UmaInstallLocator.Resolve(GetSingle(options, "--uma-dir"));
        var manifest = new ManifestDatabase(install.Path);
        var master = new MasterDataDatabase(install.Path);
        var exporter = new TextureBundleExporter(manifest);

        var skillIds = GatherValues(options, "--skill-ids");
        var iconIds = GatherValues(options, "--icon-ids");
        if (skillIds.Count == 0 && iconIds.Count == 0)
        {
            return Fail("extract-skill-icons expects --skill-ids and/or --icon-ids.");
        }

        var requestedSkillIds = skillIds
            .Select(static id => int.TryParse(id, out var parsed) ? parsed : throw new ArgumentException($"Invalid skill id '{id}'."))
            .ToArray();
        var requestedIconIds = iconIds
            .Select(static id => int.TryParse(id, out var parsed) ? parsed : throw new ArgumentException($"Invalid icon id '{id}'."))
            .ToHashSet();

        var resolvedSkillIconIds = master.GetSkillIconIds(requestedSkillIds);
        foreach (var iconId in resolvedSkillIconIds.Values)
        {
            requestedIconIds.Add(iconId);
        }

        var output = GetSingle(options, "--output")
            ?? Path.Combine(Environment.CurrentDirectory, "out", "skill-icons");
        var resourceNames = requestedIconIds
            .OrderBy(static id => id)
            .Select(static id => $"utx_ico_skill_{id}")
            .ToArray();
        var entries = manifest.FindMany(resourceNames).ToArray();
        var missingSkillIds = requestedSkillIds.Where(id => !resolvedSkillIconIds.ContainsKey(id)).ToArray();
        var missingResources = resourceNames.Except(entries.Select(static entry => entry.BaseName), StringComparer.OrdinalIgnoreCase).ToArray();

        foreach (var entry in entries.OrderBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase))
        {
            var results = exporter.ExportTextures(entry, output, [entry.BaseName]);
            foreach (var result in results)
            {
                Console.WriteLine($"{entry.BaseName}:{result.TextureName} -> {result.OutputPath}");
            }
        }

        if (missingSkillIds.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Unknown skill ids:");
            foreach (var skillId in missingSkillIds)
            {
                Console.WriteLine($"  {skillId}");
            }
        }

        if (missingResources.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Missing icon resources:");
            foreach (var resource in missingResources)
            {
                Console.WriteLine($"  {resource}");
            }
        }

        return entries.Length == 0 ? 1 : 0;
    }

    private static int RunExtractUiIcons(string[] args)
    {
        var options = ParseOptions(args);
        var output = GetSingle(options, "--output")
            ?? Path.Combine(Environment.CurrentDirectory, "out", "ui-icons");
        var requestedCatalogs = GatherValues(options, "--catalog");
        var definitions = AtlasUiCatalogDefinitions.GetDefinitions(requestedCatalogs);
        if (definitions.Count == 0)
        {
            return Fail($"No supported catalogs were requested. Supported: {string.Join(", ", AtlasUiCatalogDefinitions.SupportedCatalogs)}");
        }

        var targets = ResolveUiIconTargets(options, output, definitions);
        var console = new CliRunConsole();
        var appInfo = CliApplicationMetadata.Get();
        console.WriteBanner(appInfo);
        console.WriteMetadata(BuildUiIconMetadataLines(targets, definitions, output));

        var progress = console.BeginProgress(targets.Sum(static target => target.TotalRequestedIcons));
        var runSummary = new UiIconExtractionRunSummary();

        foreach (var target in targets)
        {
            var manifest = new ManifestDatabase(target.Install.Path);
            var spriteExporter = new SpriteBundleExporter(manifest);
            var catalogWriter = new SplitUiIconCatalogWriter();

            Directory.CreateDirectory(target.IconsRoot);
            Directory.CreateDirectory(target.CatalogsRoot);

            foreach (var definition in definitions)
            {
                var entry = manifest.FindByName(definition.AtlasName);
                if (entry is null)
                {
                    runSummary.Failures.Add($"[{target.Region}] missing atlas entry: {definition.AtlasName}");
                    progress.Advance($"{target.Region}/{definition.AtlasName} missing", definition.Icons.Count);
                    continue;
                }

                var spriteMap = definition.Icons.ToDictionary(
                    static icon => icon.SpriteName,
                    static icon => icon.SpriteName,
                    StringComparer.OrdinalIgnoreCase);
                var results = spriteExporter.ExportSprites(
                    entry,
                    target.IconsRoot,
                    spriteMap,
                    spriteName => progress.Advance($"{target.Region}/{definition.AtlasName}/{spriteName}"));
                if (results.Count == 0)
                {
                    runSummary.Failures.Add($"[{target.Region}] no matching sprites exported from {definition.AtlasName}");
                    progress.Advance($"{target.Region}/{definition.AtlasName} empty", definition.Icons.Count);
                    continue;
                }

                runSummary.ExportedIcons += results.Count;
                var catalogEntries = new List<UiIconCatalogEntry>(results.Count);
                var exportedSpriteNames = results
                    .Select(static result => result.SpriteName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var result in results)
                {
                    var iconDefinition = definition.Icons.First(icon => string.Equals(icon.SpriteName, result.SpriteName, StringComparison.OrdinalIgnoreCase));
                    var relativePath = Path.GetRelativePath(target.RegionRoot, result.OutputPath).Replace('\\', '/');
                    catalogEntries.Add(new UiIconCatalogEntry(
                        iconDefinition.Family,
                        iconDefinition.Key,
                        iconDefinition.Label,
                        result.SpriteName,
                        relativePath));
                }

                var missingCount = Math.Max(0, definition.Icons.Count - results.Count);
                if (missingCount > 0)
                {
                    var missingSpriteNames = definition.Icons
                        .Where(icon => !exportedSpriteNames.Contains(icon.SpriteName))
                        .Select(static icon => icon.SpriteName)
                        .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
                        .ToArray();
                    runSummary.Failures.Add(
                        $"[{target.Region}] {definition.CatalogName} exported {results.Count}/{definition.Icons.Count} icons; missing: {string.Join(", ", missingSpriteNames)}");
                    progress.Advance($"{target.Region}/{definition.AtlasName} partial", missingCount);
                }

                _ = catalogWriter.Write(target.CatalogsRoot, definition.CatalogName, catalogEntries);
                runSummary.CatalogsWritten++;
            }

            runSummary.Completed.Add($"[{target.Region}] {target.Install.Path} -> {target.RegionRoot}");
        }

        progress.Finish("complete");
        console.WriteSection("Failed:", runSummary.Failures);
        console.WriteSection("Done:", BuildUiIconDoneLines(runSummary, targets));

        return runSummary.ExportedIcons == 0 ? 1 : 0;
    }

    private static int RunExportRankBadges(string[] args)
    {
        var options = ParseOptions(args);
        var install = UmaInstallLocator.Resolve(GetSingle(options, "--uma-dir"));
        var manifest = new ManifestDatabase(install.Path);
        var output = GetSingle(options, "--output")
            ?? Path.Combine(Environment.CurrentDirectory, "out", "rank-badges");
        var atlasName = GetSingle(options, "--atlas-name") ?? "rank_tex";

        var written = new RankBadgeExporter(manifest).Export(output, atlasName);
        Console.WriteLine(written);
        return 0;
    }

    private static int RunExtractRawAtlases(string[] args)
    {
        var options = ParseOptions(args);
        var output = GetSingle(options, "--output")
            ?? Path.Combine(Environment.CurrentDirectory, "out", "raw-atlases");
        var requestedPresets = GatherValues(options, "--preset");
        var explicitAtlasNames = GatherValues(options, "--atlas");
        var targets = ResolveRegionTargets(options, output);

        var console = new CliRunConsole();
        console.WriteBanner(CliApplicationMetadata.Get());
        console.WriteMetadata(BuildRawAtlasMetadataLines(targets, requestedPresets, explicitAtlasNames, output));

        var discoveredCounts = new List<int>();
        foreach (var target in targets)
        {
            var manifest = new ManifestDatabase(target.Install.Path);
            var atlasNames = RawAtlasExportDefinitions.ResolveAtlasNames(manifest, requestedPresets, explicitAtlasNames);
            discoveredCounts.Add(atlasNames.Count);
        }

        var progress = console.BeginProgress(discoveredCounts.Sum());
        var spriteIndexWriter = new RawAtlasIndexWriter();
        var failures = new List<string>();
        var done = new List<string>();
        var atlasExportCount = 0;
        var spriteExportCount = 0;
        foreach (var target in targets)
        {
            var manifest = new ManifestDatabase(target.Install.Path);
            var spriteExporter = new SpriteBundleExporter(manifest);
            var atlasNames = RawAtlasExportDefinitions.ResolveAtlasNames(manifest, requestedPresets, explicitAtlasNames);
            var indexEntries = new List<RawAtlasIndexEntry>();

            foreach (var atlasName in atlasNames)
            {
                var entry = manifest.FindByName(atlasName);
                if (entry is null)
                {
                    failures.Add($"[{target.Region}] missing atlas entry: {atlasName}");
                    progress.Advance($"{target.Region}/{atlasName} missing");
                    continue;
                }

                var atlasOutputRoot = Path.Combine(target.RegionRoot, "raw-atlases", atlasName);
                try
                {
                    var results = spriteExporter.ExportSprites(entry, atlasOutputRoot);
                    atlasExportCount++;
                    spriteExportCount += results.Count;

                    foreach (var result in results)
                    {
                        var relativePath = Path.GetRelativePath(target.RegionRoot, result.OutputPath).Replace('\\', '/');
                        indexEntries.Add(new RawAtlasIndexEntry(target.Region, atlasName, result.SpriteName, relativePath));
                    }

                    if (results.Count == 0)
                    {
                        failures.Add($"[{target.Region}] {atlasName} exported no sprites");
                    }
                }
                catch (Exception ex)
                {
                    failures.Add($"[{target.Region}] {atlasName} failed: {ex.Message}");
                }

                progress.Advance($"{target.Region}/{atlasName}");
            }

            var indexPath = spriteIndexWriter.Write(target.RegionRoot, indexEntries);
            done.Add($"[{target.Region}] raw atlases: {atlasNames.Count}, sprites: {indexEntries.Count}, index: {indexPath}");
        }

        progress.Finish("complete");
        console.WriteSection("Failed:", failures);
        console.WriteSection("Done:", BuildRawAtlasDoneLines(done, atlasExportCount, spriteExportCount));
        return atlasExportCount == 0 ? 1 : 0;
    }

    private static int RunSyncAll(string[] args)
    {
        var options = ParseOptions(args);
        var output = GetSingle(options, "--output")
            ?? Path.Combine(Environment.CurrentDirectory, "out", "sync-all");
        var includeGameTora = options.ContainsKey("--with-gametora");
        var targets = ResolveRegionTargets(options, output);
        var uiDefinitions = AtlasUiCatalogDefinitions.GetDefinitions(["all"]);

        var console = new CliRunConsole();
        console.WriteBanner(CliApplicationMetadata.Get());
        console.WriteMetadata([
            $"Game Region: {string.Join(", ", targets.Select(static target => target.Region))}",
            "Extraction targets: character icons, support images, skill icons, local skill catalog, curated ui icons, raw atlases",
            $"GameTora enrichment: {(includeGameTora ? "enabled" : "disabled")}",
            $"Output root: {output}",
            "Game Files Path:",
            .. targets.Select(static target => $"  [{target.Region}] {target.Install.Path}")
        ]);

        var totalSteps = targets.Count * 6;
        var progress = console.BeginProgress(totalSteps);
        var failures = new List<string>();
        var done = new List<string>();
        var regionReports = new List<SyncAllRegionReport>();

        foreach (var target in targets)
        {
            var manifest = new ManifestDatabase(target.Install.Path);
            var textureExporter = new TextureBundleExporter(manifest);
            var spriteExporter = new SpriteBundleExporter(manifest);
            var master = new MasterDataDatabase(target.Install.Path);

            Directory.CreateDirectory(target.RegionRoot);

            var characterEntries = manifest.SearchByPrefix(["chr_icon_", "trained_chr_icon_", "chr_icon_round_", "chr_icon_plus_"]);
            foreach (var entry in characterEntries)
            {
                if (CharaIconPathParser.Parse(entry.BaseName) is null)
                {
                    continue;
                }

                try
                {
                    textureExporter.ExportTextures(entry, target.RegionRoot, [entry.BaseName]);
                }
                catch (Exception ex)
                {
                    failures.Add($"[{target.Region}] character {entry.BaseName} failed: {ex.Message}");
                }
            }

            var characterManifestPath = AssetManifestGenerator.Write(
                target.RegionRoot,
                Path.Combine(target.RegionRoot, "catalogs", "character-icons.json"));
            progress.Advance($"{target.Region}/characters");

            var supportEntries = manifest.SearchByPrefix(["support_thumb_", "support_card_s_", "tex_support_card_"]);
            foreach (var entry in supportEntries)
            {
                try
                {
                    textureExporter.ExportTextures(entry, target.RegionRoot, [entry.BaseName]);
                }
                catch (Exception ex)
                {
                    failures.Add($"[{target.Region}] support {entry.BaseName} failed: {ex.Message}");
                }
            }

            var supportManifestPath = SupportAssetManifestGenerator.Write(
                target.RegionRoot,
                Path.Combine(target.RegionRoot, "catalogs", "support-icons.json"));
            progress.Advance($"{target.Region}/supports");

            var skillRecords = master.GetAllSkills();
            var skillResourceNames = skillRecords
                .Select(static skill => $"utx_ico_skill_{skill.IconId}")
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var skillEntries = manifest.FindMany(skillResourceNames);
            foreach (var entry in skillEntries)
            {
                try
                {
                    textureExporter.ExportTextures(entry, target.RegionRoot, [entry.BaseName]);
                }
                catch (Exception ex)
                {
                    failures.Add($"[{target.Region}] skill icon {entry.BaseName} failed: {ex.Message}");
                }
            }

            var skillCatalogPath = SkillCatalogGenerator.Write(
                skillRecords,
                target.RegionRoot,
                Path.Combine(target.RegionRoot, "catalogs", "skill-catalog.json"));
            progress.Advance($"{target.Region}/skills");

            var uiCatalogWriter = new SplitUiIconCatalogWriter();
            var uiIconsRoot = Path.Combine(target.RegionRoot, "icons");
            var uiCatalogsRoot = Path.Combine(target.RegionRoot, "catalogs");
            var uiCatalogCount = 0;
            foreach (var definition in uiDefinitions)
            {
                var entry = manifest.FindByName(definition.AtlasName);
                if (entry is null)
                {
                    failures.Add($"[{target.Region}] missing curated atlas entry: {definition.AtlasName}");
                    continue;
                }

                var spriteMap = definition.Icons.ToDictionary(
                    static icon => icon.SpriteName,
                    static icon => icon.SpriteName,
                    StringComparer.OrdinalIgnoreCase);
                var results = spriteExporter.ExportSprites(entry, uiIconsRoot, spriteMap);
                var catalogEntries = results
                    .Select(result =>
                    {
                        var definitionEntry = definition.Icons.First(icon => string.Equals(icon.SpriteName, result.SpriteName, StringComparison.OrdinalIgnoreCase));
                        var relativePath = Path.GetRelativePath(target.RegionRoot, result.OutputPath).Replace('\\', '/');
                        return new UiIconCatalogEntry(definitionEntry.Family, definitionEntry.Key, definitionEntry.Label, result.SpriteName, relativePath);
                    })
                    .ToArray();
                _ = uiCatalogWriter.Write(uiCatalogsRoot, definition.CatalogName, catalogEntries);
                uiCatalogCount++;
            }

            progress.Advance($"{target.Region}/ui");

            var rawAtlasNames = RawAtlasExportDefinitions.ResolveAtlasNames(manifest, ["extras", "honor", "scenario"]);
            var rawAtlasEntries = new List<RawAtlasIndexEntry>();
            foreach (var atlasName in rawAtlasNames)
            {
                var entry = manifest.FindByName(atlasName);
                if (entry is null)
                {
                    continue;
                }

                try
                {
                    var rawOutputRoot = Path.Combine(target.RegionRoot, "raw-atlases", atlasName);
                    var results = spriteExporter.ExportSprites(entry, rawOutputRoot);
                    rawAtlasEntries.AddRange(results.Select(result =>
                        new RawAtlasIndexEntry(
                            target.Region,
                            atlasName,
                            result.SpriteName,
                            Path.GetRelativePath(target.RegionRoot, result.OutputPath).Replace('\\', '/'))));
                }
                catch (Exception ex)
                {
                    failures.Add($"[{target.Region}] raw atlas {atlasName} failed: {ex.Message}");
                }
            }

            _ = new RawAtlasIndexWriter().Write(target.RegionRoot, rawAtlasEntries);
            progress.Advance($"{target.Region}/raw-atlases");

            if (includeGameTora)
            {
                var gameToraOutput = Path.Combine(target.RegionRoot, "external", "gametora");
                var cacheDirectory = Path.Combine(Environment.CurrentDirectory, ".cache_gametora", target.Region);
                var server = string.Equals(target.Region, "japan", StringComparison.OrdinalIgnoreCase) ? "japan" : "global";
                _ = new GameToraCatalogSync(cacheDirectory, noFetch: false)
                    .SyncAsync(gameToraOutput, includeSupports: true, server)
                    .GetAwaiter()
                    .GetResult();
            }

            progress.Advance($"{target.Region}/metadata");

            var characterManifest = AssetManifestGenerator.Build(target.RegionRoot);
            var supportManifest = SupportAssetManifestGenerator.Build(target.RegionRoot);
            var regionReport = new SyncAllRegionReport
            {
                Region = target.Region,
                UmaDir = target.Install.Path,
                CharacterCount = characterManifest.Characters.Count,
                SupportCount = supportManifest.Supports.Count,
                SkillCount = skillRecords.Count,
                UiCatalogCount = uiCatalogCount,
                RawAtlasCount = rawAtlasNames.Count,
                RawAtlasSpriteCount = rawAtlasEntries.Count,
                CharacterManifestPath = Path.GetRelativePath(output, characterManifestPath).Replace('\\', '/'),
                SupportManifestPath = Path.GetRelativePath(output, supportManifestPath).Replace('\\', '/'),
                SkillCatalogPath = Path.GetRelativePath(output, skillCatalogPath).Replace('\\', '/'),
            };
            regionReports.Add(regionReport);
            done.Add($"[{target.Region}] characters: {regionReport.CharacterCount}, supports: {regionReport.SupportCount}, skills: {regionReport.SkillCount}, raw atlases: {regionReport.RawAtlasCount}");
        }

        var report = new SyncAllReport
        {
            GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
            Regions = regionReports,
            CrossRegion = BuildCrossRegionReport(targets, regionReports, output),
        };
        var reportPath = SyncAllReportWriter.Write(report, Path.Combine(output, "sync-summary.json"));
        done.Add($"summary: {reportPath}");

        progress.Finish("complete");
        console.WriteSection("Failed:", failures);
        console.WriteSection("Done:", done);
        return failures.Count > 0 ? 1 : 0;
    }

    private static Dictionary<string, List<string>> ParseOptions(string[] args)
    {
        var options = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        string? currentKey = null;

        foreach (var arg in args)
        {
            if (arg.StartsWith("--", StringComparison.Ordinal))
            {
                currentKey = arg;
                if (!options.ContainsKey(currentKey))
                {
                    options[currentKey] = [];
                }

                continue;
            }

            if (currentKey is null)
            {
                throw new ArgumentException($"Unexpected argument '{arg}'.");
            }

            options[currentKey].Add(arg);
        }

        return options;
    }

    private static List<string> GatherValues(Dictionary<string, List<string>> options, string key)
    {
        return options.TryGetValue(key, out var values) ? [.. values] : [];
    }

    private static string? GetSingle(Dictionary<string, List<string>> options, string key)
    {
        return options.TryGetValue(key, out var values) && values.Count > 0 ? values[^1] : null;
    }

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value, out var parsed) ? parsed : fallback;
    }

    private static bool IsHelp(string arg)
    {
        return arg is "help" or "--help" or "-h" or "/?";
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine($"error: {message}");
        Console.Error.WriteLine();
        PrintHelp();
        return 1;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("UmaAssetCli");
        Console.WriteLine();
        Console.WriteLine("Commands");
        Console.WriteLine("  detect");
        Console.WriteLine("    Detect local Umamusume data installs.");
        Console.WriteLine();
        Console.WriteLine("  sync-gametora [--output <dir>] [--cache-dir <dir>] [--no-fetch] [--include-supports] [--server <global|japan>]");
        Console.WriteLine("    Fetch GameTora metadata catalogs for characters and skills.");
        Console.WriteLine("    Supports are optional and only included when --include-supports is passed.");
        Console.WriteLine();
        Console.WriteLine("  lookup --name <resource> [--name <resource> ...] [--uma-dir <path>] [--json]");
        Console.WriteLine("    Resolve manifest entries by full resource name or basename.");
        Console.WriteLine();
        Console.WriteLine("  search --contains <text> [--contains <text> ...] [--limit <n>] [--uma-dir <path>] [--json]");
        Console.WriteLine("    Search manifest entries by case-insensitive substring.");
        Console.WriteLine();
        Console.WriteLine("  inspect-bundle --name <resource> [--name <resource> ...] [--uma-dir <path>]");
        Console.WriteLine("    List named assets inside matching bundle files.");
        Console.WriteLine();
        Console.WriteLine("  dump-asset --name <resource> --asset-name <name> [--uma-dir <path>]");
        Console.WriteLine("    Dump the field tree for a named asset inside a bundle.");
        Console.WriteLine();
        Console.WriteLine("  stage --name <resource> [--name <resource> ...] [--output <dir>] [--decrypt] [--flatten] [--uma-dir <path>]");
        Console.WriteLine("    Copy matching bundle blobs into an output folder. Use --decrypt to write a decrypted bundle when required.");
        Console.WriteLine();
        Console.WriteLine("  stage-chara-icons --ids <id> [<id> ...] [--plate <n>] [--output <dir>] [--decrypt] [--flatten] [--uma-dir <path>]");
        Console.WriteLine("    Resolve character icon bundle names from IDs.");
        Console.WriteLine("    4-digit ids are base icons, 6-digit ids are trained/dress icons.");
        Console.WriteLine("    Repeat --family with chr, trained, round, plus to include multiple icon families.");
        Console.WriteLine();
        Console.WriteLine("  extract-textures --name <resource> [--texture-name <name> ...] [--output <dir>] [--uma-dir <path>]");
        Console.WriteLine("    Load bundle files and export matching Texture2D assets as PNG files.");
        Console.WriteLine();
        Console.WriteLine("  extract-sprites --name <resource> [--sprite-name <name> ...] [--output <dir>] [--uma-dir <path>]");
        Console.WriteLine("    Load bundle files and export matching Sprite assets as cropped PNG files.");
        Console.WriteLine();
        Console.WriteLine("  extract-chara-icons --ids <id> [<id> ...] [--plate <n>] [--output <dir>] [--uma-dir <path>]");
        Console.WriteLine("    Resolve character icon bundle names from IDs and export Texture2D PNGs.");
        Console.WriteLine("    Repeat --family with chr, trained, round, plus to include multiple icon families.");
        Console.WriteLine();
        Console.WriteLine("  extract-support-icons --ids <id> [<id> ...] [--output <dir>] [--uma-dir <path>]");
        Console.WriteLine("    Export support thumbs, small support cards, and full support card art PNGs from local assets.");
        Console.WriteLine();
        Console.WriteLine("  extract-skill-icons [--skill-ids <id> ...] [--icon-ids <id> ...] [--output <dir>] [--uma-dir <path>]");
        Console.WriteLine("    Export local skill icon PNGs. Skill ids are resolved through master.mdb to icon ids.");
        Console.WriteLine();
        Console.WriteLine("  extract-ui-icons [--catalog <name> ...] [--output <dir>] [--uma-dir <path>] [--region <global|japan>] [--global-dir <path>] [--japan-dir <path>]");
        Console.WriteLine("    Export curated atlas-backed UI icon catalogs and flattened regional PNG assets.");
        Console.WriteLine("    Use --global-dir and --japan-dir together to process both installs in one run.");
        Console.WriteLine();
        Console.WriteLine("  extract-raw-atlases [--preset <extras|honor|scenario> ...] [--atlas <name> ...] [--output <dir>] [--uma-dir <path>] [--global-dir <path>] [--japan-dir <path>]");
        Console.WriteLine("    Export raw atlas sprite bundles and write a generated raw-atlas-index.json for browsing/downloading.");
        Console.WriteLine("    Defaults to the extras, honor, and scenario presets when no --preset is passed.");
        Console.WriteLine();
        Console.WriteLine("  sync-all [--output <dir>] [--uma-dir <path>] [--global-dir <path>] [--japan-dir <path>] [--with-gametora]");
        Console.WriteLine("    Extract all currently supported assets and write manifests/catalogs for the app.");
        Console.WriteLine("    Includes character icons, support images, skill icons/catalog, curated UI icons, raw atlases, and a sync summary.");
        Console.WriteLine();
        Console.WriteLine("  export-rank-badges [--output <dir>] [--atlas-name <name>] [--uma-dir <path>]");
        Console.WriteLine("    Export cropped rank badge PNGs from the local rank atlas.");
        Console.WriteLine();
        Console.WriteLine("  generate-manifest [--input <dir>] [--output <file>]");
        Console.WriteLine("    Scan organized extracted assets and write a JSON manifest keyed by character id.");
        Console.WriteLine();
        Console.WriteLine("Examples");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- detect");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- sync-gametora --output .\out\gametora --include-supports");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- lookup --name chr_icon_1058_105801_02 --json");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- search --contains strategy --contains motivation --limit 50");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- inspect-bundle --name pf_fl_race_run_style_setting00");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- dump-asset --name rank_tex --asset-name utx_txt_rank_00");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- stage-chara-icons --ids 1058 105801 --decrypt --output .\out\icons");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- extract-chara-icons --ids 1058 105801 --output .\out\png");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- extract-sprites --name rank_tex --sprite-name utx_txt_rank_00 --output .\out\sprites");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- extract-chara-icons --ids 1058 105801 --family chr --family trained --output .\out\organized");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- extract-support-icons --ids 30001 --output .\out\support-icons");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- extract-skill-icons --skill-ids 20011 20145 --output .\out\skill-icons");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- extract-ui-icons --catalog ui-common-icons --output .\out\ui-icons");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- extract-ui-icons --catalog ui-common-icons --uma-dir C:\temp\uma-copy --region japan");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- extract-ui-icons --global-dir %USERPROFILE%\AppData\LocalLow\Cygames\umamusume --japan-dir D:\Games\UmamusumePrettyDerby_Jpn_Data\Persistent --output .\out\ui");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- extract-raw-atlases --global-dir %USERPROFILE%\AppData\LocalLow\Cygames\umamusume --japan-dir D:\Games\UmamusumePrettyDerby_Jpn_Data\Persistent --output .\out\raw-atlases");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- sync-all --global-dir %USERPROFILE%\AppData\LocalLow\Cygames\umamusume --japan-dir D:\Games\UmamusumePrettyDerby_Jpn_Data\Persistent --output .\out\sync-all");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- export-rank-badges --output .\out\rank-badges");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- generate-manifest --input .\out\organized --output .\out\organized\character-icons.json");
    }

    private static IReadOnlyList<UiIconExtractionTarget> ResolveRegionTargets(
        Dictionary<string, List<string>> options,
        string output)
    {
        var explicitUmaDirs = GatherValues(options, "--uma-dir");
        var globalDir = GetSingle(options, "--global-dir");
        var japanDir = GetSingle(options, "--japan-dir");

        if (explicitUmaDirs.Count > 1)
        {
            throw new ArgumentException("This command accepts at most one --uma-dir. Use --global-dir and --japan-dir for dual-region runs.");
        }

        if (explicitUmaDirs.Count > 0 && (!string.IsNullOrWhiteSpace(globalDir) || !string.IsNullOrWhiteSpace(japanDir)))
        {
            throw new ArgumentException("Do not combine --uma-dir with --global-dir/--japan-dir.");
        }

        var targets = new List<UiIconExtractionTarget>();
        if (!string.IsNullOrWhiteSpace(globalDir))
        {
            var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(globalDir));
            UmaInstallLocator.EnsureValid(fullPath);
            targets.Add(new UiIconExtractionTarget("global", new UmaInstall("Explicit (Global)", fullPath), output, 0));
        }

        if (!string.IsNullOrWhiteSpace(japanDir))
        {
            var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(japanDir));
            UmaInstallLocator.EnsureValid(fullPath);
            targets.Add(new UiIconExtractionTarget("japan", new UmaInstall("Explicit (Japan)", fullPath), output, 0));
        }

        if (targets.Count > 0)
        {
            return targets;
        }

        if (explicitUmaDirs.Count == 1)
        {
            var install = UmaInstallLocator.Resolve(explicitUmaDirs[0]);
            return [new UiIconExtractionTarget(GetRegionSlug(options, install), install, output, 0)];
        }

        var detected = UmaInstallLocator.DetectAll();
        if (detected.Count == 0)
        {
            throw new DirectoryNotFoundException("No Umamusume installs were detected. Pass --uma-dir, --global-dir, or --japan-dir.");
        }

        return detected
            .Select(install => new UiIconExtractionTarget(GetRegionSlug(options, install), install, output, 0))
            .GroupBy(static target => target.Region, StringComparer.OrdinalIgnoreCase)
            .Select(static group => group.First())
            .OrderBy(static target => target.Region, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<UiIconExtractionTarget> ResolveUiIconTargets(
        Dictionary<string, List<string>> options,
        string output,
        IReadOnlyList<AtlasUiCatalogDefinition> definitions)
    {
        var totalRequestedIcons = definitions.Sum(static definition => definition.Icons.Count);
        return ResolveRegionTargets(options, output)
            .Select(target => target with { TotalRequestedIcons = totalRequestedIcons })
            .ToArray();
    }

    private static IReadOnlyList<string> BuildUiIconMetadataLines(
        IReadOnlyList<UiIconExtractionTarget> targets,
        IReadOnlyList<AtlasUiCatalogDefinition> definitions,
        string output)
    {
        var lines = new List<string>
        {
            $"Game Region: {string.Join(", ", targets.Select(static target => target.Region))}",
            $"Extraction targets: {string.Join(", ", definitions.Select(static definition => definition.CatalogName))}",
            $"Output root: {output}",
            "Temp staging: enabled",
            "Game Files Path:",
        };

        lines.AddRange(targets.Select(static target => $"  [{target.Region}] {target.Install.Path}"));
        return lines;
    }

    private static IReadOnlyCollection<string> BuildUiIconDoneLines(
        UiIconExtractionRunSummary runSummary,
        IReadOnlyList<UiIconExtractionTarget> targets)
    {
        var lines = new List<string>
        {
            $"Icons exported: {runSummary.ExportedIcons}",
            $"Catalogs written: {runSummary.CatalogsWritten}",
        };

        lines.AddRange(runSummary.Completed);
        if (targets.Count > 1)
        {
            lines.Add($"Regions processed: {string.Join(", ", targets.Select(static target => target.Region))}");
        }

        return lines;
    }

    private static IReadOnlyList<string> BuildRawAtlasMetadataLines(
        IReadOnlyList<UiIconExtractionTarget> targets,
        IReadOnlyList<string> requestedPresets,
        IReadOnlyList<string> explicitAtlasNames,
        string output)
    {
        var presetLabel = requestedPresets.Count == 0
            ? string.Join(", ", RawAtlasExportDefinitions.SupportedPresets)
            : string.Join(", ", requestedPresets);
        var atlasLabel = explicitAtlasNames.Count == 0 ? "none" : string.Join(", ", explicitAtlasNames);

        var lines = new List<string>
        {
            $"Game Region: {string.Join(", ", targets.Select(static target => target.Region))}",
            $"Raw atlas presets: {presetLabel}",
            $"Explicit atlases: {atlasLabel}",
            $"Output root: {output}",
            "Temp staging: enabled",
            "Game Files Path:",
        };

        lines.AddRange(targets.Select(static target => $"  [{target.Region}] {target.Install.Path}"));
        return lines;
    }

    private static IReadOnlyCollection<string> BuildRawAtlasDoneLines(
        IReadOnlyList<string> targetSummaries,
        int atlasExportCount,
        int spriteExportCount)
    {
        var lines = new List<string>
        {
            $"Atlases exported: {atlasExportCount}",
            $"Sprites exported: {spriteExportCount}",
        };
        lines.AddRange(targetSummaries);
        return lines;
    }

    private static SyncAllCrossRegionReport? BuildCrossRegionReport(
        IReadOnlyList<UiIconExtractionTarget> targets,
        IReadOnlyList<SyncAllRegionReport> regionReports,
        string outputRoot)
    {
        if (targets.Count < 2)
        {
            return null;
        }

        var global = regionReports.FirstOrDefault(static report => string.Equals(report.Region, "global", StringComparison.OrdinalIgnoreCase));
        var japan = regionReports.FirstOrDefault(static report => string.Equals(report.Region, "japan", StringComparison.OrdinalIgnoreCase));
        if (global is null || japan is null)
        {
            return null;
        }

        var globalCharacterManifest = AssetManifestGenerator.Build(Path.Combine(outputRoot, "global"));
        var japanCharacterManifest = AssetManifestGenerator.Build(Path.Combine(outputRoot, "japan"));
        var globalSupportManifest = SupportAssetManifestGenerator.Build(Path.Combine(outputRoot, "global"));
        var japanSupportManifest = SupportAssetManifestGenerator.Build(Path.Combine(outputRoot, "japan"));
        var globalSkills = JsonSerializer.Deserialize<GeneratedSkillCatalog>(File.ReadAllText(Path.Combine(outputRoot, global.SkillCatalogPath.Replace('/', Path.DirectorySeparatorChar)))) ?? new GeneratedSkillCatalog();
        var japanSkills = JsonSerializer.Deserialize<GeneratedSkillCatalog>(File.ReadAllText(Path.Combine(outputRoot, japan.SkillCatalogPath.Replace('/', Path.DirectorySeparatorChar)))) ?? new GeneratedSkillCatalog();

        var globalCharacterIds = globalCharacterManifest.Characters.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var japanCharacterIds = japanCharacterManifest.Characters.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var globalSupportIds = globalSupportManifest.Supports.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var japanSupportIds = japanSupportManifest.Supports.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var globalSkillIds = globalSkills.Skills.Select(static skill => skill.SkillId).ToHashSet();
        var japanSkillIds = japanSkills.Skills.Select(static skill => skill.SkillId).ToHashSet();

        return new SyncAllCrossRegionReport
        {
            SharedCharacterCount = globalCharacterIds.Intersect(japanCharacterIds, StringComparer.OrdinalIgnoreCase).Count(),
            SharedSupportCount = globalSupportIds.Intersect(japanSupportIds, StringComparer.OrdinalIgnoreCase).Count(),
            SharedSkillCount = globalSkillIds.Intersect(japanSkillIds).Count(),
            GlobalOnlyCharacterCount = globalCharacterIds.Except(japanCharacterIds, StringComparer.OrdinalIgnoreCase).Count(),
            JapanOnlyCharacterCount = japanCharacterIds.Except(globalCharacterIds, StringComparer.OrdinalIgnoreCase).Count(),
            GlobalOnlySupportCount = globalSupportIds.Except(japanSupportIds, StringComparer.OrdinalIgnoreCase).Count(),
            JapanOnlySupportCount = japanSupportIds.Except(globalSupportIds, StringComparer.OrdinalIgnoreCase).Count(),
            GlobalOnlySkillCount = globalSkillIds.Except(japanSkillIds).Count(),
            JapanOnlySkillCount = japanSkillIds.Except(globalSkillIds).Count(),
        };
    }

    private static string GetRegionSlug(Dictionary<string, List<string>> options, UmaInstall install)
    {
        var explicitRegion = GetSingle(options, "--region");
        if (string.Equals(explicitRegion, "global", StringComparison.OrdinalIgnoreCase)
            || string.Equals(explicitRegion, "japan", StringComparison.OrdinalIgnoreCase))
        {
            return explicitRegion!.ToLowerInvariant();
        }

        return install.Name.Contains("Japan", StringComparison.OrdinalIgnoreCase)
               || install.Path.Contains("_Jpn", StringComparison.OrdinalIgnoreCase)
               || install.Path.Contains("PrettyDerby_Jpn", StringComparison.OrdinalIgnoreCase)
            ? "japan"
            : "global";
    }

    private sealed record UiIconExtractionTarget(string Region, UmaInstall Install, string OutputRoot, int TotalRequestedIcons)
    {
        public string RegionRoot => Path.Combine(OutputRoot, Region);
        public string IconsRoot => Path.Combine(RegionRoot, "icons");
        public string CatalogsRoot => Path.Combine(RegionRoot, "catalogs");
    }

    private sealed class UiIconExtractionRunSummary
    {
        public int ExportedIcons { get; set; }
        public int CatalogsWritten { get; set; }
        public List<string> Failures { get; } = [];
        public List<string> Completed { get; } = [];
    }
}
