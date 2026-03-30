# Framework Design

This document explains the high-level design choices behind RitsuLib so authors can understand not only what the APIs do, but why they are shaped this way.

---

## Core Goals

RitsuLib is built around a few strong preferences:

- explicit registration over hidden magic
- fixed model identity over runtime name guessing
- composable templates over giant inheritance trees
- clean Godot scene replacement over patching vanilla assets in place
- compatibility shims only where the base game genuinely lacks safe extension points

In practice, that means the framework tries to make common work shorter without turning the whole mod into implicit behavior.

---

## Fixed Identity

For models registered through the RitsuLib content registry, `ModelId.Entry` is deterministic:

```text
<MODID>_<CATEGORY>_<TYPENAME>
```

Why this matters:

- localization keys stay stable and predictable
- refactors are easier to reason about
- content registration conflicts are easier to detect
- migration from one project structure to another does not depend on reflection order or class discovery quirks

The tradeoff is deliberate: renaming a published CLR type is now a compatibility decision, not a harmless cleanup.

---

## Registration Before Use

RitsuLib is built around explicit early registration.

`CreateContentPack(modId)` is the ergonomic entry point, but the underlying registries stay first-class and explicit.

The framework freezes registration during early boot because it wants:

- stable model identity
- stable model lists
- deterministic lookup and unlock behavior

So the design prefers early failure over silently mutating the model graph after the game has started using it.

See [Content Packs & Registries](ContentPacksAndRegistries.md) for the concrete registration model.

---

## Asset Profiles Instead Of Giant Character Bases

One of the clearest design choices is the asset-profile approach.

Instead of forcing every author into a monolithic custom-character base type with many unrelated virtual members, RitsuLib groups assets into records such as:

- `CharacterSceneAssetSet`
- `CharacterUiAssetSet`
- `CharacterVfxAssetSet`
- `CharacterAudioAssetSet`

That structure is meant to make intent obvious:

- scenes live together
- UI assets live together
- VFX tuning lives together
- audio overrides live together

This is more verbose than a single placeholder property, but it scales better as a framework because it keeps categories separated and easier to extend.

---

## Asset Safety Rails

The asset-profile system is intentionally paired with a few safety rails:

- character placeholder fallback for missing character resources
- separate APIs for full energy-counter scenes versus pool-linked icons
- one-time warnings when explicit resource paths are missing

These are not separate design accidents. They exist so a structured asset API stays practical during real mod authoring and migration.

See [Asset Profiles & Fallbacks](AssetProfilesAndFallbacks.md) for the detailed behavior and API surface.

---

## Compatibility Shims Live At The Edges

RitsuLib does include compatibility-oriented helpers, but they are kept narrow.

The framework tries not to make every system magical by default. Instead, it adds shims where the game or modding surface would otherwise be unsafe or needlessly repetitive.

Representative examples include optional `LocTable` soft-fail (`debug_compatibility_mode`), ancient dialogue append plus the narrow `THE_ARCHITECT` stub for registry characters (independent of that flag), and unlock bridge patches for vanilla progression hooks that ignore mod characters.

See [Diagnostics & Compatibility](DiagnosticsAndCompatibility.md) for the concrete compatibility layers.

---

## Why The Patching Layer Exists

Harmony is still the underlying patch engine, but RitsuLib wraps it with:

- typed patch declarations via `IPatchMethod`
- critical vs optional patch semantics
- ignore-if-missing targets
- grouped registration helpers
- dynamic patch application support

The goal is not to hide Harmony. The goal is to standardize patch shape and failure handling so large mods stay maintainable.

See [Patching Guide](PatchingGuide.md) for the patching workflow.

---

## Why Persistence Is Class-Based

Persistent entries are registered as class types rather than loose primitives.

That choice enables:

- schema version fields
- structured migrations
- future expansion without breaking call sites
- safer serialization boundaries

It is slightly more ceremony up front, but it avoids the long-term pain of primitive save keys that outgrow their original shape.

See [Persistence Guide](PersistenceGuide.md) for the full data model.

---

## Recommended Reading Order

- [Getting Started](GettingStarted.md)
- [Content Authoring Toolkit](ContentAuthoringToolkit.md)
- [Content Packs & Registries](ContentPacksAndRegistries.md)
- [Character & Unlock Scaffolding](CharacterAndUnlockScaffolding.md)
- [Timeline & Unlocks](TimelineAndUnlocks.md)
- [Asset Profiles & Fallbacks](AssetProfilesAndFallbacks.md)
- [Patching Guide](PatchingGuide.md)
- [Persistence Guide](PersistenceGuide.md)
- [Localization & Keywords](LocalizationAndKeywords.md)
- [Diagnostics & Compatibility](DiagnosticsAndCompatibility.md)
