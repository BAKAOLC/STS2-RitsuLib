# Diagnostics & Compatibility

This document describes the safety and compatibility mechanisms RitsuLib adds on top of the base game.

It focuses on:

- One-time warnings that help authors catch mistakes early
- Debug-oriented behavior for missing localization and missing epochs
- Narrow bridge patches where vanilla systems do not support mod content

---

## Design Intent

RitsuLib does not hide every engine issue behind implicit magic. It follows these rules:

- Surface real errors as early as possible
- Where vanilla offers no safe extension point, the framework may add a bridge
- When a shim would hide too much, prefer staying explicit

This layer is deliberately narrow and only handles edge cases.

---

## One-Time Warning Policy

Some RitsuLib diagnostics warn only once per issue (or once per stable key), including:

- Missing resource paths (`AssetPathDiagnostics`)
- Missing `LocTable` keys when **master + LocTable** sub-toggle are on (`[Localization][DebugCompat]`)
- **`THE_ARCHITECT`** stub when debug compat master + ancient sub-toggle are on (`[Ancient]`)
- Other unlock-related one-shots (for example `ModUnlockMissingRuleWarnings`)

The goal is actionable logs: noticeable enough to act on, without spamming every frame.

---

## Asset Path Diagnostics

Explicit asset override paths are validated by `AssetPathDiagnostics`.

When a path is missing:

- A one-time warning is logged (host type, model id, member name, missing path)
- Behavior falls back to the original asset path or original behavior

This matters especially for character assets, where vanilla has almost no safe fallback.

See [Asset Profiles & Fallbacks](AssetProfilesAndFallbacks.md).

---

## Debug Compatibility Mode

> **Master** switch `debug_compatibility_mode` plus **per-subsystem** toggles in mod settings. When the master switch is off, **all** compatibility shims are inactive (vanilla `LocTable` throws, invalid epochs throw from `EpochRuntimeCompatibility`, no `THE_ARCHITECT` stub).

### Master off (default)

- No `LocTable` placeholder patches run.
- `EpochRuntimeCompatibility` throws `InvalidOperationException` when a compatibility path would use an invalid epoch id (explicit failure).
- No `THE_ARCHITECT` empty-dialogue injection.

### Master on

The settings UI expands a **Compatibility shims** section. Each sub-toggle defaults to **on** (preserving the old “single global toggle” behavior). A sub-toggle **off** restores vanilla behavior for that area only.

| Sub-toggle | When on |
|---|---|
| **LocTable missing keys** | Placeholder resolution + one-time `[Localization][DebugCompat]` warnings |
| **Invalid unlock epochs** | Skip the grant + one-time `[Unlocks][DebugCompat]` warnings (RitsuLib-owned unlock bridges only) |
| **THE_ARCHITECT missing dialogue** | Empty-lines stub for `ModContentRegistry` characters + one-time `[Ancient]` warning |

Scope for epoch handling remains RitsuLib-owned bridges (e.g. mod `ObtainCharUnlockEpoch` flow, registered boss/elite/post-run rules). **`ModUnlockMissingRuleWarnings`** (e.g. missing boss-win rule) still logs independently.

This mode does not replace correct localization or timeline registration.

Windows settings path:

```text
%appdata%\SlayTheSpire2\steam\<user_id>\mod_data\com.ritsukage.sts2-RitsuLib\settings.json
```

---

## Registration Conflict Diagnostics

RitsuLib checks these conflicts explicitly:

| Conflict | Typical cause |
|---|---|
| Model id collision | Two registered models in the same mod/category share the same CLR type name |
| Epoch id collision | Two epochs resolve to the same `Id` |
| Story id collision | Two stories resolve to the same story identity |

When detected, the framework throws or logs errors — it does not accept ambiguous identity silently.

---

## Ancient Dialogue Compatibility Layer

> This runs before vanilla `AncientDialogueSet.PopulateLocKeys`, extending vanilla behavior.

RitsuLib automatically appends localization-defined ancient dialogues for registered mod characters.

It is positioned as a compatibility convenience:

- Authors still author dialogue keys
- The framework discovers and appends them so mod characters follow the same ancient-dialogue pattern as vanilla

### `THE_ARCHITECT` dialogue fallback

Same as the **Ancient / THE_ARCHITECT** sub-toggle under debug compatibility (master must be on). If vanilla `TheArchitect.LoadDialogue` resolves **no** dialogue, RitsuLib injects an empty-lines stub for `ModContentRegistry` characters and logs **`[Ancient]`** once.

For key structure, see [Localization & Keywords](LocalizationAndKeywords.md).

---

## Unlock Compatibility Bridges

> This section explains vanilla progression limits for mod characters and RitsuLib’s bridging strategy.

Several vanilla progression checks assume vanilla characters. RitsuLib uses narrow bridge patches so registered unlock rules apply at those nodes for mod characters:

| Bridge | Description |
|---|---|
| Elite wins | Elite kill count → epoch checks |
| Boss wins | Boss kill count → epoch checks |
| Ascension 1 | Ascension 1 → epoch checks |
| Post-run character unlock | Post-run character-unlock epochs |
| Ascension reveal | Ascension reveal unlock checks |

These patches do not invent a second progression system; they forward RitsuLib-registered rules into vanilla checkpoints that would otherwise ignore mod characters.

See [Timeline & Unlocks](TimelineAndUnlocks.md).

---

## Freeze Errors

If content, timeline, or unlock registration runs after freeze, RitsuLib throws.

That is intentional: late registration often means ModelDb caches are already built, fixed identity rules are in use, and unlock filters are active. Failing fast is the safe choice.

---

## Recommended Debugging Mindset

1. Treat warnings as configuration issues first, not random instability
2. Fix missing assets and localization at the source
3. Use debug compatibility mode only while iterating
4. Do not rely on compatibility layers when a clean explicit API exists

The framework is meant to make problems visible, not hide them permanently.

---

## Related Documents

- [Asset Profiles & Fallbacks](AssetProfilesAndFallbacks.md)
- [Localization & Keywords](LocalizationAndKeywords.md)
- [Timeline & Unlocks](TimelineAndUnlocks.md)
- [Godot Scene Authoring](GodotSceneAuthoring.md)
- [Framework Design](FrameworkDesign.md)
