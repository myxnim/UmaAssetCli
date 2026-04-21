namespace UmaAsset.Cli.Output;

public static class CliApplicationMetadata
{
    private const string RepositoryUrl = "https://github.com/myxnim/UmaAssetCli";

    public static CliApplicationInfo Get()
    {
        var version = GetVersion();
        var commitHash = TryGetCommitHash() ?? "n/a";

        return new CliApplicationInfo(
            "UMA ASSET CLI",
            version,
            commitHash,
            "unknown",
            RepositoryUrl);
    }

    private static string GetVersion()
    {
        var version = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version;
        if (version is null)
        {
            return "v1.0.0";
        }

        return $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    private static string? TryGetCommitHash()
    {
        var repoRoot = FindRepositoryRoot(Environment.CurrentDirectory)
            ?? FindRepositoryRoot(AppContext.BaseDirectory);
        if (repoRoot is null)
        {
            return null;
        }

        var gitPath = Path.Combine(repoRoot, ".git");
        if (Directory.Exists(gitPath))
        {
            return ReadHead(gitPath);
        }

        if (!File.Exists(gitPath))
        {
            return null;
        }

        var pointer = File.ReadAllText(gitPath).Trim();
        if (!pointer.StartsWith("gitdir:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relativeGitDir = pointer["gitdir:".Length..].Trim();
        var resolvedGitDir = Path.GetFullPath(Path.Combine(repoRoot, relativeGitDir));
        return Directory.Exists(resolvedGitDir) ? ReadHead(resolvedGitDir) : null;
    }

    private static string? FindRepositoryRoot(string? startPath)
    {
        if (string.IsNullOrWhiteSpace(startPath))
        {
            return null;
        }

        var current = new DirectoryInfo(Path.GetFullPath(startPath));
        while (current is not null)
        {
            if (Directory.Exists(Path.Combine(current.FullName, ".git"))
                || File.Exists(Path.Combine(current.FullName, ".git")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        return null;
    }

    private static string? ReadHead(string gitDirectory)
    {
        var headPath = Path.Combine(gitDirectory, "HEAD");
        if (!File.Exists(headPath))
        {
            return null;
        }

        var head = File.ReadAllText(headPath).Trim();
        if (!head.StartsWith("ref:", StringComparison.OrdinalIgnoreCase))
        {
            return ShortenHash(head);
        }

        var reference = head["ref:".Length..].Trim();
        var refPath = Path.Combine(gitDirectory, reference.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(refPath))
        {
            return ShortenHash(File.ReadAllText(refPath).Trim());
        }

        var packedRefsPath = Path.Combine(gitDirectory, "packed-refs");
        if (!File.Exists(packedRefsPath))
        {
            return null;
        }

        foreach (var line in File.ReadLines(packedRefsPath))
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#') || line.StartsWith('^'))
            {
                continue;
            }

            var separator = line.IndexOf(' ');
            if (separator <= 0)
            {
                continue;
            }

            if (!string.Equals(line[(separator + 1)..], reference, StringComparison.Ordinal))
            {
                continue;
            }

            return ShortenHash(line[..separator]);
        }

        return null;
    }

    private static string ShortenHash(string hash)
    {
        return hash.Length <= 8 ? hash : hash[..8];
    }
}

public sealed record CliApplicationInfo(
    string Name,
    string Version,
    string CommitHash,
    string UpdateAvailable,
    string RepositoryUrl);
