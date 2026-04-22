using System.Security.Cryptography;
using System.Text;

namespace UmaAsset.Game.Services;

public static class GameFileStager
{
    private static readonly string StageRoot = Path.Combine(Path.GetTempPath(), "UmaAssetCli", "staged");
    private static bool staleCleanupDone;

    public static void CleanupStageRoot()
    {
        if (!Directory.Exists(StageRoot))
        {
            return;
        }

        foreach (var file in Directory.GetFiles(StageRoot))
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
            }
        }
    }

    public static TemporaryStagedFile StageMetaFile(string sourcePath)
    {
        EnsureStageRootReady();
        var stagedPath = Path.Combine(StageRoot, $"{BuildStableName(sourcePath)}.{Guid.NewGuid():N}.meta");
        File.Copy(sourcePath, stagedPath, overwrite: true);
        return new TemporaryStagedFile(stagedPath);
    }

    public static TemporaryStagedFile StageBundleFile(string sourcePath)
    {
        EnsureStageRootReady();
        var tempPath = Path.Combine(StageRoot, $"{BuildStableName(sourcePath)}.{Guid.NewGuid():N}.bundle");
        File.Copy(sourcePath, tempPath, overwrite: true);
        return new TemporaryStagedFile(tempPath);
    }

    private static void EnsureStageRootReady()
    {
        Directory.CreateDirectory(StageRoot);
        if (staleCleanupDone)
        {
            return;
        }

        CleanupStageRoot();
        staleCleanupDone = true;
    }

    private static string BuildStableName(string sourcePath)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sourcePath));
        return Convert.ToHexString(bytes[..8]).ToLowerInvariant();
    }
}
