using System.Security.Cryptography;
using System.Text;

namespace UmaAsset.Game.Services;

public static class GameFileStager
{
    private static readonly string StageRoot = Path.Combine(Path.GetTempPath(), "UmaAssetCli", "staged");

    public static string StageMetaFile(string sourcePath)
    {
        Directory.CreateDirectory(StageRoot);
        var stagedPath = Path.Combine(StageRoot, $"{BuildStableName(sourcePath)}.meta");
        File.Copy(sourcePath, stagedPath, overwrite: true);
        return stagedPath;
    }

    public static TemporaryStagedFile StageBundleFile(string sourcePath)
    {
        Directory.CreateDirectory(StageRoot);
        var tempPath = Path.Combine(StageRoot, $"{BuildStableName(sourcePath)}.{Guid.NewGuid():N}.bundle");
        File.Copy(sourcePath, tempPath, overwrite: true);
        return new TemporaryStagedFile(tempPath);
    }

    private static string BuildStableName(string sourcePath)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sourcePath));
        return Convert.ToHexString(bytes[..8]).ToLowerInvariant();
    }
}
