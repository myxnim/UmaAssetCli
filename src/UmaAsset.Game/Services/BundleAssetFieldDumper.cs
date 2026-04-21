using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Text;

namespace UmaAsset.Game.Services;

public sealed class BundleAssetFieldDumper
{
    private readonly ManifestDatabase manifestDatabase;

    public BundleAssetFieldDumper(ManifestDatabase manifestDatabase)
    {
        this.manifestDatabase = manifestDatabase;
    }

    public string Dump(ManifestEntry entry, string assetName)
    {
        var assetsManager = new AssetsManager();
        using var assetStream = OpenBundleStream(entry);
        var bundle = assetsManager.LoadBundleFile(assetStream);

        foreach (var assetsFileName in bundle.file.GetAllFileNames())
        {
            var assetsFile = assetsManager.LoadAssetsFileFromBundle(bundle, assetsFileName);
            if (assetsFile?.file is null)
            {
                continue;
            }

            foreach (var assetInfo in assetsFile.file.AssetInfos)
            {
                var baseField = assetsManager.GetBaseField(assetsFile, assetInfo);
                var name = TryReadName(baseField);
                if (!string.Equals(name, assetName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var builder = new StringBuilder();
                builder.AppendLine($"Asset: {name}");
                builder.AppendLine($"Type: {assetInfo.TypeId} ({(int)assetInfo.TypeId})");
                builder.AppendLine($"PathId: {assetInfo.PathId}");
                builder.AppendLine($"File: {assetsFileName}");
                builder.AppendLine();
                AppendField(builder, baseField, 0);
                return builder.ToString();
            }
        }

        throw new InvalidOperationException($"Asset '{assetName}' was not found in bundle '{entry.Name}'.");
    }

    private static void AppendField(StringBuilder builder, AssetTypeValueField field, int depth)
    {
        var indent = new string(' ', depth * 2);
        builder.Append(indent);
        builder.Append(field.FieldName);
        builder.Append(" = ");

        if (field.Children is { Count: > 0 })
        {
            builder.AppendLine();
            foreach (var child in field.Children)
            {
                AppendField(builder, child, depth + 1);
            }

            return;
        }

        try
        {
            builder.AppendLine(field.AsString ?? string.Empty);
        }
        catch
        {
            builder.AppendLine("<unprintable>");
        }
    }

    private static string TryReadName(AssetTypeValueField? baseField)
    {
        var nameField = baseField?["m_Name"];
        if (nameField is null)
        {
            return string.Empty;
        }

        try
        {
            return nameField.AsString ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private FileStream OpenBundleStream(ManifestEntry entry)
    {
        var sourcePath = manifestDatabase.GetDataFilePath(entry);
        if (entry.EncryptionKey != 0)
        {
            return new EncryptedAssetStream(sourcePath, entry.EncryptionKey);
        }

        return new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
    }
}
