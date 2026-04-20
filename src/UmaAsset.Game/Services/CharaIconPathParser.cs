using System.Text.RegularExpressions;

namespace UmaAsset.Game.Services;

public static partial class CharaIconPathParser
{
    [GeneratedRegex(@"^chr_icon_(\d{4})$", RegexOptions.IgnoreCase)]
    private static partial Regex BaseIconRegex();

    [GeneratedRegex(@"^chr_icon_(\d{4})_(\d{6})_(\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex DressIconRegex();

    [GeneratedRegex(@"^trained_chr_icon_(\d{4})_(\d{6})_(\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex TrainedIconRegex();

    [GeneratedRegex(@"^chr_icon_round_(\d{4})$", RegexOptions.IgnoreCase)]
    private static partial Regex RoundIconRegex();

    [GeneratedRegex(@"^chr_icon_plus_(\d{4})$", RegexOptions.IgnoreCase)]
    private static partial Regex PlusIconRegex();

    public static CharaIconPathInfo? Parse(string textureName)
    {
        var match = BaseIconRegex().Match(textureName);
        if (match.Success)
        {
            var characterId = match.Groups[1].Value;
            return new CharaIconPathInfo(characterId, "icon", characterId, textureName);
        }

        match = DressIconRegex().Match(textureName);
        if (match.Success)
        {
            var characterId = match.Groups[1].Value;
            var dressId = match.Groups[2].Value;
            return new CharaIconPathInfo(characterId, "dress-icon", dressId, textureName);
        }

        match = TrainedIconRegex().Match(textureName);
        if (match.Success)
        {
            var characterId = match.Groups[1].Value;
            var dressId = match.Groups[2].Value;
            return new CharaIconPathInfo(characterId, "trained-icon", dressId, textureName);
        }

        match = RoundIconRegex().Match(textureName);
        if (match.Success)
        {
            var characterId = match.Groups[1].Value;
            return new CharaIconPathInfo(characterId, "round-icon", characterId, textureName);
        }

        match = PlusIconRegex().Match(textureName);
        if (match.Success)
        {
            var characterId = match.Groups[1].Value;
            return new CharaIconPathInfo(characterId, "plus-icon", characterId, textureName);
        }

        return null;
    }
}
