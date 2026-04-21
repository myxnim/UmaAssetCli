# UmaAssetCli

.NET CLI for extracting Umamusume assets and generating app-ready catalogs from a local install.

This project copies and adapts parts of the extraction flow from
[`MarshmallowAndroid/UmamusumeExplorer`](https://github.com/MarshmallowAndroid/UmamusumeExplorer),
especially encrypted `meta` access and bundle decryption.

Structure:

- `UmaAssetCli.sln`
- `src/UmaAsset.Cli`
- `src/UmaAsset.Core`
- `src/UmaAsset.Game`
- `src/UmaAsset.Pipeline`
- `src/UmaAsset.External.GameTora`

Current scope:

- detect supported installs automatically
- read encrypted `meta`
- stage bundle copies before parsing them
- export character, support, skill, and curated UI icons
- generate split JSON catalogs for atlas-backed UI icons
- optionally sync GameTora metadata catalogs

## Safe usage

If you do not pass `--uma-dir`, the CLI looks in standard install locations first.

If you want the safest workflow, copy the game's data directory somewhere temporary and point the CLI at that copy.

The directory you pass should be the one that contains:

- `dat/`
- `meta`
- `master/master.mdb`

For JP Steam installs that is typically `Persistent`. For global installs it is typically the LocalLow game directory itself.

```powershell
robocopy "D:\Stuff\Games\Steam\steamapps\common\UmamusumePrettyDerby_Jpn\UmamusumePrettyDerby_Jpn_Data\Persistent" "C:\temp\uma-jp-copy" /MIR
dotnet run --project .\src\UmaAsset.Cli -- extract-ui-icons --uma-dir C:\temp\uma-jp-copy --region japan --output .\out\ui
```

The CLI now stages bundle files to temp before opening them, but using a copied `Persistent` directory is still the least alarming workflow for users.

## Region

For standard detected installs, region is inferred automatically.

For `--uma-dir` paths, region is inferred from the install name/path when possible. If you point at a copied folder with a generic name, pass:

- `--region global`
- `--region japan`

That matters for region-scoped output paths such as `global/...` and `japan/...`.

## Examples

```powershell
dotnet build .\UmaAssetCli.sln
dotnet run --project .\src\UmaAsset.Cli -- detect
dotnet run --project .\src\UmaAsset.Cli -- sync-gametora --output .\out\gametora --include-supports
dotnet run --project .\src\UmaAsset.Cli -- lookup --name chr_icon_1058_105801_02 --json
dotnet run --project .\src\UmaAsset.Cli -- extract-chara-icons --ids 1058 105801 --family chr --family trained --output .\out\organized
dotnet run --project .\src\UmaAsset.Cli -- extract-skill-icons --skill-ids 100011 10321 --output .\out\skill-icons
dotnet run --project .\src\UmaAsset.Cli -- extract-ui-icons --catalog ui-common-icons --catalog ui-home-icons --output .\out\ui
dotnet run --project .\src\UmaAsset.Cli -- extract-ui-icons --catalog ui-common-icons --uma-dir C:\temp\uma-jp-copy --region japan --output .\out\ui
```

## Notes

- `--uma-dir` should point at the game data directory that contains `dat/`, `meta`, and `master/master.mdb`.
- `sync-gametora` is metadata-oriented and does not download remote images.
- `extract-ui-icons` exports flattened PNGs under `<output>/<region>/icons/` and split catalogs under `<output>/<region>/catalogs/`.
- Current curated UI catalogs include common, racecommon, raceorder, rank, statusrank, home, note, and teamstadium sets.
- Package versions are managed in `Directory.Packages.props`.
- Shared C# settings live in `Directory.Build.props`.

## Credits

- Upstream reference: `https://github.com/MarshmallowAndroid/UmamusumeExplorer`
