namespace UmaAsset.Game.Services;

public sealed class TextureExportOptions
{
    public static TextureExportOptions Default { get; } = new();

    public SupportThumbPortraitOptions SupportThumbPortrait { get; init; } = new();
}

public sealed class SupportThumbPortraitOptions
{
    public int Width { get; init; } = 392;

    public int Height { get; init; } = 512;
}
