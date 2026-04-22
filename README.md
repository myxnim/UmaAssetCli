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
- export raw atlas sprite bundles with generated browse indexes
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

If you want to run both installs in one pass, use:

- `--global-dir <path>`
- `--japan-dir <path>`

That keeps the regions explicit and avoids pairing mistakes with repeated `--uma-dir` values.

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
dotnet run --project .\src\UmaAsset.Cli -- extract-ui-icons --catalog ui-common-icons --catalog ui-rank-icons --global-dir "%USERPROFILE%\AppData\LocalLow\Cygames\umamusume" --japan-dir "D:\steamapps\common\UmamusumePrettyDerby_Jpn\UmamusumePrettyDerby_Jpn_Data\Persistent" --output .\out\ui
dotnet run --project .\src\UmaAsset.Cli -- extract-raw-atlases --global-dir "%USERPROFILE%\AppData\LocalLow\Cygames\umamusume" --japan-dir "D:\steamapps\common\UmamusumePrettyDerby_Jpn\UmamusumePrettyDerby_Jpn_Data\Persistent" --output .\out\raw-atlases
```

## Usage

Common help:

```powershell
dotnet run --project .\src\UmaAsset.Cli -- --help
dotnet run --project .\src\UmaAsset.Cli -- help lookup
dotnet run --project .\src\UmaAsset.Cli -- search --help
```

Exploration and lookup:

```powershell
dotnet run --project .\src\UmaAsset.Cli -- lookup --name common_tex --json
dotnet run --project .\src\UmaAsset.Cli -- search --contains support_card --limit 100
dotnet run --project .\src\UmaAsset.Cli -- inspect-bundle --name common_tex
dotnet run --project .\src\UmaAsset.Cli -- dump-asset --name rank_tex --asset-name utx_txt_rank_00
```

Ad hoc extraction:

```powershell
dotnet run --project .\src\UmaAsset.Cli -- extract-textures --name tex_support_card_10001 --output .\out\probe
dotnet run --project .\src\UmaAsset.Cli -- extract-sprites --name common_tex --sprite-name utx_ico_weather_00 --output .\out\probe
dotnet run --project .\src\UmaAsset.Cli -- extract-chara-icons --ids 1058 105801 --family chr --family trained --output .\out\character
dotnet run --project .\src\UmaAsset.Cli -- extract-support-icons --ids 10001 --output .\out\support
dotnet run --project .\src\UmaAsset.Cli -- extract-skill-icons --skill-ids 100011 10321 --output .\out\skills
```

Catalog and pipeline runs:

```powershell
dotnet run --project .\src\UmaAsset.Cli -- extract-ui-icons --catalog ui-common-icons --catalog ui-rank-icons --output .\out\ui
dotnet run --project .\src\UmaAsset.Cli -- extract-raw-atlases --preset extras --preset scenario --output .\out\raw-atlases
dotnet run --project .\src\UmaAsset.Cli -- sync-all --global-dir "%USERPROFILE%\AppData\LocalLow\Cygames\umamusume" --japan-dir "D:\steamapps\common\UmamusumePrettyDerby_Jpn\UmamusumePrettyDerby_Jpn_Data\Persistent" --output .\out\sync-all
```

## Notes

- `--uma-dir` should point at the game data directory that contains `dat/`, `meta`, and `master/master.mdb`.
- `extract-ui-icons` can process one copied install with `--uma-dir` or both regions together with `--global-dir` and `--japan-dir`.
- `extract-raw-atlases` can process one copied install with `--uma-dir` or both regions together with `--global-dir` and `--japan-dir`.
- exploratory commands are available too: `lookup`, `search`, `inspect-bundle`, `dump-asset`, `stage`, `extract-textures`, and `extract-sprites`.
- `sync-gametora` is metadata-oriented and does not download remote images.
- `extract-ui-icons` exports flattened PNGs under `<output>/<region>/icons/` and split catalogs under `<output>/<region>/catalogs/`.
- `extract-raw-atlases` exports atlas sprites under `<output>/<region>/raw-atlases/<atlas>/` and writes `<output>/<region>/catalogs/raw-atlas-index.json`.
- Current curated UI catalogs include common, racecommon, raceorder, rank, statusrank, circle, home, note, and teamstadium sets.
- Package versions are managed in `Directory.Packages.props`.
- Shared C# settings live in `Directory.Build.props`.

## Credits

- Upstream reference: `https://github.com/MarshmallowAndroid/UmamusumeExplorer`
