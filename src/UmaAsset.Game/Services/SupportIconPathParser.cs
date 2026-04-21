using System.Text.RegularExpressions;

namespace UmaAsset.Game.Services;

public static partial class SupportIconPathParser
{
    [GeneratedRegex(@"^support_thumb_(\d{5})$", RegexOptions.IgnoreCase)]
    private static partial Regex SupportThumbRegex();

    public static string? ParseSupportId(string textureName)
    {
        var match = SupportThumbRegex().Match(textureName);
        return match.Success ? match.Groups[1].Value : null;
    }
}
