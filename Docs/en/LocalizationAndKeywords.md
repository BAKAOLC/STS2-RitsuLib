# Localization & Keywords

RitsuLib separates localization into two distinct layers:

- **The base game's `LocString` model-key pipeline** — in-game text such as model titles and descriptions
- **Framework-provided `I18N` helper localization** — auxiliary text for the mod itself

It also provides a lightweight keyword registry to unify hover tips and keyword text.

---

## Game Model Localization

> The following describes the game engine's own localization mechanism; RitsuLib does not replace this system.

The game reads model text through `LocString` and various localization tables, commonly including:

- `cards`
- `relics`
- `powers`
- `characters`
- `card_keywords`

Those keys are built on `ModelId.Entry`.

RitsuLib's role is limited to making model identity more stable and predictable so keys are easier to author. For concrete model ID rules, see [Content Authoring Toolkit](ContentAuthoringToolkit.md).

---

## `CreateLocalization` And `CreateModLocalization`

`I18N` is RitsuLib's helper-text localization system, independent of the game's `LocString`:

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

Merge behavior is first-wins:

- file-system entries are loaded first
- embedded entries only fill missing keys
- PCK entries only fill keys still missing after that

This lets local overrides take priority over packaged defaults.

---

## Language Normalization

`I18N` normalizes locale names before loading JSON files:

| Input | Normalized |
|---|---|
| `en`, `en_us`, `eng` | `eng` |
| `zh`, `zh_cn`, `zh_hans` | `zhs` |
| `ja`, `ja_jp` | `jpn` |

If no language can be resolved, it falls back to `eng`.

---

## Runtime Reload Behavior

`I18N` subscribes to locale changes when possible:

- when the game language changes, helper localization reloads automatically
- `Changed` is raised after reload completes
- if the game localization manager is unavailable at that moment, `I18N` falls back to lazy detection

This behavior is independent of base-game `LocString` resolution.

---

## Debug Compatibility Mode

`LocTable` placeholder resolution is part of RitsuLib’s debug compatibility fallbacks. See [Diagnostics & Compatibility](DiagnosticsAndCompatibility.md) for the master toggle, the **LocTable missing keys** toggle, and one-time `[Localization][DebugCompat]` warnings.

Use this for troubleshooting, not as a substitute for authoring real keys.

---

## Keyword Registry

Use `ModKeywordRegistry` when you want reusable keyword definitions and hover tips:

```csharp
var keywords = RitsuLibFramework.GetKeywordRegistry("MyMod");

keywords.RegisterCardKeywordOwnedByLocNamespace(
    localKeywordStem: "brew",
    iconPath: "res://MyMod/ui/keywords/brew.png");
```

This creates a normalized keyword id and binds it to title / description localization keys.

---

## Automatic keyword registration (optional: CLR attributes)

If you already use `ModTypeDiscoveryHub.RegisterModAssembly(...)` to let RitsuLib scan your assemblies, you can declare keyword registration with CLR attributes:

```csharp
using STS2RitsuLib.Interop.AutoRegistration;

[RegisterOwnedCardKeyword("brew", LocNamespace = "my_mod", IconPath = "res://MyMod/ui/keywords/brew.png")]
public sealed class BrewKeywordMarker;
```

`LocNamespace` only affects the localization namespace (the `modid` portion). The keyword stem (`brew`) participates in the default rule `<namespace>_<keyword>`, producing:

- `<namespace>_<keyword>.title`
- `<namespace>_<keyword>.description`

> Compatibility note: the legacy `LocKeyPrefix` / `locKeyPrefix` historically represents the **full stem** and is easy to misread as a prefix + keyword composition, so it is now obsolete. Use `LocNamespace` for new code.

---

## Using Keywords In Code

Common helpers:

| Method | Description |
|---|---|
| `ModKeywordRegistry.CreateHoverTip(id)` | Create hover tip |
| `ModKeywordRegistry.GetTitle(id)` | Get title |
| `ModKeywordRegistry.GetDescription(id)` | Get description |
| `keywordId.GetModKeywordCardText()` | Get card text |
| `enumerable.ToHoverTips()` | Batch-convert to hover tips |

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

RitsuLib includes `AncientDialogueLocalization`. It serves two roles:

- helper API for scanning dialogue from localization keys
- automatic append of localization-defined mod-character ancient dialogues before `AncientDialogueSet.PopulateLocKeys` runs

The key format matches the base game:

| Key component | Description |
|---|---|
| `<ancientEntry>.talk.<characterEntry>.<dialogueIndex>-<lineIndex>.ancient` | Ancient line |
| `<ancientEntry>.talk.<characterEntry>.<dialogueIndex>-<lineIndex>.char` | Character line |
| Optional suffix `r` | Repeated dialogue |
| Optional suffix `.sfx` | Sound effect |
| Optional suffix `-visit` | Visit override |
| Optional suffix `-attack` | Architect-only attacker override |

Authors only need to write localization entries to add ancient dialogue for custom characters, without manually patching each `AncientDialogueSet`.

If **no** keys exist for an ancient, vanilla may still show `PROCEED` for `THE_ARCHITECT` while `WinRun` assumes `Dialogue` is non-null. RitsuLib adds a narrow compatibility fallback (empty `Lines`, safe attackers) for `ModContentRegistry` characters **only** when the debug compatibility master toggle and the **THE_ARCHITECT missing dialogue** toggle are enabled, with a one-time `[Ancient]` warning.

---

## Recommended Split

| Use case | Tool |
|---|---|
| Game model text (titles, descriptions) | Base game `LocString` tables |
| Mod-owned auxiliary text (settings, explanations) | `I18N` |
| Reusable keyword definitions | `ModKeywordRegistry` |
| Ancient dialogue | Localization keys + `AncientDialogueLocalization` |

---

## Related Documents

- [Content Authoring Toolkit](ContentAuthoringToolkit.md)
- [Character & Unlock Templates](CharacterAndUnlockScaffolding.md)
- [Diagnostics & Compatibility](DiagnosticsAndCompatibility.md)
- [LocString Placeholder Resolution](LocStringPlaceholderResolution.md)
- [Mod Settings UI](ModSettings.md)
