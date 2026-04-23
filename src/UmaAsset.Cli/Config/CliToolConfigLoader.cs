using System.Text.Json;
using UmaAsset.Game.Services;

namespace UmaAsset.Cli.Config;

public static class CliToolConfigLoader
{
    private const string ConfigFileName = "umaassetcli.config.json";

    public static CliToolConfig Load()
    {
        var path = ResolvePath();
        if (path is null || !File.Exists(path))
        {
            return new CliToolConfig();
        }

        try
        {
            return JsonSerializer.Deserialize<CliToolConfig>(
                       File.ReadAllText(path),
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true,
                       })
                   ?? new CliToolConfig();
        }
        catch
        {
            return new CliToolConfig();
        }
    }

    public static TextureExportOptions CreateTextureExportOptions()
    {
        var config = Load();
        return new TextureExportOptions
        {
            SupportThumbPortrait = new SupportThumbPortraitOptions
            {
                Width = Math.Max(1, config.TextureExport.SupportThumbPortrait.Width),
                Height = Math.Max(1, config.TextureExport.SupportThumbPortrait.Height),
            },
        };
    }

    private static string? ResolvePath()
    {
        var baseDirectoryPath = Path.Combine(AppContext.BaseDirectory, ConfigFileName);
        if (File.Exists(baseDirectoryPath))
        {
            return baseDirectoryPath;
        }

        var currentDirectoryPath = Path.Combine(Environment.CurrentDirectory, ConfigFileName);
        if (File.Exists(currentDirectoryPath))
        {
            return currentDirectoryPath;
        }

        return baseDirectoryPath;
    }
}
