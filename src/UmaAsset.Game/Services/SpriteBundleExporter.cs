using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace UmaAsset.Game.Services;

public sealed class SpriteBundleExporter
{
    private const int SpriteAssetTypeId = 213;
    private readonly ManifestDatabase manifestDatabase;

    public SpriteBundleExporter(ManifestDatabase manifestDatabase)
    {
        this.manifestDatabase = manifestDatabase;
    }

    public IReadOnlyList<SpriteExportResult> ExportSprites(
        ManifestEntry entry,
        string outputRoot,
        IReadOnlyDictionary<string, string>? spriteNameToOutputName = null,
        Action<string>? onSpriteExported = null)
    {
        var results = new List<SpriteExportResult>();
        Directory.CreateDirectory(outputRoot);

        var assetsManager = new AssetsManager();
        using var staged = GameFileStager.StageBundleFile(manifestDatabase.GetDataFilePath(entry));
        using var assetStream = entry.EncryptionKey != 0
            ? new EncryptedAssetStream(staged.Path, entry.EncryptionKey)
            : new FileStream(staged.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var bundle = assetsManager.LoadBundleFile(assetStream);

        foreach (var assetsFileName in bundle.file.GetAllFileNames())
        {
            AssetsFileInstance? assetsFile;
            try
            {
                assetsFile = assetsManager.LoadAssetsFileFromBundle(bundle, assetsFileName);
            }
            catch
            {
                continue;
            }

            if (assetsFile?.file is null)
            {
                continue;
            }

            var textures = assetsFile.file.GetAssetsOfType(AssetClassID.Texture2D)
                .ToDictionary(static asset => asset.PathId);
            var sprites = assetsFile.file.GetAssetsOfType(SpriteAssetTypeId);

            foreach (var spriteAsset in sprites)
            {
                var spriteField = assetsManager.GetBaseField(assetsFile, spriteAsset);
                var spriteName = TryReadName(spriteField);
                if (string.IsNullOrWhiteSpace(spriteName))
                {
                    continue;
                }

                var outputName = spriteName;
                if (spriteNameToOutputName is not null
                    && spriteNameToOutputName.Count > 0)
                {
                    if (!spriteNameToOutputName.TryGetValue(spriteName, out outputName))
                    {
                        continue;
                    }
                }
                var texturePathId = spriteField["m_RD"]["texture"]["m_PathID"].AsLong;
                if (!textures.TryGetValue(texturePathId, out var textureAsset))
                {
                    continue;
                }

                var textureField = assetsManager.GetBaseField(assetsFile, textureAsset);
                var textureFile = TextureFile.ReadTextureFile(textureField);
                var rawData = textureFile.DecodeTextureRaw(textureFile.FillPictureData(assetsFile), false);
                using var image = Image.LoadPixelData<Rgba32>(rawData, textureFile.m_Width, textureFile.m_Height);
                image.Mutate(static op => op.Flip(FlipMode.Vertical));

                var rectField = spriteField["m_RD"]["textureRect"];
                var x = (int)Math.Floor(rectField["x"].AsFloat);
                var y = (int)Math.Floor(rectField["y"].AsFloat);
                var width = (int)Math.Ceiling(rectField["width"].AsFloat);
                var height = (int)Math.Ceiling(rectField["height"].AsFloat);
                var cropY = image.Height - y - height;
                var crop = new Rectangle(x, cropY, width, height);

                crop = Rectangle.Intersect(crop, new Rectangle(0, 0, image.Width, image.Height));
                if (crop.Width <= 0 || crop.Height <= 0)
                {
                    continue;
                }

                using var spriteImage = image.Clone(op => op.Crop(crop));
                var safeOutputName = SanitizeFileName(outputName);
                var outputPath = Path.Combine(outputRoot, $"{safeOutputName}.png");
                spriteImage.SaveAsPng(outputPath);

                results.Add(new SpriteExportResult(spriteName, outputName, outputPath));
                onSpriteExported?.Invoke(spriteName);
            }
        }

        return results;
    }
    private static string TryReadName(AssetTypeValueField baseField)
    {
        var nameField = baseField["m_Name"];
        try
        {
            return nameField.AsString ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(value) ? "sprite" : value;
    }
}
