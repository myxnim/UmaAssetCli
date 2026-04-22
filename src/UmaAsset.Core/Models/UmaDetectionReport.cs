namespace UmaAsset.Core.Models;

public sealed class UmaDetectionReport
{
    public List<UmaInstall> Installs { get; set; } = [];

    public List<UmaDetectionProbe> Probes { get; set; } = [];
}

public sealed record UmaDetectionProbe(
    string Name,
    string Region,
    string Source,
    string Path,
    bool IsValid);
