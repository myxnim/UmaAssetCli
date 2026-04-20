# UmaAssetCli

Small .NET CLI for staging Umamusume assets from a local install.

This project copies and adapts parts of the extraction flow from
[`MarshmallowAndroid/UmamusumeExplorer`](https://github.com/MarshmallowAndroid/UmamusumeExplorer),
especially the encrypted `meta` access pattern and asset decryption logic.

Current scope:

- Detect supported install locations automatically.
- Open the encrypted `meta` database with the same known keys used by `UmamusumeExplorer`.
- Resolve manifest entries by resource name or basename.
- Copy raw asset bundles from `dat`, or write decrypted bundle files when the manifest entry uses an encryption key.
- Load Unity bundles directly with `AssetsTools.NET`.
- Export `Texture2D` assets to PNG files without relying on `ubt`.
- Resolve character icon bundle names directly from base or dress ids.

Examples:

```powershell
dotnet run --project UmaAssetCli -- detect
dotnet run --project UmaAssetCli -- lookup --name chr_icon_1058_105801_02 --json
dotnet run --project UmaAssetCli -- stage-chara-icons --ids 1058 105801 105802 --decrypt --output .\out\icons
dotnet run --project UmaAssetCli -- extract-chara-icons --ids 1058 105801 --output .\out\png
```

Notes:

- `--uma-dir` should point at the game's `Persistent` directory when you need to override auto-detection.
- `stage-chara-icons` treats 4-digit ids as base character ids and 6-digit ids as trained/dress ids.
- `extract-chara-icons` resolves the same ids but exports PNG files directly from the Unity bundle.
- `extract-textures` can export all `Texture2D` assets in a bundle, or filter them with `--texture-name`.
- Character icon families are organized under `character/<characterId>/icons/<family>/`.
- Current family names are `icon`, `dress-icon`, `trained-icon`, `round-icon`, and `plus-icon` when those textures exist.
- `--decrypt` writes a decrypted copy of the bundle file. It does not yet unpack Unity assets into PNGs.
- Encrypted `meta` support uses the `SQLite3MC.PCLRaw.bundle` package instead of the stock `SQLitePCLRaw` bundle.
- In this workspace, direct texture export is working for character icon bundles.
- `ubt` is not used by this CLI because its bundle parsing was not reliable against these files in local testing.

Credits:

- Upstream reference: `https://github.com/MarshmallowAndroid/UmamusumeExplorer`
- This CLI is intended as a focused command-line extraction pipeline, not a replacement for the explorer UI.
