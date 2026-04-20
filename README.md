# UmaAssetCli

Solution-based .NET CLI for staging Umamusume assets from a local install.

This project copies and adapts parts of the extraction flow from
[`MarshmallowAndroid/UmamusumeExplorer`](https://github.com/MarshmallowAndroid/UmamusumeExplorer),
especially the encrypted `meta` access pattern and asset decryption logic.

Structure:

- `UmaAssetCli.sln`: solution entrypoint.
- `src/UmaAsset.Cli`: thin console app and command dispatch.
- `src/UmaAsset.Core`: shared models and contracts.
- `src/UmaAsset.Game`: local install detection, manifest access, decryption, and bundle extraction.
- `src/UmaAsset.Pipeline`: generated manifest/output shaping.
- `src/UmaAsset.External.GameTora`: optional GameTora metadata sync.

Current scope:

- Detect supported install locations automatically.
- Fetch GameTora metadata catalogs for characters and skills.
- Optionally fetch GameTora support metadata when requested.
- Open the encrypted `meta` database with the same known keys used by `UmamusumeExplorer`.
- Resolve manifest entries by resource name or basename.
- Copy raw asset bundles from `dat`, or write decrypted bundle files when the manifest entry uses an encryption key.
- Load Unity bundles directly with `AssetsTools.NET`.
- Export `Texture2D` assets to PNG files without relying on `ubt`.
- Resolve character icon bundle names directly from base or dress ids.

Examples:

```powershell
dotnet build .\UmaAssetCli.sln
dotnet run --project .\src\UmaAsset.Cli -- detect
dotnet run --project .\src\UmaAsset.Cli -- sync-gametora --output .\out\gametora
dotnet run --project .\src\UmaAsset.Cli -- sync-gametora --output .\out\gametora --include-supports
dotnet run --project .\src\UmaAsset.Cli -- lookup --name chr_icon_1058_105801_02 --json
dotnet run --project .\src\UmaAsset.Cli -- stage-chara-icons --ids 1058 105801 105802 --decrypt --output .\out\icons
dotnet run --project .\src\UmaAsset.Cli -- extract-chara-icons --ids 1058 105801 --output .\out\png
dotnet run --project .\src\UmaAsset.Cli -- extract-chara-icons --ids 1058 105801 --family chr --family trained --output .\out\organized
dotnet run --project .\src\UmaAsset.Cli -- generate-manifest --input .\out\organized --output .\out\organized\character-icons.json
```

Notes:

- `--uma-dir` should point at the game's `Persistent` directory when you need to override auto-detection.
- Package versions are managed centrally through `Directory.Packages.props`.
- Shared C# build settings live in `Directory.Build.props`.
- `sync-gametora` writes metadata-only catalogs and does not download remote images.
- `sync-gametora` always writes character and skill catalogs; pass `--include-supports` if you also want the support catalog.
- `stage-chara-icons` treats 4-digit ids as base character ids and 6-digit ids as trained/dress ids.
- `extract-chara-icons` resolves the same ids but exports PNG files directly from the Unity bundle.
- Repeat `--family` to include multiple icon families in one pass. Supported values are `chr`, `trained`, `round`, and `plus`.
- `extract-textures` can export all `Texture2D` assets in a bundle, or filter them with `--texture-name`.
- Character icon families are organized under `character/<characterId>/icons/<family>/`.
- Current family names are `icon`, `dress-icon`, `trained-icon`, `round-icon`, and `plus-icon` when those textures exist.
- `generate-manifest` writes a `character-icons.json` file keyed by character id with family-grouped relative asset paths.
- `--decrypt` writes a decrypted copy of the bundle file. It does not yet unpack Unity assets into PNGs.
- Encrypted `meta` support uses the `SQLite3MC.PCLRaw.bundle` package instead of the stock `SQLitePCLRaw` bundle.
- In this workspace, direct texture export is working for character icon bundles.
- `ubt` is not used by this CLI because its bundle parsing was not reliable against these files in local testing.

Credits:

- Upstream reference: `https://github.com/MarshmallowAndroid/UmamusumeExplorer`
- This CLI is intended as a focused command-line extraction pipeline, not a replacement for the explorer UI.
