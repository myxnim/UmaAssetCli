namespace UmaAsset.Game.Services;

public static class GameFileCleanupRegistry
{
    private static readonly object Sync = new();
    private static readonly HashSet<string> RegisteredPaths = new(StringComparer.OrdinalIgnoreCase);
    private static bool initialized;

    public static void Register(string path)
    {
        EnsureInitialized();

        lock (Sync)
        {
            RegisteredPaths.Add(path);
        }
    }

    public static void Unregister(string path)
    {
        lock (Sync)
        {
            RegisteredPaths.Remove(path);
        }
    }

    private static void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        lock (Sync)
        {
            if (initialized)
            {
                return;
            }

            Console.CancelKeyPress += static (_, _) => CleanupAll();
            AppDomain.CurrentDomain.ProcessExit += static (_, _) => CleanupAll();
            initialized = true;
        }
    }

    private static void CleanupAll()
    {
        List<string> paths;
        lock (Sync)
        {
            paths = [.. RegisteredPaths];
            RegisteredPaths.Clear();
        }

        foreach (var path in paths)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }
    }
}
