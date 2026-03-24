# Localization & Keywords

RitsuLib intentionally treats localization as two related but separate layers:

- the game's `LocString` model-key pipeline
- framework-owned helper localization via `I18N`

It also provides a small keyword registry so mod-defined terms can produce consistent hover tips and card text helpers.

---

## Game Model Localization

For registered models, the game still reads localization through normal tables such as:

- `cards`
- `relics`
- `powers`
- `characters`
- `card_keywords`

Those keys are based on the fixed `ModelId.Entry` described in [Content Authoring Toolkit](ContentAuthoringToolkit.md).

RitsuLib does not replace that system. It makes the identity side deterministic so those keys are easier to author.

---

## `CreateLocalization` And `CreateModLocalization`

For framework-owned helper text, use `I18N`:

```csharp
var i18n = RitsuLibFramework.CreateModLocalization(
    modId: "MyMod",
    instanceName: "MyMod-I18N",
    resourceFolders: ["MyMod.localization"],
    pckFolders: ["res://MyMod/localization"]);
```

`CreateModLocalization` is a convenience wrapper over `CreateLocalization`.
If you do not provide file-system folders, it defaults to:

```text
user://mod-configs/<modId>/localization
```

---

## Source Merge Order

`I18N` can merge translations from three source kinds:

1. file system folders
2. embedded resources
3. PCK folders

Merge behavior is first-wins, not last-wins:

- file-system entries are loaded first
- embedded entries only fill missing keys
- PCK entries only fill keys still missing after that

This lets local overrides take priority over packaged defaults.

---

## Language Normalization

`I18N` normalizes locale names before loading JSON files.

Examples:

- `en`, `en_us`, `eng` -> `eng`
- `zh`, `zh_cn`, `zh_hans` -> `zhs`
- `ja`, `ja_jp` -> `jpn`

If no language can be resolved, it falls back to `eng`.

---

## Runtime Reload Behavior

`I18N` subscribes to locale changes when possible.

That means:

- switching the game language reloads the helper localization
- `Changed` listeners are notified after reload
- if the game localization manager is unavailable, `I18N` falls back to lazy detection

This behavior is separate from normal `LocString` resolution.

---

## Debug Compatibility Mode

RitsuLib includes a debug-only compatibility shim for missing `LocTable` keys.

When `debug_compatibility_mode` is enabled:

- missing `LocTable.GetLocString(...)` no longer throws immediately
- missing `LocTable.GetRawText(...)` no longer throws immediately
- the framework returns a placeholder based on the key
- a one-time warning is logged

This is a debugging aid, not a replacement for correct localization.

---

## Keyword Registry

Use `ModKeywordRegistry` when you want reusable keyword definitions and hover tips:

```csharp
var keywords = RitsuLibFramework.GetKeywordRegistry("MyMod");

keywords.RegisterCardKeyword(
    id: "brew",
    locKeyPrefix: "my_mod_brew",
    iconPath: "res://MyMod/ui/keywords/brew.png");
```

This creates a normalized keyword id and binds it to title / description localization keys.

---

## Using Keywords In Code

Common helpers:

- `ModKeywordRegistry.CreateHoverTip(id)`
- `ModKeywordRegistry.GetTitle(id)`
- `ModKeywordRegistry.GetDescription(id)`
- `keywordId.GetModKeywordCardText()`
- `enumerable.ToHoverTips()`

You can also attach runtime keywords to arbitrary objects via `ModKeywordExtensions`:

```csharp
card.AddModKeyword("brew");

if (card.HasModKeyword("brew"))
{
    // ...
}
```

This is useful when keyword presence is driven by runtime state rather than static card text.

---

## Ancient Dialogue Localization

RitsuLib now includes `AncientDialogueLocalization`.

It serves two roles:

- helper API for building dialogue discovery from localization keys
- automatic append of localization-defined mod-character ancient dialogues before `AncientDialogueSet.PopulateLocKeys` runs

The expected key shape matches the base game:

- `<ancientEntry>.talk.<characterEntry>.<dialogueIndex>-<lineIndex>.ancient`
- `<ancientEntry>.talk.<characterEntry>.<dialogueIndex>-<lineIndex>.char`
- optional repeating suffix: `r`
- optional `.sfx`
- optional `-visit`
- architect-only optional `-attack`

This means authors can add character-specific ancient dialogue by writing localization entries, without manually patching each ancient dialogue set.

---

## Recommended Split

Use the right tool for the right job:

- game-facing card / relic / character text -> `LocString` tables
- framework-owned helper strings -> `I18N`
- reusable keyword definitions -> `ModKeywordRegistry`
- ancient event conversations -> localization keys plus `AncientDialogueLocalization`

---

## Related Documents

- [Content Authoring Toolkit](ContentAuthoringToolkit.md)
- [Character & Unlock Scaffolding](CharacterAndUnlockScaffolding.md)
- [Diagnostics & Compatibility](DiagnosticsAndCompatibility.md)
- [LocString Placeholder Resolution](LocStringPlaceholderResolution.md)
