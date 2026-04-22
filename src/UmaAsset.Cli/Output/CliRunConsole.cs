namespace UmaAsset.Cli.Output;

public sealed class CliRunConsole
{
    private const int MaxContentWidth = 96;

    public int BoundedWidth => GetBoundedWidth();

    public void PrepareScreen()
    {
        if (!Console.IsOutputRedirected)
        {
            AnsiConsole.Clear();
        }
    }

    public void WriteBanner(CliApplicationInfo info)
    {
        var grid = new Grid()
            .AddColumn(new GridColumn().NoWrap());

        grid.AddRow(new Markup($"[bold]{Escape(info.Name)}[/]"));
        grid.AddRow(new Markup($"[grey]version:[/] [white]{Escape(info.Version)}[/]"));
        grid.AddRow(new Markup($"[grey]commit hash:[/] [white]{Escape(info.CommitHash)}[/]"));
        grid.AddRow(new Markup($"[grey]update available:[/] [white]{Escape(info.UpdateAvailable)}[/]"));
        grid.AddRow(new Markup($"[link={info.RepositoryUrl}]{Escape(info.RepositoryUrl)}[/]"));

        var panel = new Panel(Align.Center(grid))
        {
            Border = BoxBorder.Double,
            Padding = new Padding(2, 1, 2, 1),
            Width = GetBoundedWidth(),
        };
        panel.BorderStyle = Style.Parse("mediumpurple");

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    public void WriteMetadata(IEnumerable<string> lines)
    {
        var text = string.Join(Environment.NewLine, lines.Select(FormatMetadataLine));
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var panel = new Panel(new Markup(text))
        {
            Header = new PanelHeader("Run", Justify.Left),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0, 1, 0),
            Width = GetBoundedWidth(),
        };
        panel.BorderStyle = Style.Parse("grey");

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    public T RunPipelineProgress<T>(int totalPhases, Func<CliPipelineProgressReporter, T> action)
    {
        AnsiConsole.Write(new Rule("[bold]Current Progress[/]").LeftJustified());

        var normalizedPhases = Math.Max(1, totalPhases);
        if (Console.IsOutputRedirected)
        {
            var reporter = new CliPipelineProgressReporter(null, normalizedPhases, GetBoundedWidth(), interactive: false);
            reporter.Refresh();
            return action(reporter);
        }

        var initial = BuildProgressRenderable(new PipelineProgressSnapshot(normalizedPhases));
        return AnsiConsole.Live(initial)
            .AutoClear(false)
            .Overflow(VerticalOverflow.Crop)
            .Cropping(VerticalOverflowCropping.Top)
            .Start(context =>
            {
                var reporter = new CliPipelineProgressReporter(context, normalizedPhases, GetBoundedWidth(), interactive: true);
                reporter.Refresh();
                return action(reporter);
            });
    }

    public void WriteSection(string heading, IReadOnlyCollection<string> lines)
    {
        var text = string.Join(Environment.NewLine, lines.Count == 0 ? ["None"] : lines);

        var panel = new Panel(new Markup(Escape(text)))
        {
            Header = new PanelHeader(heading.TrimEnd(':'), Justify.Left),
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0, 1, 0),
            Width = GetBoundedWidth(),
        };
        panel.BorderStyle = Style.Parse(lines.Count == 0 ? "grey" : heading.StartsWith("Failed", StringComparison.OrdinalIgnoreCase) ? "red" : "green");

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    public void WriteSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[green]{Markup.Escape(message)}[/]");
    }

    public void WriteWarning(string message)
    {
        AnsiConsole.MarkupLine($"[yellow]{Markup.Escape(message)}[/]");
    }

    public void WriteError(string message)
    {
        AnsiConsole.MarkupLine($"[red]{Markup.Escape(message)}[/]");
    }

    internal static Spectre.Console.Rendering.IRenderable BuildProgressRenderable(PipelineProgressSnapshot snapshot)
    {
        const int gapWidth = 2;
        const int labelWidth = 35;
        var percentWidth = 5;
        var contentWidth = GetBoundedWidth() - 6;
        var barWidth = Math.Max(16, contentWidth - labelWidth - percentWidth - gapWidth);

        var progressRows = new Spectre.Console.Rendering.IRenderable[]
        {
            BuildProgressRow(snapshot.OverallSpinner, snapshot.OverallLabel, snapshot.OverallRatio, barWidth, labelWidth, percentWidth, "mediumpurple"),
            BuildProgressRow(snapshot.TargetSpinner, snapshot.TargetLabel, snapshot.TargetRatio, barWidth, labelWidth, percentWidth, "deepskyblue1"),
            BuildItemRow(snapshot.ItemSpinner, snapshot.ItemLabel, contentWidth),
        };

        var errorRows = snapshot.Errors.Count == 0
            ? new Spectre.Console.Rendering.IRenderable[] { new Markup("[grey]No errors[/]") }
            : snapshot.Errors
                .TakeLast(6)
                .Select(static error => new Markup($"[red]{Escape(error)}[/]"))
                .Cast<Spectre.Console.Rendering.IRenderable>()
                .ToArray();

        var statusRows = snapshot.Targets.Count == 0
            ? new Spectre.Console.Rendering.IRenderable[] { new Markup("[grey]No targets registered[/]") }
            : snapshot.Targets
                .Select(target => BuildTargetStatusRow(target, contentWidth))
                .ToArray();

        var group = new Rows(
        [
            ..progressRows,
            new Rule("[grey]Errors[/]").LeftJustified(),
            ..errorRows,
            new Rule("[grey]Succeeded[/]").LeftJustified(),
            ..statusRows,
        ]);
        var panel = new Panel(group)
        {
            Border = BoxBorder.Rounded,
            Padding = new Padding(1, 0, 1, 0),
            Width = GetBoundedWidth(),
        };
        panel.BorderStyle = Style.Parse("grey");
        return panel;
    }

    private static Spectre.Console.Rendering.IRenderable BuildProgressRow(
        string spinner,
        string label,
        double ratio,
        int barWidth,
        int labelWidth,
        int percentWidth,
        string color)
    {
        var clampedRatio = Math.Clamp(ratio, 0d, 1d);
        var filled = (int)Math.Round(clampedRatio * barWidth, MidpointRounding.AwayFromZero);
        filled = Math.Clamp(filled, 0, barWidth);
        var bar = $"{new string('=', filled)}{new string('─', barWidth - filled)}";
        var percent = $"{(int)Math.Round(clampedRatio * 100, MidpointRounding.AwayFromZero),3}%";
        var text = $"{spinner}  {PadOrTrim(label, labelWidth)}  [{color}]{Escape(bar)}[/]  [white]{percent.PadLeft(percentWidth)}[/]";
        return new Markup(text);
    }

    private static Spectre.Console.Rendering.IRenderable BuildItemRow(
        string spinner,
        string label,
        int contentWidth)
    {
        var availableLabelWidth = Math.Max(12, contentWidth - 4);
        return new Markup($"{spinner}  {PadOrTrim(label, availableLabelWidth)}");
    }

    private static Spectre.Console.Rendering.IRenderable BuildTargetStatusRow(
        PipelineTargetSnapshot target,
        int contentWidth)
    {
        const int percentWidth = 5;
        var labelWidth = Math.Max(18, Math.Min(40, contentWidth - percentWidth - 2));
        var percent = $"{target.SuccessPercent,3}%";
        var color = target.State switch
        {
            PipelineTargetState.Completed => "green",
            PipelineTargetState.InProgress => "yellow",
            _ => "white",
        };

        return new Markup($"[{color}]{PadOrTrim(target.Label, labelWidth)}[/]  [{color}]{percent.PadLeft(percentWidth)}[/]");
    }

    private static string Escape(string value) => Markup.Escape(value);

    private static string FormatMetadataLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return string.Empty;
        }

        if (line.StartsWith("  [", StringComparison.Ordinal))
        {
            return $"[grey]{Escape(line.Trim())}[/]";
        }

        if (line.StartsWith("    ", StringComparison.Ordinal))
        {
            return $"[white]{Escape(line.Trim())}[/]";
        }

        var separatorIndex = line.IndexOf(':');
        if (separatorIndex >= 0)
        {
            var label = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                return $"[grey]{Escape(label + ":")}[/]";
            }

            return $"[grey]{Escape(label + ":")}[/] [white]{Escape(value)}[/]";
        }

        return $"[white]{Escape(line)}[/]";
    }

    private static string PadOrTrim(string value, int width)
    {
        if (value.Length == width)
        {
            return Escape(value);
        }

        if (value.Length < width)
        {
            return Escape(value.PadRight(width));
        }

        if (width <= 1)
        {
            return Escape(value[..width]);
        }

        return Escape(value[..(width - 1)] + "…");
    }

    private static int GetBoundedWidth()
    {
        var width = AnsiConsole.Profile.Width;
        if (width <= 0)
        {
            return MaxContentWidth;
        }

        return Math.Min(MaxContentWidth, Math.Max(60, width - 2));
    }
}

internal sealed record PipelineProgressSnapshot(
    int TotalPhases,
    int CompletedPhases = 0,
    string OverallLabel = "Waiting",
    string TargetLabel = "Waiting",
    string ItemLabel = "Waiting",
    double TargetRatio = 0,
    double ItemRatio = 0,
    string OverallSpinner = "⠋",
    string TargetSpinner = "⠋",
    string ItemSpinner = "⠋",
    IReadOnlyList<PipelineTargetSnapshot>? Targets = null,
    IReadOnlyList<string>? Errors = null)
{
    public double OverallRatio => TotalPhases <= 0 ? 0 : (double)CompletedPhases / TotalPhases;
    public IReadOnlyList<PipelineTargetSnapshot> Targets { get; init; } = Targets ?? [];
    public IReadOnlyList<string> Errors { get; init; } = Errors ?? [];
}

internal sealed record PipelineTargetSnapshot(string Key, string Label, PipelineTargetState State, int SuccessPercent);

internal enum PipelineTargetState
{
    Pending,
    InProgress,
    Completed,
}

public sealed class CliPipelineProgressReporter
{
    private static readonly string[] SpinnerFrames = ["⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏"];

    private readonly LiveDisplayContext? context;
    private readonly bool interactive;
    private readonly int totalPhases;
    private PipelineProgressSnapshot snapshot;
    private int frameIndex;
    private int currentTargetTotal = 1;
    private int currentTargetCompleted;
    private int currentItemTotal = 1;
    private int currentItemCompleted;
    private string? lastPrinted;
    private string? currentTargetKey;
    private readonly Dictionary<string, PipelineTargetSnapshot> targets = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> errors = [];

    public CliPipelineProgressReporter(LiveDisplayContext? context, int totalPhases, int width, bool interactive)
    {
        this.context = context;
        this.totalPhases = totalPhases;
        this.interactive = interactive;
        snapshot = new PipelineProgressSnapshot(totalPhases, OverallLabel: "Preparing", TargetLabel: "Waiting", ItemLabel: "Waiting");
    }

    public void SeedTargets(IEnumerable<string> targetLabels)
    {
        foreach (var label in targetLabels)
        {
            if (targets.ContainsKey(label))
            {
                continue;
            }

            targets[label] = new PipelineTargetSnapshot(label, label, PipelineTargetState.Pending, 0);
        }

        UpdateCollections();
        Refresh();
    }

    public void StartPhase(string region, string target, int totalItems)
    {
        currentTargetKey = $"{region} // {target}";
        currentTargetTotal = Math.Max(1, totalItems);
        currentTargetCompleted = 0;
        currentItemTotal = Math.Max(1, totalItems);
        currentItemCompleted = 0;
        targets[currentTargetKey] = new PipelineTargetSnapshot(currentTargetKey, currentTargetKey, PipelineTargetState.InProgress, 0);

        snapshot = snapshot with
        {
            OverallLabel = region,
            TargetLabel = target,
            ItemLabel = $"Preparing {target}",
            TargetRatio = 0,
            ItemRatio = 0,
        };

        UpdateCollections();
        Tick();
    }

    public void AdvanceItem(string itemLabel, int steps = 1)
    {
        var increment = Math.Max(1, steps);
        currentTargetCompleted = Math.Min(currentTargetTotal, currentTargetCompleted + increment);
        currentItemCompleted = Math.Min(currentItemTotal, currentItemCompleted + increment);

        snapshot = snapshot with
        {
            ItemLabel = itemLabel,
            TargetRatio = (double)currentTargetCompleted / currentTargetTotal,
            ItemRatio = (double)currentItemCompleted / currentItemTotal,
        };

        UpdateCurrentTarget(PipelineTargetState.InProgress);
        Tick();
    }

    public void CompletePhase(string summary)
    {
        currentTargetCompleted = currentTargetTotal;
        currentItemCompleted = currentItemTotal;
        snapshot = snapshot with
        {
            CompletedPhases = Math.Min(totalPhases, snapshot.CompletedPhases + 1),
            ItemLabel = summary,
            TargetRatio = 1,
            ItemRatio = 1,
        };

        UpdateCurrentTarget(PipelineTargetState.Completed);
        Tick();
    }

    public void Finish(string summary)
    {
        snapshot = snapshot with
        {
            CompletedPhases = totalPhases,
            OverallLabel = summary,
            TargetLabel = "All targets complete",
            ItemLabel = "All items complete",
            TargetRatio = 1,
            ItemRatio = 1,
        };

        UpdateCollections();
        Tick();
    }

    public void ReportFailure(string message)
    {
        errors.Add(message);
        UpdateCollections();
        Refresh();
    }

    public void Refresh()
    {
        if (interactive && context is not null)
        {
            context.UpdateTarget(CliRunConsole.BuildProgressRenderable(snapshot));
            context.Refresh();
            return;
        }

        var line = $"{snapshot.OverallLabel} // {snapshot.TargetLabel}: {(int)Math.Round(snapshot.TargetRatio * 100, MidpointRounding.AwayFromZero)}%";
        if (!string.Equals(lastPrinted, line, StringComparison.Ordinal))
        {
            AnsiConsole.MarkupLine($"[mediumpurple]{Markup.Escape(line)}[/]");
            lastPrinted = line;
        }
    }

    private void Tick()
    {
        var frame = SpinnerFrames[frameIndex];
        frameIndex = (frameIndex + 1) % SpinnerFrames.Length;
        snapshot = snapshot with
        {
            OverallSpinner = frame,
            TargetSpinner = frame,
            ItemSpinner = frame,
        };
        Refresh();
    }

    private void UpdateCurrentTarget(PipelineTargetState state)
    {
        if (string.IsNullOrWhiteSpace(currentTargetKey) || !targets.TryGetValue(currentTargetKey, out var target))
        {
            return;
        }

        var successPercent = state == PipelineTargetState.Completed
            ? 100
            : (int)Math.Round(snapshot.TargetRatio * 100, MidpointRounding.AwayFromZero);
        targets[currentTargetKey] = target with
        {
            State = state,
            SuccessPercent = successPercent,
        };
        UpdateCollections();
    }

    private void UpdateCollections()
    {
        snapshot = snapshot with
        {
            Targets = targets.Values.ToArray(),
            Errors = errors.ToArray(),
        };
    }
}
