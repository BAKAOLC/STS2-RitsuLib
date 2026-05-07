# STS2-RitsuLib

Shared framework library for Slay the Spire 2 mods.

Chinese README: [README.zh.md](README.zh.md)

RitsuLib is maintained as a practical authoring library. API growth is demand-driven and focused on the patterns used by
the bundled mods.

The library exists alongside [BaseLib](https://github.com/Alchyr/BaseLib-StS2) and currently does not conflict with it.

Documentation site (Valaxy, bilingual): https://sts2-ritsulib.ritsukage.com/

## Optional analyzers

Companion Roslyn package for RitsuLib-style mods (localization keys, `Mod*Template` usage, registration attributes):
[STS2-ModAnalyzers-RitsuLib](https://github.com/BAKAOLC/STS2-ModAnalyzers-RitsuLib) (NuGet:
`STS2.ModAnalyzers.RitsuLib`).

## Mod Settings

RitsuLib includes a settings UI layer for player-editable values.

- register pages explicitly with `RitsuLibFramework.RegisterModSettings(...)`
- bind controls to `ModDataStore` instead of introducing a separate configuration backend
- source labels and descriptions from `I18N` or game-native `LocString`
- keep RitsuLib settings registration independent from BaseLib's config-page registry and file paths

Mod settings guide: https://sts2-ritsulib.ritsukage.com/guide/mod-settings

## Debug Compatibility Mode

`debug_compatibility_mode` defaults to **off**. In that state, patched systems keep vanilla behavior.

When the master toggle is **on**, the settings page exposes per-feature compatibility fallbacks. Sub-toggles default to
**on**.

| Sub-setting                    | Effect when enabled                                                                                 |
|--------------------------------|-----------------------------------------------------------------------------------------------------|
| LocTable missing keys          | Resolve to placeholder `LocString` values and log one `[Localization][DebugCompat]` warning per key |
| Invalid unlock epochs          | Skip invalid epoch grants and log one `[Unlocks][DebugCompat]` warning per stable key               |
| THE_ARCHITECT missing dialogue | Inject empty `Lines` entries for `ModContentRegistry` characters when vanilla provides no dialogue  |

Disabling a sub-toggle removes only that fallback.

Windows settings path:

`%appdata%\SlayTheSpire2\steam\<user_id>\mod_data\com.ritsukage.sts2-RitsuLib\settings.json`

## Runtime bundle (multi-API, interim)

End users who want **one mod folder** that picks the correct RitsuLib build for the running game should install the
GitHub
asset `STS2-RitsuLib.<version>.variant-pack.zip` (not the per-compat `*.github.zip` files). Extract it under
`mods/STS2-RitsuLib/`: the root `STS2-RitsuLib.dll` is a small loader; real builds live under `lib/<api-version>/` with
the same assembly name as today. Downstream mods keep declaring `dependencies: ["STS2-RitsuLib"]` and continue to
reference NuGet (`STS2.RitsuLib` / `STS2.RitsuLib.Compat.*`) unchanged. This path is expected to be temporary until
first-party workshop / per-branch installs make separate DLLs straightforward.

## License

MIT
