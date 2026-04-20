namespace UmaAsset.Game.Services;

public static class CharaIconResourceNames
{
    public static IReadOnlyList<string> FromIds(IEnumerable<string> ids, int plate, IReadOnlyCollection<string>? families = null)
    {
        var resourceNames = new List<string>();
        var normalizedFamilies = NormalizeFamilies(families);

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
                var baseCharacterId = int.Parse(normalized);
                if (normalizedFamilies.Contains("chr"))
                {
                    resourceNames.Add($"chr_icon_{baseCharacterId:d4}");
                }
                if (normalizedFamilies.Contains("round"))
                {
                    resourceNames.Add($"chr_icon_round_{baseCharacterId:d4}");
                }
                if (normalizedFamilies.Contains("plus"))
                {
                    resourceNames.Add($"chr_icon_plus_{baseCharacterId:d4}");
                }

                continue;
            }

            var baseId = int.Parse(normalized[..4]);
            var dressId = int.Parse(normalized);
            if (normalizedFamilies.Contains("chr"))
            {
                resourceNames.Add($"chr_icon_{baseId:d4}_{dressId:d6}_{plate:d2}");
            }
            if (normalizedFamilies.Contains("trained"))
            {
                resourceNames.Add($"trained_chr_icon_{baseId:d4}_{dressId:d6}_{plate:d2}");
            }
        }

        return resourceNames.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static HashSet<string> NormalizeFamilies(IReadOnlyCollection<string>? families)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (families is null || families.Count == 0)
        {
            result.Add("chr");
            return result;
        }

        foreach (var family in families)
        {
            var normalized = family.Trim().ToLowerInvariant();
            if (normalized is "chr" or "trained" or "round" or "plus")
            {
                result.Add(normalized);
                continue;
            }

            throw new ArgumentException($"Unsupported family '{family}'. Expected one of: chr, trained, round, plus.");
        }

        if (result.Count == 0)
        {
            result.Add("chr");
        }

        return result;
    }
}
