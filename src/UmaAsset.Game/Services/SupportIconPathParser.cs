using System.Text.RegularExpressions;

namespace UmaAsset.Game.Services;

public static partial class SupportIconPathParser
{
    [GeneratedRegex(@"^support_thumb_(\d{5})$", RegexOptions.IgnoreCase)]
    private static partial Regex SupportThumbRegex();

    [GeneratedRegex(@"^support_card_s_(\d{5})$", RegexOptions.IgnoreCase)]
    private static partial Regex SupportCardSmallRegex();

    [GeneratedRegex(@"^tex_support_card_(\d{5})$", RegexOptions.IgnoreCase)]
    private static partial Regex SupportCardFullRegex();

    [GeneratedRegex(@"^tex_support_card_(\d{5})_mask$", RegexOptions.IgnoreCase)]
    private static partial Regex SupportCardMaskRegex();

    public static string? ParseSupportId(string textureName)
    {
        return Parse(textureName)?.SupportId;
    }

    public static SupportImagePathInfo? Parse(string textureName)
    {
        var thumbMatch = SupportThumbRegex().Match(textureName);
        if (thumbMatch.Success)
        {
            return new SupportImagePathInfo(thumbMatch.Groups[1].Value, "thumb", textureName);
        }

        var smallCardMatch = SupportCardSmallRegex().Match(textureName);
        if (smallCardMatch.Success)
        {
            return new SupportImagePathInfo(smallCardMatch.Groups[1].Value, "card-small", textureName);
        }

        var fullCardMatch = SupportCardFullRegex().Match(textureName);
        if (fullCardMatch.Success)
        {
            return new SupportImagePathInfo(fullCardMatch.Groups[1].Value, "card-full", textureName);
        }

        var maskMatch = SupportCardMaskRegex().Match(textureName);
        if (maskMatch.Success)
        {
            return new SupportImagePathInfo(maskMatch.Groups[1].Value, "card-mask", textureName);
        }

        return null;
    }
}

public sealed record SupportImagePathInfo(
    string SupportId,
    string Family,
    string TextureName);
