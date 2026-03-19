# Character & Unlock Scaffolding

This document covers character templates, content pool definitions, epoch templates, and unlock rule registration with full examples.

---

## Overview

A full character mod typically includes:

| Content | Base Type | Example |
|---|---|---|
| Card pool | `TypeListCardPoolModel` | `WineFoxCardPool` |
| Relic pool | `TypeListRelicPoolModel` | `WineFoxRelicPool` |
| Potion pool | `TypeListPotionPoolModel` | `WineFoxPotionPool` |
| Character | `ModCharacterTemplate<TCard, TRelic, TPotion>` | `WineFoxCharacter` |
| Story | `ModStoryTemplate` | `WineFoxStory` |
| Epoch | `CharacterUnlockEpochTemplate<T>` or custom | `WineFoxEpoch2` |

---

## Pools

Use `TypeList*PoolModel` to declare pool contents by type — no manual `ModelId` handling required:

```csharp
public class WineFoxCardPool : TypeListCardPoolModel
{
    protected override IEnumerable<Type> CardTypes =>
    [
        typeof(WineFoxStrike),
        typeof(WineFoxDefend),
        typeof(WineFoxSignatureCard),
    ];
}

public class WineFoxRelicPool : TypeListRelicPoolModel
{
    protected override IEnumerable<Type> RelicTypes =>
    [
        typeof(WineFoxStarterRelic),
    ];
}

public class WineFoxPotionPool : TypeListPotionPoolModel
{
    // Leave empty if the character has no exclusive potions
    protected override IEnumerable<Type> PotionTypes => [];
}
```

### Configure Card Frame Color (HSV)

`TypeListCardPoolModel` supports directly overriding `PoolFrameMaterial`. When this property returns a non-null material, that material is used for card frame rendering and `CardFrameMaterialPath` is no longer required.

```csharp
using Godot;
using STS2RitsuLib.Utils;

public class WineFoxCardPool : TypeListCardPoolModel
{
    protected override IEnumerable<Type> CardTypes =>
    [
        typeof(WineFoxStrike),
        typeof(WineFoxDefend),
    ];

    // Generate a frame material from HSV: H=0.55, S=0.45, V=0.95
    public override Material? PoolFrameMaterial =>
        MaterialUtils.CreateHsvShaderMaterial(0.55f, 0.45f, 0.95f);
}
```

If you prefer path-based configuration, simply leave `PoolFrameMaterial` as `null` and override `CardFrameMaterialPath` instead.

---

## Character Template

Inherit `ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool>` and provide the starting deck, relics, and asset paths:

```csharp
public class WineFoxCharacter : ModCharacterTemplate<WineFoxCardPool, WineFoxRelicPool, WineFoxPotionPool>
{
    protected override IEnumerable<Type> StartingDeckTypes =>
    [
        typeof(WineFoxStrike), typeof(WineFoxStrike), typeof(WineFoxStrike),
        typeof(WineFoxDefend), typeof(WineFoxDefend),
    ];

    protected override IEnumerable<Type> StartingRelicTypes =>
    [
        typeof(WineFoxStarterRelic),
    ];

    public override CharacterAssetProfile AssetProfile => new()
    {
        CombatSpineSkeletonDataPath = "res://WineFox/spine/wine_fox.tres",
        IconTexturePath             = "res://WineFox/art/icon.png",
        CharacterSelectBgPath       = "res://WineFox/art/select_bg.png",
    };
}
```

---

## Story Template

Inherit `ModStoryTemplate` to bind a narrative campaign to the character:

```csharp
public class WineFoxStory : ModStoryTemplate
{
    public override Type CharacterType => typeof(WineFoxCharacter);
}
```

---

## Epoch Templates

RitsuLib provides pre-built epoch templates for common unlock targets:

| Template | Purpose |
|---|---|
| `CharacterUnlockEpochTemplate<TCharacter>` | Epoch that unlocks the character itself |
| `CardUnlockEpochTemplate` | Epoch that unlocks additional cards |
| `RelicUnlockEpochTemplate` | Epoch that unlocks additional relics |
| `PotionUnlockEpochTemplate` | Epoch that unlocks additional potions |

```csharp
public class WineFoxEpoch2 : CardUnlockEpochTemplate
{
    protected override IEnumerable<Type> UnlockedCardTypes =>
    [
        typeof(WineFoxAdvancedCard),
    ];
}
```

---

## Full Registration Example

```csharp
RitsuLibFramework.CreateContentPack("STS2-WineFox")
    // Cards (specify the owning pool)
    .Card<WineFoxCardPool, WineFoxStrike>()
    .Card<WineFoxCardPool, WineFoxDefend>()
    .Card<WineFoxCardPool, WineFoxSignatureCard>()
    .Card<WineFoxCardPool, WineFoxAdvancedCard>()

    // Relics
    .Relic<WineFoxRelicPool, WineFoxStarterRelic>()

    // Character
    .Character<WineFoxCharacter>()

    // Story and epoch
    .Story<WineFoxStory>()
    .Epoch<WineFoxEpoch2>()

    // Unlock rules
    .RequireEpoch<WineFoxAdvancedCard, WineFoxEpoch2>()       // hide card until epoch 2
    .UnlockEpochAfterRunAs<WineFoxCharacter, WineFoxEpoch2>() // unlock epoch 2 after one run

    .Apply();
```

---

## Model ID and Localization

Character models follow the same fixed `ModelId.Entry` rule as all other content (see [ContentAuthoringToolkit.md](ContentAuthoringToolkit.md)).

Example — mod id `STS2-WineFox`, type `WineFoxCharacter`:
- `ModelId.Entry` → `STS2_WINE_FOX_CHARACTER_WINE_FOX`
- Localization key → `STS2_WINE_FOX_CHARACTER_WINE_FOX.title`

> Renaming a CLR type changes its derived entry. Avoid renaming types after they have been published.

---

## Dependency Rules

- All card / relic / potion types referenced by a pool must be registered before runtime model lookup occurs.
- A character's referenced pool types must all be registered.
- Every model — including epoch-gated content — must still be registered. Unlock rules do not replace registration.

---

## Related Documents

- [ContentAuthoringToolkit.md](ContentAuthoringToolkit.md)
- [GettingStarted.md](GettingStarted.md)
