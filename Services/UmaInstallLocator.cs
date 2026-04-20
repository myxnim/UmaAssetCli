using System.Text.Json.Nodes;
using UmaAssetCli.Models;

namespace UmaAssetCli.Services;

public static class UmaInstallLocator
{
    public static IReadOnlyList<UmaInstall> DetectAll()
    {
        var installs = new List<UmaInstall>();

        AddIfValid(
            installs,
            "Default (AppData)",
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low",
                "Cygames",
                "umamusume"));

        AddIfValid(
            installs,
            "Default (AppData JP)",
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low",
                "Cygames",
                "UmamusumePrettyDerby_Jpn"));

        foreach (var dmmInstall in DetectDmmInstalls())
        {
            AddIfValid(installs, dmmInstall.Name, dmmInstall.Path);
        }

        AddIfValid(
            installs,
            "Steam (Japan)",
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Steam",
                "steamapps",
                "common",
                "UmamusumePrettyDerby_Jpn",
                "UmamusumePrettyDerby_Jpn_Data",
                "Persistent"));

        return installs;
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
                "No Umamusume install was detected. Pass --uma-dir to point at the game's Persistent directory.");
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

    private static void AddIfValid(List<UmaInstall> installs, string name, string path)
    {
        if (IsValid(path))
        {
            installs.Add(new UmaInstall(name, path));
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
                "DMM Games",
                Path.Combine(installDir, "umamusume_Data", "Persistent"));
        }
    }
}
