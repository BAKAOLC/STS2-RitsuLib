---
title:
  en: Asset Profiles And Fallbacks
  zh-CN: 资源配置与回退
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Use Profiles First{lang="en"}

::: en

When a template exposes `AssetProfile`, put paths there instead of patching UI nodes.

```csharp
public sealed class MyCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
    public override CardAssetProfile AssetProfile => new()
    {
        PortraitPath = "res://MyMod/images/cards/my_card.png",
        EnergyIconPath = "res://MyMod/images/ui/energy_orange.png",
        FrameMaterialPath = "res://MyMod/materials/card_frame_orange.tres",
    };
}
```

RitsuLib reads only non-empty fields. Missing fields keep the base game value or the template fallback.

For cards, texture fields cover portrait, beta portrait, frame, portrait border, energy icon, banner, overlay scene, and
ancient-only border/title banner/text background. Material fields are available for portrait, frame, portrait border,
energy icon, banner, ancient border, ancient title banner, and ancient text background; direct `Material` instance fields
can be used when a material should be created in code instead of loaded from a resource path. Set
`VisualStyle = CardVisualStyle.Ancient` to force the ancient card layout without changing gameplay rarity, or
`CardVisualStyle.Standard` to force the normal layout for an ancient-rarity card. `ContentAssetProfiles.AncientCard(...)`
sets `CardVisualStyle.Ancient` for you.

:::

## 优先使用 Profile{lang="zh-CN"}

::: zh-CN

模板暴露 `AssetProfile` 时，把路径写在 profile 里，不要直接 patch UI 节点。

```csharp
public sealed class MyCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
    public override CardAssetProfile AssetProfile => new()
    {
        PortraitPath = "res://MyMod/images/cards/my_card.png",
        EnergyIconPath = "res://MyMod/images/ui/energy_orange.png",
        FrameMaterialPath = "res://MyMod/materials/card_frame_orange.tres",
    };
}
```

RitsuLib 只读取非空字段。没有填写的字段会保留游戏原值或模板回退值。

卡牌 profile 的贴图字段覆盖 portrait、beta portrait、frame、portrait border、energy icon、banner、overlay scene，
以及 ancient 专用的 border/title banner/text background。材质字段覆盖 portrait、frame、portrait border、energy icon、
banner、ancient border、ancient title banner 和 ancient text background；如果材质需要在代码中创建，也可以使用直接的
`Material` 实例字段。设置 `VisualStyle = CardVisualStyle.Ancient` 可以在不改变玩法稀有度的情况下强制使用 ancient
卡面布局；设置 `CardVisualStyle.Standard` 可以让 ancient 稀有度卡强制使用普通布局。`ContentAssetProfiles.AncientCard(...)`
会自动设置 `CardVisualStyle.Ancient`。

:::

## Supported Profiles{lang="en"}

::: en

| Content | Profile |
| --- | --- |
| Card | `CardAssetProfile` |
| Relic | `RelicAssetProfile` |
| Power | `PowerAssetProfile` |
| Orb | `OrbAssetProfile` |
| Potion | `PotionAssetProfile` |
| Affliction | `AfflictionAssetProfile` |
| Enchantment | `EnchantmentAssetProfile` |
| Act | `ActAssetProfile` |
| Monster | `MonsterAssetProfile` |
| Encounter | `EncounterAssetProfile` |
| Event / ancient event layout | `EventAssetProfile` |
| Ancient map / run-history presentation | `AncientEventPresentationAssetProfile` |
| Rest site option | `RestSiteOptionAssetProfile` |
| Epoch portrait | `EpochAssetProfile` |
| Character | `CharacterAssetProfile` |

Use `ContentAssetProfiles.*(...)` only when you intentionally borrow a base-game path convention. For mod art, explicit `res://MyMod/...` paths are easier to review.

:::

## 支持的 Profile{lang="zh-CN"}

::: zh-CN

| 内容 | Profile |
| --- | --- |
| 卡牌 | `CardAssetProfile` |
| 遗物 | `RelicAssetProfile` |
| 能力 | `PowerAssetProfile` |
| Orb | `OrbAssetProfile` |
| 药水 | `PotionAssetProfile` |
| Affliction | `AfflictionAssetProfile` |
| Enchantment | `EnchantmentAssetProfile` |
| Act | `ActAssetProfile` |
| Monster | `MonsterAssetProfile` |
| Encounter | `EncounterAssetProfile` |
| 事件 / Ancient 事件布局 | `EventAssetProfile` |
| Ancient 地图 / 历史记录表现 | `AncientEventPresentationAssetProfile` |
| Rest site 选项 | `RestSiteOptionAssetProfile` |
| Epoch 头像 | `EpochAssetProfile` |
| 角色 | `CharacterAssetProfile` |

只有在明确要借用原版路径约定时才使用 `ContentAssetProfiles.*(...)`。Mod 自己的美术资源直接写 `res://MyMod/...` 更容易检查。

:::

## Character Profiles{lang="en"}

::: en

Character assets are grouped by purpose:

```csharp
public override CharacterAssetProfile AssetProfile => new()
{
    Scenes = new()
    {
        VisualsPath = "res://MyMod/scenes/characters/my_character_visuals.tscn",
        EnergyCounterPath = "res://MyMod/scenes/ui/my_energy_counter.tscn",
    },
    Ui = new()
    {
        CharacterSelectIconPath = "res://MyMod/images/character/select_icon.png",
        MapMarkerPath = "res://MyMod/images/character/map_marker.png",
    },
    Audio = new()
    {
        AttackSfx = "event:/MyMod/character_attack",
    },
};
```

`PlaceholderCharacterId` lets a partial profile borrow missing fields from a vanilla character. Use it as a temporary scaffold, then replace important visible assets before release.

:::

## 角色 Profile{lang="zh-CN"}

::: zh-CN

角色资源按用途分组：

```csharp
public override CharacterAssetProfile AssetProfile => new()
{
    Scenes = new()
    {
        VisualsPath = "res://MyMod/scenes/characters/my_character_visuals.tscn",
        EnergyCounterPath = "res://MyMod/scenes/ui/my_energy_counter.tscn",
    },
    Ui = new()
    {
        CharacterSelectIconPath = "res://MyMod/images/character/select_icon.png",
        MapMarkerPath = "res://MyMod/images/character/map_marker.png",
    },
    Audio = new()
    {
        AttackSfx = "event:/MyMod/character_attack",
    },
};
```

`PlaceholderCharacterId` 可让不完整 profile 从原版角色借缺失字段。它适合开发期搭建，发布前应替换重要可见资源。

:::

## Missing Paths{lang="en"}

::: en

Check resource paths before release. If a path is missing, RitsuLib logs a warning and keeps a fallback where one exists. Character visuals have fewer safe base-game fallbacks than cards or relics, so treat character path warnings as release blockers.

:::

## 缺失路径{lang="zh-CN"}

::: zh-CN

发布前检查资源路径。路径缺失时，RitsuLib 会记录警告，并在存在回退值时继续使用回退。角色视觉资源不像卡牌或遗物那样总有安全原版回退，因此角色路径警告应视为发布阻断问题。

:::
