using System.Text.Json;
using UmaAssetCli.Models;
using UmaAssetCli.Services;

namespace UmaAssetCli.Commands;

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
                "lookup" => RunLookup(args[1..]),
                "stage" => RunStage(args[1..], treatIdsAsCharaIcons: false),
                "stage-chara-icons" => RunStage(args[1..], treatIdsAsCharaIcons: true),
                "extract-textures" => RunExtractTextures(args[1..], treatIdsAsCharaIcons: false),
                "extract-chara-icons" => RunExtractTextures(args[1..], treatIdsAsCharaIcons: true),
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
            if (ids.Count == 0)
            {
                return Fail("stage-chara-icons expects at least one value after --ids.");
            }

            requestedNames.AddRange(CharaIconResourceNames.FromIds(ids, plate));
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
            if (ids.Count == 0)
            {
                return Fail("extract-chara-icons expects at least one value after --ids.");
            }

            requestedNames.AddRange(CharaIconResourceNames.FromIds(ids, plate));
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
        Console.WriteLine("  lookup --name <resource> [--name <resource> ...] [--uma-dir <path>] [--json]");
        Console.WriteLine("    Resolve manifest entries by full resource name or basename.");
        Console.WriteLine();
        Console.WriteLine("  stage --name <resource> [--name <resource> ...] [--output <dir>] [--decrypt] [--flatten] [--uma-dir <path>]");
        Console.WriteLine("    Copy matching bundle blobs into an output folder. Use --decrypt to write a decrypted bundle when required.");
        Console.WriteLine();
        Console.WriteLine("  stage-chara-icons --ids <id> [<id> ...] [--plate <n>] [--output <dir>] [--decrypt] [--flatten] [--uma-dir <path>]");
        Console.WriteLine("    Resolve character icon bundle names from IDs.");
        Console.WriteLine("    4-digit ids are base icons, 6-digit ids are trained/dress icons.");
        Console.WriteLine();
        Console.WriteLine("  extract-textures --name <resource> [--texture-name <name> ...] [--output <dir>] [--uma-dir <path>]");
        Console.WriteLine("    Load bundle files and export matching Texture2D assets as PNG files.");
        Console.WriteLine();
        Console.WriteLine("  extract-chara-icons --ids <id> [<id> ...] [--plate <n>] [--output <dir>] [--uma-dir <path>]");
        Console.WriteLine("    Resolve character icon bundle names from IDs and export Texture2D PNGs.");
        Console.WriteLine();
        Console.WriteLine("Examples");
        Console.WriteLine(@"  dotnet run --project UmaAssetCli -- detect");
        Console.WriteLine(@"  dotnet run --project UmaAssetCli -- lookup --name chr_icon_1058_105801_02 --json");
        Console.WriteLine(@"  dotnet run --project UmaAssetCli -- stage-chara-icons --ids 1058 105801 --decrypt --output .\out\icons");
        Console.WriteLine(@"  dotnet run --project UmaAssetCli -- extract-chara-icons --ids 1058 105801 --output .\out\png");
    }
}
