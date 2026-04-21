using AssetsTools.NET;
using AssetsTools.NET.Extra;

namespace UmaAsset.Game.Services;

public sealed class BundleAssetInspector
{
    private readonly ManifestDatabase manifestDatabase;

    public BundleAssetInspector(ManifestDatabase manifestDatabase)
    {
        this.manifestDatabase = manifestDatabase;
    }

    public IReadOnlyList<BundleAssetInfo> Inspect(ManifestEntry entry)
    {
        var results = new List<BundleAssetInfo>();
        var assetsManager = new AssetsManager();

        using var staged = GameFileStager.StageBundleFile(manifestDatabase.GetDataFilePath(entry));
        using var assetStream = entry.EncryptionKey != 0
            ? new EncryptedAssetStream(staged.Path, entry.EncryptionKey)
            : new FileStream(staged.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
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
                var nameField = baseField?["m_Name"];
                var name = string.Empty;
                if (nameField is not null)
                {
                    try
                    {
                        name = nameField.AsString ?? string.Empty;
                    }
                    catch (NullReferenceException)
                    {
                        name = string.Empty;
                    }
                }

                results.Add(new BundleAssetInfo(
                    assetsFileName,
                    assetInfo.PathId,
                    (int)assetInfo.TypeId,
                    assetInfo.TypeId.ToString(),
                    name));
            }
        }

        return results;
    }
}
