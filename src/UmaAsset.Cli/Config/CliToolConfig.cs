namespace UmaAsset.Cli.Config;

public sealed class CliToolConfig
{
    public TextureExportConfig TextureExport { get; init; } = new();
}

public sealed class TextureExportConfig
{
    public SupportThumbPortraitConfig SupportThumbPortrait { get; init; } = new();
}

public sealed class SupportThumbPortraitConfig
{
    public int Width { get; init; } = 392;

    public int Height { get; init; } = 512;
}
