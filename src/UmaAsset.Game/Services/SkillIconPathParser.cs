using System.Text.RegularExpressions;

namespace UmaAsset.Game.Services;

public static partial class SkillIconPathParser
{
    [GeneratedRegex(@"^utx_ico_skill_(\d+)$", RegexOptions.IgnoreCase)]
    private static partial Regex SkillIconRegex();

    public static string? ParseIconId(string textureName)
    {
        var match = SkillIconRegex().Match(textureName);
        return match.Success ? match.Groups[1].Value : null;
    }
}
