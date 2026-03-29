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

RitsuLib provides a debug compatibility mode for selected runtime compatibility failures during iteration.

- Setting: debug_compatibility_mode
- Default: disabled (false)
- Behavior when enabled:
    - missing `LocTable` keys no longer throw immediately; they fall back to key placeholder text and emit a warning log
    - missing epoch ids encountered by RitsuLib unlock compatibility bridges are downgraded to warnings and skipped so
      the run can continue

Settings file path on Windows:

%appdata%\SlayTheSpire2\steam\<user_id>\mod_data\com.ritsukage.sts2-RitsuLib\settings.json

## License

MIT
