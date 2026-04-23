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
        IReadOnlyCollection<string>? exactTextureNames = null,
        bool overwriteExisting = true)
    {
        var results = new List<TextureExportResult>();
        Directory.CreateDirectory(outputRoot);

        var assetsManager = new AssetsManager();
        using var staged = GameFileStager.StageBundleFile(manifestDatabase.GetDataFilePath(entry));
        using var assetStream = entry.EncryptionKey != 0
            ? new EncryptedAssetStream(staged.Path, entry.EncryptionKey)
            : new FileStream(staged.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var bundle = assetsManager.LoadBundleFile(assetStream);

        foreach (var assetsFileName in bundle.file.GetAllFileNames())
        {
            if (ShouldSkipEmbeddedFile(assetsFileName))
            {
                continue;
            }

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
                var pictureData = TryLoadPictureData(textureFile, bundle, assetsFile);
                using var image = LoadImage(textureFile, pictureData);

                var safeTextureName = SanitizeFileName(textureName);
                var directory = BuildOutputDirectory(outputRoot, entry, assetsFileName, textureName);
                Directory.CreateDirectory(directory);

                var outputPath = Path.Combine(directory, $"{safeTextureName}.png");
                if (!overwriteExisting && File.Exists(outputPath))
                {
                    continue;
                }

                image.SaveAsPng(outputPath);

                results.Add(new TextureExportResult(textureName, outputPath));
            }
        }

        return results;
    }

    private static bool ShouldSkipEmbeddedFile(string assetsFileName)
    {
        return assetsFileName.EndsWith(".resS", StringComparison.OrdinalIgnoreCase);
    }

    private static byte[] TryLoadPictureData(
        TextureFile textureFile,
        BundleFileInstance bundle,
        AssetsFileInstance assetsFile)
    {
        try
        {
            textureFile.SetPictureDataFromBundle(bundle);
            if (textureFile.pictureData is { Length: > 0 })
            {
                return textureFile.pictureData;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Streaming data load from bundle failed for {textureFile.m_Name} " +
                $"(format={(TextureFormat)textureFile.m_TextureFormat}/{textureFile.m_TextureFormat}, " +
                $"size={textureFile.m_Width}x{textureFile.m_Height}, streamPath={textureFile.m_StreamData.path}, " +
                $"streamSize={textureFile.m_StreamData.size}, completeImageSize={textureFile.m_CompleteImageSize}).",
                ex);
        }

        try
        {
            return textureFile.FillPictureData(assetsFile);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Streaming data load from assets file failed for {textureFile.m_Name} " +
                $"(format={(TextureFormat)textureFile.m_TextureFormat}/{textureFile.m_TextureFormat}, " +
                $"size={textureFile.m_Width}x{textureFile.m_Height}, streamPath={textureFile.m_StreamData.path}, " +
                $"streamSize={textureFile.m_StreamData.size}, completeImageSize={textureFile.m_CompleteImageSize}).",
                ex);
        }
    }

    private static Image<Rgba32> LoadImage(TextureFile textureFile, byte[] pictureData)
    {
        try
        {
            var rawData = textureFile.DecodeTextureRaw(pictureData, false);
            var image = Image.LoadPixelData<Rgba32>(rawData, textureFile.m_Width, textureFile.m_Height);
            image.Mutate(static op => op.Flip(FlipMode.Vertical));
            return image;
        }
        catch (Exception) when (pictureData.Length > 0)
        {
            try
            {
                var rawData = TextureFile.DecodeManagedData(
                    pictureData,
                    (TextureFormat)textureFile.m_TextureFormat,
                    textureFile.m_Width,
                    textureFile.m_Height,
                    false,
                    textureFile.GetSwizzler());
                var image = Image.LoadPixelData<Rgba32>(rawData, textureFile.m_Width, textureFile.m_Height);
                image.Mutate(static op => op.Flip(FlipMode.Vertical));
                return image;
            }
            catch (Exception)
            {
                try
                {
                    if (TryDecodeBlockCompressed(textureFile, pictureData, out var manualImage))
                    {
                        manualImage.Mutate(static op => op.Flip(FlipMode.Vertical));
                        return manualImage;
                    }

                    using var stream = new MemoryStream();
                    textureFile.DecodeTextureImage(pictureData, stream, ImageExportType.Png, 100);
                    stream.Position = 0;
                    return Image.Load<Rgba32>(stream);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Texture decode failed for {textureFile.m_Name} " +
                        $"(format={(TextureFormat)textureFile.m_TextureFormat}/{textureFile.m_TextureFormat}, " +
                        $"size={textureFile.m_Width}x{textureFile.m_Height}, pictureData={pictureData.Length}, " +
                        $"completeImageSize={textureFile.m_CompleteImageSize}).",
                        ex);
                }
            }
        }
    }

    private static bool TryDecodeBlockCompressed(
        TextureFile textureFile,
        byte[] pictureData,
        out Image<Rgba32>? image)
    {
        image = null;
        var format = (TextureFormat)textureFile.m_TextureFormat;
        byte[]? rgba = format switch
        {
            TextureFormat.DXT1 => DecodeDxt1(pictureData, textureFile.m_Width, textureFile.m_Height),
            TextureFormat.DXT5 => DecodeDxt5(pictureData, textureFile.m_Width, textureFile.m_Height),
            _ => null,
        };

        if (rgba is null)
        {
            return false;
        }

        image = Image.LoadPixelData<Rgba32>(rgba, textureFile.m_Width, textureFile.m_Height);
        return true;
    }

    private static byte[] DecodeDxt1(byte[] source, int width, int height)
    {
        var output = new byte[width * height * 4];
        var blockWidth = (width + 3) / 4;
        var blockHeight = (height + 3) / 4;
        var offset = 0;

        for (var by = 0; by < blockHeight; by++)
        {
            for (var bx = 0; bx < blockWidth; bx++)
            {
                var color0 = BitConverter.ToUInt16(source, offset);
                var color1 = BitConverter.ToUInt16(source, offset + 2);
                var indices = BitConverter.ToUInt32(source, offset + 4);
                offset += 8;

                Span<Rgba32> palette = stackalloc Rgba32[4];
                palette[0] = DecodeRgb565(color0);
                palette[1] = DecodeRgb565(color1);

                if (color0 > color1)
                {
                    palette[2] = Interpolate(palette[0], palette[1], 2, 1, 3);
                    palette[3] = Interpolate(palette[0], palette[1], 1, 2, 3);
                }
                else
                {
                    palette[2] = Interpolate(palette[0], palette[1], 1, 1, 2);
                    palette[3] = new Rgba32(0, 0, 0, 0);
                }

                WriteBlock(output, width, height, bx, by, palette, indices, hasAlpha: false, alphaIndices: 0, alphaPalette: []);
            }
        }

        return output;
    }

    private static byte[] DecodeDxt5(byte[] source, int width, int height)
    {
        var output = new byte[width * height * 4];
        var blockWidth = (width + 3) / 4;
        var blockHeight = (height + 3) / 4;
        var offset = 0;

        for (var by = 0; by < blockHeight; by++)
        {
            for (var bx = 0; bx < blockWidth; bx++)
            {
                Span<byte> alphaPalette = stackalloc byte[8];
                alphaPalette[0] = source[offset];
                alphaPalette[1] = source[offset + 1];
                PopulateDxt5AlphaPalette(alphaPalette);

                ulong alphaBits = 0;
                for (var i = 0; i < 6; i++)
                {
                    alphaBits |= (ulong)source[offset + 2 + i] << (8 * i);
                }

                var color0 = BitConverter.ToUInt16(source, offset + 8);
                var color1 = BitConverter.ToUInt16(source, offset + 10);
                var indices = BitConverter.ToUInt32(source, offset + 12);
                offset += 16;

                Span<Rgba32> palette = stackalloc Rgba32[4];
                palette[0] = DecodeRgb565(color0);
                palette[1] = DecodeRgb565(color1);
                palette[2] = Interpolate(palette[0], palette[1], 2, 1, 3);
                palette[3] = Interpolate(palette[0], palette[1], 1, 2, 3);

                WriteBlock(output, width, height, bx, by, palette, indices, hasAlpha: true, alphaBits, alphaPalette);
            }
        }

        return output;
    }

    private static void PopulateDxt5AlphaPalette(Span<byte> palette)
    {
        var alpha0 = palette[0];
        var alpha1 = palette[1];
        if (alpha0 > alpha1)
        {
            palette[2] = (byte)((6 * alpha0 + alpha1) / 7);
            palette[3] = (byte)((5 * alpha0 + 2 * alpha1) / 7);
            palette[4] = (byte)((4 * alpha0 + 3 * alpha1) / 7);
            palette[5] = (byte)((3 * alpha0 + 4 * alpha1) / 7);
            palette[6] = (byte)((2 * alpha0 + 5 * alpha1) / 7);
            palette[7] = (byte)((alpha0 + 6 * alpha1) / 7);
        }
        else
        {
            palette[2] = (byte)((4 * alpha0 + alpha1) / 5);
            palette[3] = (byte)((3 * alpha0 + 2 * alpha1) / 5);
            palette[4] = (byte)((2 * alpha0 + 3 * alpha1) / 5);
            palette[5] = (byte)((alpha0 + 4 * alpha1) / 5);
            palette[6] = 0;
            palette[7] = 255;
        }
    }

    private static void WriteBlock(
        byte[] output,
        int width,
        int height,
        int blockX,
        int blockY,
        Span<Rgba32> palette,
        uint colorIndices,
        bool hasAlpha,
        ulong alphaIndices,
        ReadOnlySpan<byte> alphaPalette)
    {
        for (var py = 0; py < 4; py++)
        {
            for (var px = 0; px < 4; px++)
            {
                var x = (blockX * 4) + px;
                var y = (blockY * 4) + py;
                if (x >= width || y >= height)
                {
                    continue;
                }

                var pixelIndex = py * 4 + px;
                var colorIndex = (int)((colorIndices >> (pixelIndex * 2)) & 0x3);
                var color = palette[colorIndex];
                if (hasAlpha)
                {
                    var alphaIndex = (int)((alphaIndices >> (pixelIndex * 3)) & 0x7);
                    color.A = alphaPalette[alphaIndex];
                }

                var outputIndex = ((y * width) + x) * 4;
                output[outputIndex] = color.R;
                output[outputIndex + 1] = color.G;
                output[outputIndex + 2] = color.B;
                output[outputIndex + 3] = color.A;
            }
        }
    }

    private static Rgba32 DecodeRgb565(ushort value)
    {
        var r = (byte)((((value >> 11) & 0x1F) * 255) / 31);
        var g = (byte)((((value >> 5) & 0x3F) * 255) / 63);
        var b = (byte)(((value & 0x1F) * 255) / 31);
        return new Rgba32(r, g, b, 255);
    }

    private static Rgba32 Interpolate(Rgba32 a, Rgba32 b, int weightA, int weightB, int divisor)
    {
        return new Rgba32(
            (byte)((a.R * weightA + b.R * weightB) / divisor),
            (byte)((a.G * weightA + b.G * weightB) / divisor),
            (byte)((a.B * weightA + b.B * weightB) / divisor),
            255);
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
                charaIconInfo.Family);
        }

        var supportInfo = SupportIconPathParser.Parse(textureName);
        if (supportInfo is not null)
        {
            return Path.Combine(
                outputRoot,
                "support",
                supportInfo.SupportId);
        }

        var skillIconId = SkillIconPathParser.ParseIconId(textureName);
        if (skillIconId is not null)
        {
            return Path.Combine(outputRoot, "skill-icons");
        }

        var safeAssetsFileName = SanitizeFileName(Path.GetFileNameWithoutExtension(assetsFileName));
        return Path.Combine(outputRoot, SanitizePath(entry.Manifest), safeAssetsFileName);
    }
}
