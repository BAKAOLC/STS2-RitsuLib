# STS2-RitsuLib

A personal shared framework library for Slay the Spire 2 mods.

Chinese README: [README.zh.md](README.zh.md)

This library is primarily developed for personal use, so development pace is need-driven and not strictly scheduled.

It was created as an alternative to [BaseLib](https://github.com/Alchyr/BaseLib-StS2) due to design and coding style
differences.
There is currently no conflict between this library and BaseLib.

Documentation index: [Docs/README.md](Docs/README.md)

## Mod Settings API

RitsuLib now ships with a dedicated mod settings API and a built-in settings submenu.

- settings pages are registered explicitly through `RitsuLibFramework.RegisterModSettings(...)`
- UI bindings reuse `ModDataStore` instead of inventing a separate config backend
- text can come from either `I18N` or game-native `LocString`
- the menu is isolated from BaseLib's config button flow and does not share its registry or file paths

Guide: [Docs/en/ModSettings.md](Docs/en/ModSettings.md)

## Debug Compatibility Mode

Master switch `debug_compatibility_mode` defaults to **off**: RitsuLib does **not** soften `LocTable` misses, does **not
** skip invalid epoch grants (vanilla or explicit `InvalidOperationException`), and does **not** inject the
`THE_ARCHITECT` dialogue stub.

When the master switch is **on**, the in-game settings page shows **sub-toggles** (each defaults **on** so behavior
matches the previous single-toggle era):

| Sub-setting                    | Effect when on                                                 |
|--------------------------------|----------------------------------------------------------------|
| LocTable missing keys          | Placeholders + one-time `[Localization][DebugCompat]` warnings |
| Invalid unlock epochs          | Skip grant + one-time `[Unlocks][DebugCompat]` warnings        |
| THE_ARCHITECT missing dialogue | Empty-lines stub for `ModContentRegistry` characters           |

Turning a sub-toggle **off** while the master remains on forces **vanilla-style** behavior for that subsystem only.

Settings file path on Windows:

%appdata%\SlayTheSpire2\steam\<user_id>\mod_data\com.ritsukage.sts2-RitsuLib\settings.json

## License

MIT
