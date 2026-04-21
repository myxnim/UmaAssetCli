namespace UmaAsset.Game.Services;

public static class SupportIconResourceNames
{
    public static IReadOnlyList<string> FromIds(IEnumerable<string> ids)
    {
        var resourceNames = new List<string>();

        foreach (var rawId in ids)
        {
            if (!int.TryParse(rawId, out var supportId) || supportId < 0)
            {
                throw new ArgumentException($"Invalid support id '{rawId}'.");
            }

            resourceNames.Add($"support_thumb_{supportId:d5}");
        }

        return resourceNames
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static name => name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
