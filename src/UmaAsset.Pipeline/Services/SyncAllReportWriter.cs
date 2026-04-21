using System.Text.Json;

namespace UmaAsset.Pipeline.Services;

public static class SyncAllReportWriter
{
    public static string Write(SyncAllReport report, string outputFile)
    {
        var json = JsonSerializer.Serialize(report, new JsonSerializerOptions
        {
            WriteIndented = true,
        });

        var fullOutputPath = Path.GetFullPath(outputFile);
        Directory.CreateDirectory(Path.GetDirectoryName(fullOutputPath)!);
        File.WriteAllText(fullOutputPath, json);
        return fullOutputPath;
    }
}
