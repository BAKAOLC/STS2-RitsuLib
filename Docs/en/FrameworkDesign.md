# Framework Design

This document explains the architectural decisions behind RitsuLib and the constraints those decisions impose on mod code.

---

## Core Goals

RitsuLib is built around a small set of explicit design priorities:

- explicit registration instead of opaque “magic” discovery
- fixed model identity instead of runtime name inference
- composable asset records instead of large inheritance hierarchies
- scene replacement instead of in-place mutation of vanilla assets
- compatibility fallbacks only where the base game has no safe extension point

The framework reduces repetitive authoring work, but it does not convert the mod into an implicit runtime graph.

Optional **attribute-based** registration is still explicit: only types in assemblies registered with `ModTypeDiscoveryHub.RegisterModAssembly` are considered, each attribute maps to ordinary registry calls, and `AutoRegistrationAttribute.Inherit` defaults to **off** so derived types do not pick up base annotations unless you opt in. Details: [Content Packs & Registries](ContentPacksAndRegistries.md#attribute-based-registration-optional).

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
- migration between project structures does not depend on reflection order or class discovery behavior

The tradeoff is deliberate: renaming a published CLR type becomes a compatibility change.

---

## Registration Before Use

RitsuLib relies on explicit registration during early boot.

`CreateContentPack(modId)` is the convenience entry point, but the underlying registries remain first-class.

Registration is frozen during early boot to preserve:

- stable model identity
- stable model lists
- deterministic lookup and unlock behavior

The framework therefore fails fast instead of mutating the model graph after runtime systems have started consuming it.

See [Content Packs & Registries](ContentPacksAndRegistries.md) for the concrete registration model.

---

## Asset Profiles Instead Of Large Character Bases

Character authoring is organized around asset profiles.

Instead of requiring a monolithic custom-character base type with unrelated virtual members, RitsuLib groups assets into records such as:

- `CharacterSceneAssetSet`
- `CharacterUiAssetSet`
- `CharacterVfxAssetSet`
- `CharacterAudioAssetSet`

This keeps responsibility boundaries explicit:

- scenes live together
- UI assets live together
- VFX tuning lives together
- audio overrides live together

This is more verbose than a single placeholder property, but it scales better because each asset category can evolve independently.

---

## Asset Safety Mechanisms

The asset-profile system is paired with a small set of safety mechanisms:

- character placeholder fallback for missing character resources
- separate APIs for full energy-counter scenes versus pool-linked icons
- one-time warnings when explicit resource paths are missing

These behaviors are part of the same design: a structured asset API must remain usable during migration and partial-content development.

See [Asset Profiles & Fallbacks](AssetProfilesAndFallbacks.md) for the detailed behavior and API surface.

---

## Compatibility Layers Stay Narrow

RitsuLib includes compatibility-oriented patches, but they are intentionally narrow.

The framework does not hide every engine limitation behind automation. It adds fallbacks only where the game or modding surface would otherwise be unsafe or excessively repetitive.

Examples include `LocTable` and `THE_ARCHITECT` fallbacks under `debug_compatibility_mode`, ancient dialogue key injection, and unlock bridge patches for vanilla progression checks that skip mod characters.

See [Diagnostics & Compatibility](DiagnosticsAndCompatibility.md) for the concrete compatibility layers.

---

## Why The Patching Layer Exists

Harmony is still the underlying patch engine, but RitsuLib wraps it with:

- typed patch declarations via `IPatchMethod`
- critical vs optional patch semantics
- ignore-if-missing targets
- grouped registration helpers
- dynamic patch application support

The goal is not to abstract Harmony away. The goal is to standardize patch declaration and failure handling so large mods remain maintainable.

See [Patching Guide](PatchingGuide.md) for the patching workflow.

---

## Why Persistence Is Class-Based

Persistent entries are registered as class types rather than loose primitives.

That choice enables:

- schema version fields
- structured migrations
- future expansion without breaking call sites
- safer serialization boundaries

This adds some upfront structure, but avoids primitive save keys that later need to carry schema growth.

See [Persistence Guide](PersistenceGuide.md) for the full data model.

---

## Recommended Reading Order

- [Getting Started](GettingStarted.md)
- [Content Authoring Toolkit](ContentAuthoringToolkit.md)
- [Content Packs & Registries](ContentPacksAndRegistries.md)
- [Character & Unlock Templates](CharacterAndUnlockScaffolding.md)
- [Timeline & Unlocks](TimelineAndUnlocks.md)
- [Asset Profiles & Fallbacks](AssetProfilesAndFallbacks.md)
- [Patching Guide](PatchingGuide.md)
- [Persistence Guide](PersistenceGuide.md)
- [Localization & Keywords](LocalizationAndKeywords.md)
- [Diagnostics & Compatibility](DiagnosticsAndCompatibility.md)
