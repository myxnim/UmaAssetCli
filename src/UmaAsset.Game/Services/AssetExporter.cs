namespace UmaAsset.Game.Services;

public sealed class AssetExporter
{
    private readonly ManifestDatabase manifestDatabase;

    public AssetExporter(ManifestDatabase manifestDatabase)
    {
        this.manifestDatabase = manifestDatabase;
    }

    public string Export(ManifestEntry entry, string outputRoot, bool decrypt, bool flatten)
    {
        var sourcePath = manifestDatabase.GetDataFilePath(entry);
        if (!File.Exists(sourcePath))
        {
            throw new FileNotFoundException($"Data file was not found for '{entry.Name}'.", sourcePath);
        }

        Directory.CreateDirectory(outputRoot);

        var relativePath = flatten
            ? BuildFlatName(entry, decrypt)
            : BuildRelativePath(entry, decrypt);
        var destinationPath = Path.Combine(outputRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        if (decrypt && entry.EncryptionKey != 0)
        {
            using var input = new EncryptedAssetStream(sourcePath, entry.EncryptionKey);
            using var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);
            input.CopyTo(output);
            return destinationPath;
        }

        File.Copy(sourcePath, destinationPath, overwrite: true);
        return destinationPath;
    }

    private static string BuildRelativePath(ManifestEntry entry, bool decrypt)
    {
        var cleanName = entry.Name.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var suffix = decrypt && entry.EncryptionKey != 0 ? ".decrypted" : ".bundle";
        return $"{cleanName}{suffix}";
    }

    private static string BuildFlatName(ManifestEntry entry, bool decrypt)
    {
        var cleanBase = entry.BaseName.Replace('/', '_');
        var suffix = decrypt && entry.EncryptionKey != 0 ? ".decrypted" : ".bundle";
        return $"{cleanBase}{suffix}";
    }
}
