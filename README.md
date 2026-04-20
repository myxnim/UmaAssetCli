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
- Resolve character icon bundle names directly from base or dress ids.
- Provide a clean first stage for a future in-tool Unity asset unpack step.

Examples:

```powershell
dotnet run --project UmaAssetCli -- detect
dotnet run --project UmaAssetCli -- lookup --name chr_icon_1058_105801_02 --json
dotnet run --project UmaAssetCli -- stage-chara-icons --ids 1058 105801 105802 --decrypt --output .\out\icons
```

Notes:

- `--uma-dir` should point at the game's `Persistent` directory when you need to override auto-detection.
- `stage-chara-icons` treats 4-digit ids as base character ids and 6-digit ids as trained/dress ids.
- `--decrypt` writes a decrypted copy of the bundle file. It does not yet unpack Unity assets into PNGs.
- Encrypted `meta` support uses the `SQLite3MC.PCLRaw.bundle` package instead of the stock `SQLitePCLRaw` bundle.
- In this workspace, manifest lookup and decrypted bundle staging are working. The next stage should use `AssetsTools.NET` directly for texture export instead of relying on `ubt`.

Credits:

- Upstream reference: `https://github.com/MarshmallowAndroid/UmamusumeExplorer`
- This CLI is intended as a focused command-line extraction pipeline, not a replacement for the explorer UI.
