namespace UmaAsset.Game.Services;

public sealed record BundleAssetInfo(
    string AssetsFileName,
    long PathId,
    int TypeId,
    string TypeName,
    string Name);
