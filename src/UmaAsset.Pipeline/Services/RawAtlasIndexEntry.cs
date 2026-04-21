namespace UmaAsset.Pipeline.Services;

public sealed record RawAtlasIndexEntry(
    string Region,
    string Atlas,
    string SpriteName,
    string RelativePath);
