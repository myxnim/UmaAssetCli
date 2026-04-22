namespace UmaAsset.Game.Services;

public sealed class TemporaryStagedFile : IDisposable
{
    public TemporaryStagedFile(string path)
    {
        Path = path;
        GameFileCleanupRegistry.Register(path);
    }

    public string Path { get; }

    public void Dispose()
    {
        try
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
        catch
        {
        }
        finally
        {
            GameFileCleanupRegistry.Unregister(Path);
        }
    }
}
