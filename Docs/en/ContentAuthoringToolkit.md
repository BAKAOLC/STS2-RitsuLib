# Content Authoring Toolkit

This document is the overview for content authoring: registration entry points, model identity, localization coupling, and asset override basics.

Detailed registration mechanics live in [Content Packs & Registries](ContentPacksAndRegistries.md). Detailed asset semantics live in [Asset Profiles & Fallbacks](AssetProfilesAndFallbacks.md).

---

## Registration APIs

| API | Purpose |
|---|---|
| `RitsuLibFramework.CreateContentPack(modId)` | Recommended entry point — fluent builder |
| `RitsuLibFramework.GetContentRegistry(modId)` | Low-level content registry |
| `RitsuLibFramework.GetKeywordRegistry(modId)` | Keyword registry |
| `RitsuLibFramework.GetTimelineRegistry(modId)` | Timeline (story / epoch) registry |
| `RitsuLibFramework.GetUnlockRegistry(modId)` | Unlock rule registry |
| `ModCardPileRegistry.For(modId)` | Mod-owned card pile registry (qualified ids + `static_hover_tips` keys) |
| `ModTopBarButtonRegistry.For(modId)` | Mod-owned top-bar button registry (qualified ids + `static_hover_tips` keys) |

`CreateContentPack` wraps the **four** registries exposed on `ModContentPackContext` in a fluent builder that executes registered steps in insertion order when `Apply()` is called.

`ModCardPileRegistry` / `ModTopBarButtonRegistry` are separate per-mod singletons (not fields on `ModContentPackContext`). See [Content Packs & Registries](ContentPacksAndRegistries.md) for how they fit the overall map, and [Localization & Keywords](LocalizationAndKeywords.md) for hover-tip key conventions.

This document keeps the overview short. For builder surface, manifests, fixed-entry ownership, and freeze behavior, see [Content Packs & Registries](ContentPacksAndRegistries.md).

---

## Content Pack Builder

All builder methods are chainable. A representative example:

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>()
    .Card<MyCardPool, MyCard>()
    .Relic<MyRelicPool, MyRelic>()
    .CardKeywordOwnedByLocNamespace("my_keyword", iconPath: "res://MyMod/art/kw.png")
    .Story<MyStory>()
    .Epoch<MyEpoch>()
    .RequireEpoch<MyCard, MyEpoch>()
    .Custom(ctx => { /* ... */ })
    .Apply();
```

`Apply()` returns `ModContentPackContext` for further access to individual registries.

---

## Model ID Rule

For any model registered through the RitsuLib content registry, `ModelId.Entry` uses:

```
<MODID>_<CATEGORY>_<TYPENAME>
```

All segments are normalized to **UPPER_SNAKE_CASE**.

### Examples (Mod id `MyMod`)

| C# Type | Category | ModelId.Entry |
|---|---|---|
| `MyStrike` | card | `MY_MOD_CARD_MY_STRIKE` |
| `MyStarterRelic` | relic | `MY_MOD_RELIC_MY_STARTER_RELIC` |
| `MyCharacter` | character | `MY_MOD_CHARACTER_MY_CHARACTER` |

> If two types under the same mod id and category share the same CLR name, they resolve to the same entry and must be renamed.

---

## Localization Rule

Localization keys are written directly against the fixed `ModelId.Entry`:

```json
{
  "MY_MOD_CARD_MY_STRIKE.title": "My Strike",
  "MY_MOD_CARD_MY_STRIKE.description": "Deal {damage} damage.",
  "MY_MOD_RELIC_MY_STARTER_RELIC.title": "My Starter Relic"
}
```

`RitsuLibFramework.CreateModLocalization(...)` operates independently from the game's `LocString` pipeline.

---

## Asset Override Rule

RitsuLib applies template-based asset overrides via interface matching at render time.

### Card Overrides

Inherit `ModCardTemplate` and override via `AssetProfile` (recommended) or individual properties:

```csharp
public class MyCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
    // Unified profile (recommended)
    public override CardAssetProfile AssetProfile => new()
    {
        PortraitPath      = "res://MyMod/art/my_card.png",
        FramePath         = "res://MyMod/art/frame.png",
        FrameMaterialPath = "res://MyMod/art/frame.material",
    };

    // Or override a single property directly
    public override string? CustomPortraitPath => "res://MyMod/art/my_card.png";
}
```

Supported card fields include portrait, frame, portrait border, energy icon, overlay, and banner-related assets.

### Other Content

| Content type | Supported override fields |
|---|---|
| Relic | icon, icon outline, big icon |
| Power | icon, big icon |
| Orb | icon, visuals scene |
| Potion | image, outline |

Override behavior:
1. The model must implement the matching override interface (directly or via `Mod*Template`)
2. The override member must return a non-empty path
3. If the resource path does not exist, RitsuLib emits a one-time warning and falls back to the base asset

This warning behavior is especially important for character assets because the base game has almost no safe fallback for missing paths.

For the full profile records, helper factories, placeholder behavior, and diagnostics policy, see [Asset Profiles & Fallbacks](AssetProfilesAndFallbacks.md).

---

## Registration Timing

All content registration must be completed before the framework freezes content registration (during early game boot). Additional registration after the freeze is invalid and may throw.

The freeze is signaled by `ContentRegistrationClosedEvent`.

---

## Compatibility

The fixed-entry rule applies only to model types explicitly registered through the RitsuLib content registry, at `ModelDb.GetEntry(Type)`. Models not registered through RitsuLib are unaffected.

---

## Related Documents

- [Getting Started](GettingStarted.md)
- [Content Packs & Registries](ContentPacksAndRegistries.md)
- [Character & Unlock Templates](CharacterAndUnlockScaffolding.md)
- [Custom Events](CustomEvents.md)
- [Card Dynamic Variables](CardDynamicVarToolkit.md)
- [Localization & Keywords](LocalizationAndKeywords.md)
- [Framework Design](FrameworkDesign.md)
- [Asset Profiles & Fallbacks](AssetProfilesAndFallbacks.md)
