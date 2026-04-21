using AssetsTools.NET;
using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Image = SixLabors.ImageSharp.Image;

namespace UmaAsset.Game.Services;

public sealed class TextureBundleExporter
{
    private readonly ManifestDatabase manifestDatabase;

    public TextureBundleExporter(ManifestDatabase manifestDatabase)
    {
        this.manifestDatabase = manifestDatabase;
    }

    public IReadOnlyList<TextureExportResult> ExportTextures(
        ManifestEntry entry,
        string outputRoot,
        IReadOnlyCollection<string>? exactTextureNames = null)
    {
        var results = new List<TextureExportResult>();
        Directory.CreateDirectory(outputRoot);

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

            var textures = assetsFile.file.GetAssetsOfType(AssetClassID.Texture2D);

            foreach (var textureAsset in textures)
            {
                var baseField = assetsManager.GetBaseField(assetsFile, textureAsset);
                var textureName = baseField["m_Name"].AsString;

                if (exactTextureNames is not null
                    && exactTextureNames.Count > 0
                    && !exactTextureNames.Contains(textureName, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var textureFile = TextureFile.ReadTextureFile(baseField);
                var rawData = textureFile.DecodeTextureRaw(textureFile.FillPictureData(assetsFile), false);
                using var image = Image.LoadPixelData<Rgba32>(rawData, textureFile.m_Width, textureFile.m_Height);
                image.Mutate(static op => op.Flip(FlipMode.Vertical));

                var safeTextureName = SanitizeFileName(textureName);
                var directory = BuildOutputDirectory(outputRoot, entry, assetsFileName, textureName);
                Directory.CreateDirectory(directory);

                var outputPath = Path.Combine(directory, $"{safeTextureName}.png");
                image.SaveAsPng(outputPath);

                results.Add(new TextureExportResult(textureName, outputPath));
            }
        }

        return results;
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

    private static string SanitizePath(string value)
    {
        return string.Join(
            "_",
            value.Split(Path.GetInvalidPathChars().Append(Path.DirectorySeparatorChar).Append(Path.AltDirectorySeparatorChar).ToArray(),
                StringSplitOptions.RemoveEmptyEntries));
    }

    private static string SanitizeFileName(string value)
    {
        foreach (var invalid in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(invalid, '_');
        }

        return string.IsNullOrWhiteSpace(value) ? "texture" : value;
    }

    private static string BuildOutputDirectory(
        string outputRoot,
        ManifestEntry entry,
        string assetsFileName,
        string textureName)
    {
        var charaIconInfo = CharaIconPathParser.Parse(textureName);
        if (charaIconInfo is not null)
        {
            return Path.Combine(
                outputRoot,
                "character",
                charaIconInfo.CharacterId,
                "icons",
                charaIconInfo.Family);
        }

        var supportId = SupportIconPathParser.ParseSupportId(textureName);
        if (supportId is not null)
        {
            return Path.Combine(
                outputRoot,
                "support",
                supportId,
                "icons",
                "thumb");
        }

        var skillIconId = SkillIconPathParser.ParseIconId(textureName);
        if (skillIconId is not null)
        {
            return Path.Combine(outputRoot, "skill-icons");
        }

        if (textureName.StartsWith("utx_ico_motivation_l_", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(outputRoot, "ui", "motivation");
        }

        if (textureName.StartsWith("utx_ico_charastatus_l_", StringComparison.OrdinalIgnoreCase))
        {
            return Path.Combine(outputRoot, "ui", "charastatus");
        }

        var safeAssetsFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(assetsFileName));
        return Path.Combine(outputRoot, SanitizePath(entry.Manifest), safeAssetsFileName);
    }
}
