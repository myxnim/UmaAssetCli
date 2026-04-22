using System.Text.Json.Nodes;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace UmaAsset.Game.Services;

public static class UmaInstallLocator
{
    public static IReadOnlyList<UmaInstall> DetectAll()
    {
        return DetectReport().Installs;
    }

    public static UmaDetectionReport DetectReport()
    {
        var installs = new List<UmaInstall>();
        var probes = new List<UmaDetectionProbe>();
        var localLow = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData",
            "LocalLow",
            "Cygames");

        AddProbe(
            installs,
            probes,
            "Default (AppData)",
            "global",
            "LocalLow",
            Path.Combine(localLow, "umamusume"));

        AddProbe(
            installs,
            probes,
            "Default (AppData Japan)",
            "japan",
            "LocalLow",
            Path.Combine(localLow, "UmamusumePrettyDerby_Jpn"));

        var dmmConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "dmmgameplayer5",
            "dmmgame.cnf");
        probes.Add(new UmaDetectionProbe(
            "DMM config",
            "japan",
            "DMM",
            NormalizePath(dmmConfigPath),
            File.Exists(dmmConfigPath)));

        foreach (var dmmInstall in DetectDmmInstalls())
        {
            AddProbe(installs, probes, dmmInstall.Name, "japan", "DMM", dmmInstall.Path);
        }

        foreach (var steamInstall in DetectSteamInstalls())
        {
            AddProbe(installs, probes, steamInstall.Name, "japan", "Steam", steamInstall.Path);
        }

        return new UmaDetectionReport
        {
            Installs = installs
                .GroupBy(static install => install.Path, StringComparer.OrdinalIgnoreCase)
                .Select(static group => group.First())
                .ToList(),
            Probes = probes
                .GroupBy(static probe => probe.Path, StringComparer.OrdinalIgnoreCase)
                .Select(static group => group.First())
                .ToList(),
        };
    }

    public static UmaInstall Resolve(string? explicitPath)
    {
        if (!string.IsNullOrWhiteSpace(explicitPath))
        {
            var fullPath = Path.GetFullPath(Environment.ExpandEnvironmentVariables(explicitPath));
            EnsureValid(fullPath);
            return new UmaInstall("Explicit", fullPath);
        }

        var detected = DetectAll();
        if (detected.Count == 0)
        {
            throw new DirectoryNotFoundException(
                "No Umamusume install was detected. Pass --uma-dir to point at the game's data directory.");
        }

        return detected[0];
    }

    public static void EnsureValid(string path)
    {
        if (!IsValid(path))
        {
            throw new DirectoryNotFoundException(
                $"'{path}' does not look like a valid Umamusume data directory. Expected 'dat', 'meta', and 'master/master.mdb'.");
        }
    }

    public static bool IsValid(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return Directory.Exists(Path.Combine(path, "dat"))
            && File.Exists(Path.Combine(path, "meta"))
            && File.Exists(Path.Combine(path, "master", "master.mdb"));
    }

    private static void AddProbe(
        List<UmaInstall> installs,
        List<UmaDetectionProbe> probes,
        string name,
        string region,
        string source,
        string path)
    {
        var isValid = IsValid(path);
        var normalizedPath = NormalizePath(path);
        probes.Add(new UmaDetectionProbe(name, region, source, normalizedPath, isValid));
        if (isValid)
        {
            installs.Add(new UmaInstall(name, normalizedPath));
        }
    }

    private static IEnumerable<UmaInstall> DetectDmmInstalls()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var configPath = Path.Combine(appData, "dmmgameplayer5", "dmmgame.cnf");
        if (!File.Exists(configPath))
        {
            yield break;
        }

        JsonNode? config;
        try
        {
            config = JsonNode.Parse(File.ReadAllText(configPath));
        }
        catch
        {
            yield break;
        }

        var contents = config?["contents"]?.AsArray();
        if (contents is null)
        {
            yield break;
        }

        foreach (var product in contents)
        {
            if (product?["productId"]?.ToString() != "umamusume")
            {
                continue;
            }

            var installDir = product["detail"]?["path"]?.ToString();
            if (string.IsNullOrWhiteSpace(installDir))
            {
                continue;
            }

            yield return new UmaInstall(
                "DMM Games (Japan)",
                Path.Combine(installDir, "umamusume_Data", "Persistent"));
        }
    }

    private static IEnumerable<UmaInstall> DetectSteamInstalls()
    {
        foreach (var steamRoot in GetSteamRoots())
        {
            yield return new UmaInstall(
                "Steam (Japan)",
                Path.Combine(
                    steamRoot,
                    "steamapps",
                    "common",
                    "UmamusumePrettyDerby_Jpn",
                    "UmamusumePrettyDerby_Jpn_Data",
                    "Persistent"));
        }
    }

    private static IEnumerable<string> GetSteamRoots()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var defaultSteamRoots = new[]
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
        };

        foreach (var root in defaultSteamRoots)
        {
            if (Directory.Exists(root))
            {
                roots.Add(NormalizePath(root));
            }
        }

        foreach (var root in GetSteamRegistryRoots())
        {
            if (Directory.Exists(root))
            {
                roots.Add(NormalizePath(root));
            }
        }

        foreach (var root in roots.ToArray())
        {
            var libraryFile = Path.Combine(root, "steamapps", "libraryfolders.vdf");
            if (!File.Exists(libraryFile))
            {
                continue;
            }

            foreach (var libraryRoot in ParseSteamLibraryFolders(libraryFile))
            {
                roots.Add(libraryRoot);
            }
        }

        return roots;
    }

    private static IEnumerable<string> GetSteamRegistryRoots()
    {
        if (!OperatingSystem.IsWindows())
        {
            yield break;
        }

        foreach (var root in GetSteamRegistryRootsWindows())
        {
            yield return root;
        }
    }

    [SupportedOSPlatform("windows")]
    private static IEnumerable<string> GetSteamRegistryRootsWindows()
    {
        var registryLocations = new[]
        {
            (RegistryHive.CurrentUser, @"Software\Valve\Steam", "SteamPath"),
            (RegistryHive.CurrentUser, @"Software\Valve\Steam", "SteamExe"),
            (RegistryHive.LocalMachine, @"Software\WOW6432Node\Valve\Steam", "InstallPath"),
            (RegistryHive.LocalMachine, @"Software\Valve\Steam", "InstallPath"),
        };

        foreach (var (hive, keyPath, valueName) in registryLocations)
        {
            string? rawValue = null;
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, RegistryView.Registry64);
                using var key = baseKey.OpenSubKey(keyPath);
                rawValue = key?.GetValue(valueName)?.ToString();
            }
            catch
            {
            }

            if (string.IsNullOrWhiteSpace(rawValue))
            {
                continue;
            }

            var candidate = Environment.ExpandEnvironmentVariables(rawValue);
            if (candidate.EndsWith("steam.exe", StringComparison.OrdinalIgnoreCase))
            {
                candidate = Path.GetDirectoryName(candidate) ?? candidate;
            }

            yield return candidate;
        }
    }

    private static IEnumerable<string> ParseSteamLibraryFolders(string libraryFoldersPath)
    {
        foreach (var line in File.ReadLines(libraryFoldersPath))
        {
            var trimmed = line.Trim();
            if (!trimmed.Contains("\"path\"", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var parts = trimmed.Split('"', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 4)
            {
                continue;
            }

            var candidate = Environment.ExpandEnvironmentVariables(parts[3]).Replace(@"\\", @"\");
            if (Directory.Exists(candidate))
            {
                yield return NormalizePath(candidate);
            }
        }
    }

    private static string NormalizePath(string path)
    {
        var expanded = Environment.ExpandEnvironmentVariables(path);
        try
        {
            expanded = Path.GetFullPath(expanded);
        }
        catch
        {
        }

        return expanded
            .Replace('/', '\\')
            .TrimEnd('\\');
    }
}
