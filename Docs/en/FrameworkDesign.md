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

RitsuLib splits authoring into registries:

- content registry
- keyword registry
- timeline registry
- unlock registry
- persistent data store

`CreateContentPack(modId)` is the ergonomic entry point, but the underlying registries still exist and remain explicit.

The framework freezes content registration during early boot. This is intentional:

- model identity is finalized once
- later lookups stay deterministic
- runtime "surprise registration" bugs are avoided

The design prefers early failure over silently mutating the model graph after the game has started using it.

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

## Placeholder Fallback Is A Safety Layer

The base game does not provide robust fallback handling for missing character assets.

That makes placeholder inheritance necessary, not merely convenient.

RitsuLib now handles this through `ModCharacterTemplate.PlaceholderCharacterId`:

- default value: `ironclad`
- missing character assets are filled from that base-character profile
- returning `null` disables fallback entirely

This keeps the framework-style explicit profile system, while removing the migration pain of manually filling merchant, rest-site, map marker, and default SFX paths one by one.

The important design detail is that placeholder fallback is additive, not authoritative:

- your explicit asset profile stays the source of truth
- only missing entries are inherited

---

## Custom Energy Counter vs Pool Energy Icons

RitsuLib treats these as different layers:

- `CustomEnergyCounterPath`: the combat counter scene itself
- `BigEnergyIconPath`: the large energy icon resolved through `EnergyIconHelper`
- `TextEnergyIconPath`: the small inline icon used in formatted text

This split is intentional.

The combat counter is a scene-level UI concern and deserves a scene-level API.
The pool icons are content-pipeline concerns and belong on pool models.

That separation keeps the clean scene-based approach for full counters while still providing convenience APIs for the icon use cases authors expect.

---

## Missing Paths Warn And Fall Back

RitsuLib now validates explicit asset paths more aggressively.

Current behavior:

- if an override path is empty, it is ignored
- if an override path exists, it is used
- if an override path is missing, RitsuLib logs a one-time warning and falls back to the base asset

This is especially important for character assets, where a bad path can otherwise surface much later as a load failure in unrelated UI.

The framework keeps the warning one-time so that a bad path is visible without flooding logs every frame or every screen refresh.

---

## Compatibility Shims Live At The Edges

RitsuLib does include compatibility-oriented helpers, but they are kept narrow:

- localization debug compatibility mode for missing `LocTable` keys
- ancient dialogue auto-discovery from localization keys
- unlock progression bridge patches where vanilla hooks are incomplete

The framework tries not to make every system magical by default.
Instead, it adds shims where the game or modding surface would otherwise be unsafe or needlessly repetitive.

---

## Why The Patching Layer Exists

Harmony is still the underlying patch engine, but RitsuLib wraps it with:

- typed patch declarations via `IPatchMethod`
- critical vs optional patch semantics
- ignore-if-missing targets
- grouped registration helpers
- dynamic patch application support

The goal is not to hide Harmony. The goal is to standardize patch shape and failure handling so large mods stay maintainable.

See [PatchingGuide.md](PatchingGuide.md) for the patching workflow.

---

## Why Persistence Is Class-Based

Persistent entries are registered as class types rather than loose primitives.

That choice enables:

- schema version fields
- structured migrations
- future expansion without breaking call sites
- safer serialization boundaries

It is slightly more ceremony up front, but it avoids the long-term pain of primitive save keys that outgrow their original shape.

See [PersistenceGuide.md](PersistenceGuide.md) for the full data model.

---

## Recommended Reading Order

- [GettingStarted.md](GettingStarted.md)
- [ContentAuthoringToolkit.md](ContentAuthoringToolkit.md)
- [CharacterAndUnlockScaffolding.md](CharacterAndUnlockScaffolding.md)
- [PatchingGuide.md](PatchingGuide.md)
- [PersistenceGuide.md](PersistenceGuide.md)
- [LocalizationAndKeywords.md](LocalizationAndKeywords.md)
