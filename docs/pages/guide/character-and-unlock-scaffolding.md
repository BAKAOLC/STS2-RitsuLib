---
title:
  en: Character And Unlock Scaffolding
  zh-CN: 角色与解锁脚手架
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Character Template{lang="en"}

::: en

Use `ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool>` when your character owns its own card, relic, and potion pools.

```csharp
public sealed class MyCharacter
    : ModCharacterTemplate<MyCardPool, MyRelicPool, MyPotionPool>
{
    public override CharacterAssetProfile AssetProfile => new()
    {
        Ui = new() { CharacterSelectIconPath = "res://MyMod/images/character/icon.png" }
    };

    public override bool RequiresEpochAndTimeline => true;
}
```

Write the character name and pronouns in `characters.json`. Register the character and its starter content with
attributes:

```csharp
[RegisterCharacter]
public sealed class MyCharacter
    : ModCharacterTemplate<MyCardPool, MyRelicPool, MyPotionPool>
{
}

[RegisterCard(typeof(MyCardPool))]
[RegisterCharacterStarterCard(typeof(MyCharacter), 4, Order = 10)]
public sealed class MyStrike : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
}

[RegisterRelic(typeof(MyRelicPool))]
[RegisterCharacterStarterRelic(typeof(MyCharacter), Order = 0)]
public sealed class MyStarterRelic : ModRelicTemplate
{
}
```

Use a content pack when the starter list is easier to review in one place:

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>(c => c
        .AddStartingRelic<MyStarterRelic>(1, order: 0)
        .AddStartingCard<MyStrike>(4, order: 10)
        .AddStartingCard<MyDefend>(4, order: 20))
    .Card<MyCardPool, MyStrike>()
    .Card<MyCardPool, MyDefend>()
    .Relic<MyRelicPool, MyStarterRelic>()
    .Apply();
```

Prefer additive starter registration over overriding legacy `StartingDeckTypes`, `StartingRelicTypes`, or `StartingPotionTypes`.

`Order` controls starter content ordering inside each starter list. Lower values appear earlier; entries with the same
order keep registration order. `count` adds multiple copies at the same ordered position, which is what you want for
starter cards such as four strikes or four defends. Use the same rule for starter relics and starter potions.

:::

## 角色模板{lang="zh-CN"}

::: zh-CN

当角色拥有自己的卡池、遗物池和药水池时，使用 `ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool>`。

```csharp
public sealed class MyCharacter
    : ModCharacterTemplate<MyCardPool, MyRelicPool, MyPotionPool>
{
    public override CharacterAssetProfile AssetProfile => new()
    {
        Ui = new() { CharacterSelectIconPath = "res://MyMod/images/character/icon.png" }
    };

    public override bool RequiresEpochAndTimeline => true;
}
```

角色名称和代词写在 `characters.json`。角色和初始内容可以用注解注册：

```csharp
[RegisterCharacter]
public sealed class MyCharacter
    : ModCharacterTemplate<MyCardPool, MyRelicPool, MyPotionPool>
{
}

[RegisterCard(typeof(MyCardPool))]
[RegisterCharacterStarterCard(typeof(MyCharacter), 4, Order = 10)]
public sealed class MyStrike : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
}

[RegisterRelic(typeof(MyRelicPool))]
[RegisterCharacterStarterRelic(typeof(MyCharacter), Order = 0)]
public sealed class MyStarterRelic : ModRelicTemplate
{
}
```

如果初始内容列表集中写更容易审查，也可以使用 content pack：

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>(c => c
        .AddStartingRelic<MyStarterRelic>(1, order: 0)
        .AddStartingCard<MyStrike>(4, order: 10)
        .AddStartingCard<MyDefend>(4, order: 20))
    .Card<MyCardPool, MyStrike>()
    .Card<MyCardPool, MyDefend>()
    .Relic<MyRelicPool, MyStarterRelic>()
    .Apply();
```

优先使用 additive starter registration，不要在新项目里覆写旧的 `StartingDeckTypes`、`StartingRelicTypes` 或 `StartingPotionTypes`。

`Order` 控制每类初始内容列表中的排序。值越小越靠前；相同 order 按注册插入顺序排列。`count` 会在同一个排序位置添加多份副本，适合四张打击、四张防御这类初始牌。初始遗物和初始药水也使用同一规则。

:::

## Visibility And Vanilla Progression{lang="en"}

::: en

Override these properties when the character should not behave like a built-in character:

| Property | Use |
| --- | --- |
| `RequiresEpochAndTimeline` | Set `false` for characters that do not participate in vanilla epoch / timeline assumptions. |
| `HideFromVanillaCharacterSelect` | Hide from the vanilla character-select list. |
| `AllowInVanillaRandomCharacterSelect` | Control random character selection. |
| `HideInCardLibraryCompendium` | Hide the character card-pool filter in the compendium. |
| `CardLibraryCompendiumPlacementRules` | Place the character pool near another pool or in a custom order. |

If a character is meant to be playable from the normal UI, keep `RequiresEpochAndTimeline` true and register proper story / epoch entries.

:::

## 可见性与原版进度{lang="zh-CN"}

::: zh-CN

当角色不应完全按原版角色处理时，覆写这些属性：

| 属性 | 用途 |
| --- | --- |
| `RequiresEpochAndTimeline` | 不参与原版 epoch / timeline 假设的角色设为 `false`。 |
| `HideFromVanillaCharacterSelect` | 从原版角色选择列表隐藏。 |
| `AllowInVanillaRandomCharacterSelect` | 控制是否能被随机角色选中。 |
| `HideInCardLibraryCompendium` | 隐藏图鉴里的角色卡池筛选。 |
| `CardLibraryCompendiumPlacementRules` | 控制角色卡池筛选的排序或相邻位置。 |

如果角色应从普通 UI 游玩，保留 `RequiresEpochAndTimeline = true`，并注册正确的 story / epoch。

:::

## Unlock Rules{lang="en"}

::: en

Create epoch models for content gates, then register rules through the content pack.

```csharp
public sealed class MyFirstEpoch : ModEpochTemplate
{
    public override string Id => "MY_MOD_FIRST_EPOCH";
}

RitsuLibFramework.CreateContentPack("MyMod")
    .Epoch<MyFirstEpoch>()
    .RequireEpoch<MyRareCard, MyFirstEpoch>()
    .UnlockEpochAfterWinAs<MyCharacter, MyFirstEpoch>()
    .Apply();
```

Common rule helpers:

| Rule | Meaning |
| --- | --- |
| `.RequireEpoch<TModel, TEpoch>()` | Hide a model until the epoch is obtained. |
| `.UnlockEpochAfterRunAs<TCharacter, TEpoch>()` | Grant after any run with that character. |
| `.UnlockEpochAfterWinAs<TCharacter, TEpoch>()` | Grant after a win. |
| `.UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(level)` | Grant after an ascension win at or above the level. |
| `.UnlockEpochAfterRunCount<TEpoch>(count, requireVictory)` | Grant after global run count. |
| `.UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(count)` | Grant after counted elite wins. |
| `.UnlockEpochAfterBossVictories<TCharacter, TEpoch>(count)` | Grant after counted boss wins. |

:::

## 解锁规则{lang="zh-CN"}

::: zh-CN

用 epoch 模型表示内容门槛，然后通过 content pack 注册规则。

```csharp
public sealed class MyFirstEpoch : ModEpochTemplate
{
    public override string Id => "MY_MOD_FIRST_EPOCH";
}

RitsuLibFramework.CreateContentPack("MyMod")
    .Epoch<MyFirstEpoch>()
    .RequireEpoch<MyRareCard, MyFirstEpoch>()
    .UnlockEpochAfterWinAs<MyCharacter, MyFirstEpoch>()
    .Apply();
```

常用规则：

| 规则 | 含义 |
| --- | --- |
| `.RequireEpoch<TModel, TEpoch>()` | 获得 epoch 前隐藏模型。 |
| `.UnlockEpochAfterRunAs<TCharacter, TEpoch>()` | 使用该角色完成任意 run 后授予。 |
| `.UnlockEpochAfterWinAs<TCharacter, TEpoch>()` | 获胜后授予。 |
| `.UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(level)` | 达到指定进阶等级并获胜后授予。 |
| `.UnlockEpochAfterRunCount<TEpoch>(count, requireVictory)` | 按全局 run 次数授予。 |
| `.UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(count)` | 按精英胜利计数授予。 |
| `.UnlockEpochAfterBossVictories<TCharacter, TEpoch>(count)` | 按 Boss 胜利计数授予。 |

:::

## Related Pages{lang="en"}

::: en

- [Timeline and unlocks](/guide/timeline-and-unlocks)
- [Asset profiles and fallbacks](/guide/asset-profiles-and-fallbacks)
- [Creature visuals and animation](/guide/creature-visuals-and-animation)

:::

## 相关页面{lang="zh-CN"}

::: zh-CN

- [时间线与解锁](/guide/timeline-and-unlocks)
- [资源配置与回退](/guide/asset-profiles-and-fallbacks)
- [生物视觉与动画](/guide/creature-visuals-and-animation)

:::
