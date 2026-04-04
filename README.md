# STS2-RitsuLib

Shared framework library for Slay the Spire 2 mods.

Chinese README: [README.zh.md](README.zh.md)

RitsuLib is maintained as a practical authoring library. API growth is demand-driven and focused on the patterns used by
the bundled mods.

The library exists alongside [BaseLib](https://github.com/Alchyr/BaseLib-StS2) and currently does not conflict with it.

Documentation index: [Docs/README.md](Docs/README.md)

## Mod Settings

RitsuLib includes a settings UI layer for player-editable values.

- register pages explicitly with `RitsuLibFramework.RegisterModSettings(...)`
- bind controls to `ModDataStore` instead of introducing a separate configuration backend
- source labels and descriptions from `I18N` or game-native `LocString`
- keep RitsuLib settings registration independent from BaseLib's config-page registry and file paths

Guide: [Docs/en/ModSettings.md](Docs/en/ModSettings.md)

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

## License

MIT
