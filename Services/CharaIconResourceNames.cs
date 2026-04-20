namespace UmaAssetCli.Services;

public static class CharaIconResourceNames
{
    public static IReadOnlyList<string> FromIds(IEnumerable<string> ids, int plate)
    {
        var resourceNames = new List<string>();

        foreach (var raw in ids)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            var normalized = raw.Trim();
            if (!normalized.All(char.IsDigit))
            {
                throw new ArgumentException($"'{raw}' is not a numeric character id.");
            }

            if (normalized.Length <= 4)
            {
                resourceNames.Add($"chr_icon_{int.Parse(normalized):d4}");
                continue;
            }

            var baseId = int.Parse(normalized[..4]);
            var dressId = int.Parse(normalized);
            resourceNames.Add($"chr_icon_{baseId:d4}_{dressId:d6}_{plate:d2}");
        }

        return resourceNames;
    }
}
