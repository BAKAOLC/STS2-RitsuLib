# Diagnostics & Compatibility

This document explains the small safety and compatibility layers RitsuLib adds around the base game.

It focuses on:

- one-time warnings that help authors find broken setup
- debug-only compatibility behaviors for missing localization and missing epoch ids
- narrow bridge patches for vanilla systems that do not naturally support mod content

---

## Design Intent

RitsuLib does not try to hide every engine problem behind implicit magic.

Instead, it follows a narrower rule:

- if the framework can surface a real mistake early, it should
- if the base game lacks a safe extension point, the framework may bridge it
- if a shim would hide too much behavior, the framework prefers to keep it explicit

So this layer is intentionally limited and targeted.

---

## One-Time Warning Policy

Several diagnostics in RitsuLib only warn once per unique issue.

That includes cases like:

- missing resource paths
- missing localization keys in debug compatibility mode

The goal is to keep logs actionable:

- visible enough to notice
- not noisy enough to flood every frame or every screen refresh

---

## Asset Path Diagnostics

Explicit asset overrides are validated through `AssetPathDiagnostics`.

When a resource-like override path is missing:

- the framework logs a warning once
- the base asset path or base behavior is used instead

This is especially important for character assets, where the base game offers very little safe fallback on its own.

Detailed asset semantics live in [Asset Profiles & Fallbacks](AssetProfilesAndFallbacks.md).

---

## Debug Compatibility Mode

RitsuLib includes a debug-only compatibility shim for selected runtime failures.

When `debug_compatibility_mode` is enabled:

- missing `LocTable.GetLocString(key)` returns a placeholder `LocString`
- missing `LocTable.GetRawText(key)` returns the key text itself
- the missing key is logged once with a warning
- missing epoch ids encountered by RitsuLib unlock compatibility bridges are logged once, skipped, and allowed to continue

This mode is disabled by default.

It exists to reduce interruption during iteration, not to replace correct localization data or correct timeline registration.

The missing-epoch downgrade only applies to compatibility paths owned by RitsuLib itself, such as:

- vanilla-style `ObtainCharUnlockEpoch(...)` handling for mod characters when the derived `...2_EPOCH/3_EPOCH/4_EPOCH` id is absent
- RitsuLib-managed boss / elite / ascension / post-run epoch unlock bridges

Warnings from this mode still indicate a real configuration problem that should be fixed in the mod.

Windows settings path:

```text
%appdata%\SlayTheSpire2\steam\<user_id>\mod_data\com.ritsukage.sts2-RitsuLib\settings.json
```

---

## Registration Conflict Diagnostics

RitsuLib performs explicit conflict checks for:

- model id collisions
- epoch id collisions
- story id collisions

These checks exist because fixed public identity is only useful if collisions are caught loudly.

Typical failure situations include:

- two registered model types in the same mod/category sharing the same CLR type name
- two epochs resolving to the same `Id`
- two stories resolving to the same story id

When collisions are detected, the framework throws or logs instead of silently accepting ambiguous identity.

---

## Ancient Dialogue Compatibility Layer

RitsuLib now auto-appends localization-defined ancient dialogues for registered mod characters before `AncientDialogueSet.PopulateLocKeys` runs.

This is a compatibility convenience layer, not a replacement for author intent:

- you still own the localization keys
- the framework just discovers and appends the dialogues so mod characters participate in the same ancient dialogue pattern as base characters

Detailed key structure lives in [Localization & Keywords](LocalizationAndKeywords.md).

---

## Unlock Compatibility Bridges

Vanilla progression checks were built around vanilla characters.

RitsuLib adds narrow bridge patches so registered unlock rules can drive equivalent behavior for mod characters in places such as:

- elite-win based epoch unlocks
- boss-win based epoch unlocks
- ascension-one epoch unlocks
- post-run character unlock epochs
- ascension reveal checks

These patches do not invent a second progression system. They only forward registered RitsuLib rules into the vanilla checkpoints that would otherwise ignore mod characters.

Detailed rule semantics live in [Timeline & Unlocks](TimelineAndUnlocks.md).

---

## Freeze Errors Are Intentional Diagnostics

When content, timeline, or unlock registration is attempted after freeze, RitsuLib throws.

This should be understood as a diagnostic feature, not a usability bug.

Late registration would mean:

- ModelDb caches may already be warm
- fixed identity may already be relied on
- unlock filters may already be running

So the framework fails hard rather than letting the mod continue in a partial state.

---

## Recommended Debugging Mindset

When you see a compatibility or diagnostic warning:

1. treat it as a setup issue first, not as random engine instability
2. fix missing assets or missing localization at the source
3. use debug compatibility mode only while iterating
4. avoid depending on compatibility shims where a clean explicit API already exists

The framework is trying to make mistakes visible, not make them disappear.

---

## Related Documents

- [Asset Profiles & Fallbacks](AssetProfilesAndFallbacks.md)
- [Localization & Keywords](LocalizationAndKeywords.md)
- [Timeline & Unlocks](TimelineAndUnlocks.md)
- [Godot Scene Authoring](GodotSceneAuthoring.md)
- [Framework Design](FrameworkDesign.md)
