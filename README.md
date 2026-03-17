# STS2-RitsuLib

Shared framework library for Slay the Spire 2 mods.

## Features

- Shared framework bootstrap via `RitsuLibFramework`
- Unified logger and patcher factories (`CreateLogger`, `CreatePatcher`)
- Reusable patching infrastructure built on Harmony
- Expanded runtime lifecycle events (framework/profile init, game bootstrap, model registry, game ready, run start/end)
- Shared persistence lifecycle via `DataReadyLifecycle` (`ProfileDataReady`, `ProfileDataChanged`, `ProfileDataInvalidated`)
- Per-mod persistent storage via `ModDataStore`
- `using`-based batch registration via `RitsuLibFramework.BeginModDataRegistration(modId)` (auto unified load on scope exit)
- Content registration helper via `RitsuLibFramework.GetContentRegistry(modId)` for cards, relics, potions, characters, events, and ancients
- Timeline registration helper via `RitsuLibFramework.GetTimelineRegistry(modId)` for custom stories and epochs
- Unlock helper via `RitsuLibFramework.GetUnlockRegistry(modId)` for epoch-gated content and common post-run unlock rules
- Character/pool scaffolding helpers for type-list pools, character templates, and common epoch templates
- Card asset override helpers for portraits, frame materials, and custom/reused overlay scenes
- Multi-instance localization helpers (`CreateLocalization`, `CreateModLocalization`)

## License

MIT
