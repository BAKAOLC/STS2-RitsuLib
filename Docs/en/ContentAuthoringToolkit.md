# Content Authoring Toolkit

This document defines the content registration APIs, model ID derivation rules, asset override behavior, and compatibility contracts.

---

## Registration APIs

| API | Purpose |
|---|---|
| `RitsuLibFramework.CreateContentPack(modId)` | Recommended entry point — fluent builder |
| `RitsuLibFramework.GetContentRegistry(modId)` | Low-level content registry |
| `RitsuLibFramework.GetKeywordRegistry(modId)` | Keyword registry |
| `RitsuLibFramework.GetTimelineRegistry(modId)` | Timeline (story / epoch) registry |
| `RitsuLibFramework.GetUnlockRegistry(modId)` | Unlock rule registry |

`CreateContentPack` wraps all of the above in a fluent builder that executes registered steps in insertion order when `Apply()` is called.

---

## Content Pack Builder

All builder methods are chainable:

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    // Content models
    .Character<MyCharacter>()
    .Card<MyCardPool, MyCard>()
    .Relic<MyRelicPool, MyRelic>()
    .Potion<MyPotionPool, MyPotion>()
    .Power<MyPower>()
    .Orb<MyOrb>()
    .SharedEvent<MyEvent>()
    .ActEvent<MyAct, MyActEvent>()
    .SharedAncient<MyAncient>()
    .ActAncient<MyAct, MyActAncient>()
    .Act<MyAct>()

    // Keywords
    .CardKeyword("my_keyword", locKeyPrefix: "my_mod_my_keyword", iconPath: "res://MyMod/art/kw.png")
    .Keyword("my_keyword", titleTable: "card_keywords")

    // Timeline
    .Story<MyStory>()
    .Epoch<MyEpoch>()

    // Unlock conditions
    .RequireEpoch<MyCard, MyEpoch>()
    .UnlockEpochAfterRunAs<MyCharacter, MyEpoch>()
    .UnlockEpochAfterWinAs<MyCharacter, MyEpoch>()
    .UnlockEpochAfterAscensionWin<MyCharacter, MyEpoch>(ascensionLevel: 10)
    .UnlockEpochAfterRunCount<MyEpoch>(requiredRuns: 5, requireVictory: false)

    // Arbitrary registration step
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

### Examples (Mod id `STS2-WineFox`)

| C# Type | Category | ModelId.Entry |
|---|---|---|
| `WineFoxStrike` | card | `STS2_WINE_FOX_CARD_WINE_FOX_STRIKE` |
| `HandCrank` | relic | `STS2_WINE_FOX_RELIC_HAND_CRANK` |
| `WineFox` | character | `STS2_WINE_FOX_CHARACTER_WINE_FOX` |

> If two types under the same mod id and category share the same CLR name, they resolve to the same entry and must be renamed.

---

## Localization Rule

Localization keys are written directly against the fixed `ModelId.Entry`:

```json
{
  "STS2_WINE_FOX_CARD_WINE_FOX_STRIKE.title": "WineFox Strike",
  "STS2_WINE_FOX_CARD_WINE_FOX_STRIKE.description": "Deal {damage} damage.",
  "STS2_WINE_FOX_RELIC_HAND_CRANK.title": "Hand Crank"
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

Supported card fields: `PortraitPath`, `BetaPortraitPath`, `FramePath`, `PortraitBorderPath`, `EnergyIconPath`, `FrameMaterialPath`, `OverlayScenePath`, `BannerTexturePath`, `BannerMaterialPath`

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

This warning behavior is especially important for character assets because the base game has almost no safe fallback for missing paths. Character-specific placeholder fallback is documented in [CharacterAndUnlockScaffolding.md](CharacterAndUnlockScaffolding.md).

---

## Registration Timing

All content registration must be completed before the framework freezes content registration (during early game boot). Additional registration after the freeze is invalid and may throw.

The freeze is signaled by `ContentRegistrationClosedEvent`.

---

## Compatibility

The fixed-entry rule applies only to model types explicitly registered through the RitsuLib content registry, at `ModelDb.GetEntry(Type)`. Models not registered through RitsuLib are unaffected.

---

## Related Documents

- [GettingStarted.md](GettingStarted.md)
- [CharacterAndUnlockScaffolding.md](CharacterAndUnlockScaffolding.md)
- [CardDynamicVarToolkit.md](CardDynamicVarToolkit.md)
- [LocalizationAndKeywords.md](LocalizationAndKeywords.md)
- [FrameworkDesign.md](FrameworkDesign.md)
