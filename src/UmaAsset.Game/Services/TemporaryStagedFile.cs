namespace UmaAsset.Game.Services;

public sealed class TemporaryStagedFile : IDisposable
{
    public TemporaryStagedFile(string path)
    {
        Path = path;
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
    }
}
