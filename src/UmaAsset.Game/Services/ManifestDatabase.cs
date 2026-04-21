using SQLite;

namespace UmaAsset.Game.Services;

public sealed class ManifestDatabase
{
    private static readonly string[] Keys =
    {
        "9c2bab97bcf8c0c4f1a9ea7881a213f6c9ebf9d8d4c6a8e43ce5a259bde7e9fd",
        "a713a5c79dbc9497c0a88669",
    };

    private readonly string metaPath;
    private string? selectedKey;
    private static bool sqliteInitialized;

    public ManifestDatabase(string umaDir)
    {
        UmaDir = umaDir;
        metaPath = Path.Combine(umaDir, "meta");
    }

    public string UmaDir { get; }

    public string DataDir => Path.Combine(UmaDir, "dat");

    public bool IsEncrypted
    {
        get
        {
            using var stream = new FileStream(metaPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new BinaryReader(stream);
            return reader.ReadUInt32() != 0x694C5153;
        }
    }

    public ManifestEntry? FindByName(string name)
    {
        using var connection = OpenConnection();
        return connection
            .Table<ManifestEntry>()
            .ToList()
            .FirstOrDefault(entry =>
                string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase)
                || string.Equals(entry.BaseName, name, StringComparison.OrdinalIgnoreCase));
    }

    public IReadOnlyList<ManifestEntry> FindMany(IEnumerable<string> names)
    {
        var wanted = names
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (wanted.Length == 0)
        {
            return [];
        }

        using var connection = OpenConnection();
        var entries = connection.Table<ManifestEntry>().ToList();

        return entries
            .Where(entry => wanted.Contains(entry.Name, StringComparer.OrdinalIgnoreCase)
                || wanted.Contains(entry.BaseName, StringComparer.OrdinalIgnoreCase))
            .ToArray();
    }

    public IReadOnlyList<ManifestEntry> SearchBySubstring(IEnumerable<string> patterns, int limit = 200)
    {
        var wanted = patterns
            .Where(static pattern => !string.IsNullOrWhiteSpace(pattern))
            .Select(static pattern => pattern.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (wanted.Length == 0)
        {
            return [];
        }

        using var connection = OpenConnection();
        var entries = connection.Table<ManifestEntry>().ToList();

        return entries
            .Where(entry => wanted.Any(pattern =>
                entry.Name.Contains(pattern, StringComparison.OrdinalIgnoreCase)
                || entry.BaseName.Contains(pattern, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(static entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .Take(Math.Max(1, limit))
            .ToArray();
    }

    public string GetDataFilePath(ManifestEntry entry)
    {
        return Path.Combine(DataDir, entry.HashName[..2], entry.HashName);
    }

    private SQLiteConnection OpenConnection()
    {
        if (!sqliteInitialized)
        {
            SQLitePCL.Batteries_V2.Init();
            sqliteInitialized = true;
        }

        var connection = new SQLiteConnection(metaPath, SQLiteOpenFlags.ReadWrite);
        if (!IsEncrypted)
        {
            return connection;
        }

        if (selectedKey is null)
        {
            foreach (var candidate in Keys)
            {
                try
                {
                    connection.ExecuteScalar<string>($"pragma hexkey = '{candidate}';");
                    var cipher = connection.ExecuteScalar<string>("SELECT sqlite3mc_config('cipher')");
                    if (string.Equals(cipher, "chacha20", StringComparison.OrdinalIgnoreCase))
                    {
                        selectedKey = candidate;
                        return connection;
                    }
                }
                catch (SQLiteException)
                {
                }
            }

            connection.Dispose();
            throw new InvalidOperationException("Unable to unlock the encrypted meta database with known keys.");
        }

        connection.ExecuteScalar<string>($"pragma hexkey = '{selectedKey}';");
        return connection;
    }
}
