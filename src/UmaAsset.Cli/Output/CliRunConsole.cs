namespace UmaAsset.Cli.Output;

public sealed class CliRunConsole
{
    public void WriteBanner(CliApplicationInfo info)
    {
        var lines = new[]
        {
            info.Name,
            $"version: {info.Version}",
            $"commit hash: {info.CommitHash}",
            $"update available: {info.UpdateAvailable}",
            info.RepositoryUrl,
        };

        var width = Math.Max(58, lines.Max(static line => line.Length) + 8);
        var border = new string('/', width);
        Console.WriteLine(border);
        foreach (var line in lines)
        {
            Console.WriteLine($"//{Center(line, width - 4)}//");
        }

        Console.WriteLine(border);
        Console.WriteLine();
    }

    public void WriteMetadata(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }

        Console.WriteLine();
    }

    public CliProgressReporter BeginProgress(int totalSteps)
    {
        Console.WriteLine("Currently processing:");
        return new CliProgressReporter(totalSteps);
    }

    public void WriteSection(string heading, IReadOnlyCollection<string> lines)
    {
        Console.WriteLine(heading);
        if (lines.Count == 0)
        {
            Console.WriteLine("None");
            Console.WriteLine();
            return;
        }

        foreach (var line in lines)
        {
            Console.WriteLine(line);
        }

        Console.WriteLine();
    }

    private static string Center(string value, int width)
    {
        if (value.Length >= width)
        {
            return value;
        }

        var totalPadding = width - value.Length;
        var leftPadding = totalPadding / 2;
        var rightPadding = totalPadding - leftPadding;
        return $"{new string(' ', leftPadding)}{value}{new string(' ', rightPadding)}";
    }
}

public sealed class CliProgressReporter
{
    private readonly bool interactive;
    private readonly int totalSteps;
    private int completedSteps;
    private int previousLength;

    public CliProgressReporter(int totalSteps)
    {
        interactive = !Console.IsOutputRedirected;
        this.totalSteps = Math.Max(0, totalSteps);
    }

    public void Advance(string label, int steps = 1)
    {
        completedSteps = Math.Min(totalSteps, completedSteps + Math.Max(0, steps));
        if (!interactive)
        {
            return;
        }

        Render(label);
    }

    public void Finish(string label)
    {
        if (interactive)
        {
            Render(label);
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine($"[{BuildBar()}] {completedSteps}/{totalSteps} {label}");
        }
    }

    private void Render(string label)
    {
        var line = $"[{BuildBar()}] {completedSteps}/{totalSteps} {label}";
        var padded = previousLength > line.Length
            ? line + new string(' ', previousLength - line.Length)
            : line;

        Console.Write($"\r{padded}");
        previousLength = padded.Length;
    }

    private string BuildBar()
    {
        const int width = 28;
        if (totalSteps == 0)
        {
            return new string('-', width);
        }

        var filled = (int)Math.Round((double)completedSteps / totalSteps * width, MidpointRounding.AwayFromZero);
        filled = Math.Clamp(filled, 0, width);
        return $"{new string('#', filled)}{new string('-', width - filled)}";
    }
}
