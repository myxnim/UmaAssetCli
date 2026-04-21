using System.Text.Json;

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
                "extract-chara-icons" => RunExtractTextures(args[1..], treatIdsAsCharaIcons: true),
                "extract-support-icons" => RunExtractSupportIcons(args[1..]),
                "extract-skill-icons" => RunExtractSkillIcons(args[1..]),
                "extract-ui-icons" => RunExtractUiIcons(args[1..]),
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
        var install = UmaInstallLocator.Resolve(GetSingle(options, "--uma-dir"));
        var manifest = new ManifestDatabase(install.Path);
        var exporter = new TextureBundleExporter(manifest);

        var families = GatherValues(options, "--family");
        if (families.Count == 0)
        {
            return Fail($"extract-ui-icons expects at least one --family value. Supported: {string.Join(", ", UiIconResourceNames.SupportedFamilies)}");
        }

        var output = GetSingle(options, "--output")
            ?? Path.Combine(Environment.CurrentDirectory, "out", "ui-icons");
        var requestedFamilies = families
            .Where(static family => !string.IsNullOrWhiteSpace(family))
            .Select(static family => family.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var resourceNames = UiIconResourceNames.ExpandFamilies(requestedFamilies);
        if (resourceNames.Count == 0)
        {
            return Fail($"No supported families were requested. Supported: {string.Join(", ", UiIconResourceNames.SupportedFamilies)}");
        }

        var entries = manifest.FindMany(resourceNames).ToArray();
        var missing = resourceNames.Except(entries.Select(static entry => entry.BaseName), StringComparer.OrdinalIgnoreCase).ToArray();
        var catalogEntries = new List<UiIconCatalogEntry>();

        foreach (var entry in entries.OrderBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase))
        {
            var results = exporter.ExportTextures(entry, output, [entry.BaseName]);
            foreach (var result in results)
            {
                Console.WriteLine($"{entry.BaseName}:{result.TextureName} -> {result.OutputPath}");

                var family = requestedFamilies.FirstOrDefault(family =>
                    UiIconResourceNames.GetFriendlyNames(family).ContainsKey(entry.BaseName));
                if (family is null)
                {
                    continue;
                }

                var label = UiIconResourceNames.GetFriendlyNames(family).TryGetValue(entry.BaseName, out var friendlyName)
                    ? friendlyName
                    : entry.BaseName;
                var relativePath = Path.GetRelativePath(output, result.OutputPath).Replace('\\', '/');
                catalogEntries.Add(new UiIconCatalogEntry(
                    family,
                    entry.BaseName,
                    label,
                    entry.Name,
                    relativePath));
            }
        }

        if (catalogEntries.Count > 0)
        {
            var catalogPath = new UiIconCatalogWriter().Write(output, catalogEntries);
            Console.WriteLine($"catalog -> {catalogPath}");
        }

        if (missing.Length > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Missing UI icon resources:");
            foreach (var resource in missing)
            {
                Console.WriteLine($"  {resource}");
            }
        }

        return entries.Length == 0 ? 1 : 0;
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
        Console.WriteLine("  extract-chara-icons --ids <id> [<id> ...] [--plate <n>] [--output <dir>] [--uma-dir <path>]");
        Console.WriteLine("    Resolve character icon bundle names from IDs and export Texture2D PNGs.");
        Console.WriteLine("    Repeat --family with chr, trained, round, plus to include multiple icon families.");
        Console.WriteLine();
        Console.WriteLine("  extract-support-icons --ids <id> [<id> ...] [--output <dir>] [--uma-dir <path>]");
        Console.WriteLine("    Export support card thumbnail PNGs from local assets.");
        Console.WriteLine();
        Console.WriteLine("  extract-skill-icons [--skill-ids <id> ...] [--icon-ids <id> ...] [--output <dir>] [--uma-dir <path>]");
        Console.WriteLine("    Export local skill icon PNGs. Skill ids are resolved through master.mdb to icon ids.");
        Console.WriteLine();
        Console.WriteLine("  extract-ui-icons --family <motivation|charastatus> [--family ...] [--output <dir>] [--uma-dir <path>]");
        Console.WriteLine("    Export selected direct UI icon families from local assets.");
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
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- extract-chara-icons --ids 1058 105801 --family chr --family trained --output .\out\organized");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- extract-support-icons --ids 30001 --output .\out\support-icons");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- extract-skill-icons --skill-ids 20011 20145 --output .\out\skill-icons");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- extract-ui-icons --family motivation --output .\out\ui-icons");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- export-rank-badges --output .\out\rank-badges");
        Console.WriteLine(@"  dotnet run --project .\src\UmaAsset.Cli -- generate-manifest --input .\out\organized --output .\out\organized\character-icons.json");
    }
}
