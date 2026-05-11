---
title:
  en: Card Dynamic Variables
  zh-CN: 卡牌动态变量
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Add A Variable{lang="en"}

::: en

Use `ModCardVars` when a card needs values in its description that can change at runtime.

```csharp
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;

public sealed class MyStrike : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
    public override DynamicVarSet DynamicVars => new()
    {
        ModCardVars.Int("damage", Damage),
        ModCardVars.Computed("block", 3, card => card?.Upgraded == true ? 5 : 3),
    };
}
```

Then use those variable names in `cards.json`, for example `Deal {damage} damage. Gain {block} block.`

Use a normal `IntVar` for values that already live on the card. Use `ComputedDynamicVar` when the display value depends on target, upgrade state, or preview mode.

:::

## 添加变量{lang="zh-CN"}

::: zh-CN

当卡牌描述中需要显示运行时变化的数值时，使用 `ModCardVars`。

```csharp
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Cards.DynamicVars;

public sealed class MyStrike : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
    public override DynamicVarSet DynamicVars => new()
    {
        ModCardVars.Int("damage", Damage),
        ModCardVars.Computed("block", 3, card => card?.Upgraded == true ? 5 : 3),
    };
}
```

然后在 `cards.json` 中使用这些变量名，例如 `造成 {damage} 点伤害。获得 {block} 点格挡。`

值已经存在于卡牌上时，用普通 `IntVar`。显示值依赖目标、升级状态或预览模式时，用 `ComputedDynamicVar`。

:::

## Add A Tooltip{lang="en"}

::: en

Attach hover tips directly to a dynamic variable.

```csharp
ModCardVars.Int("heat", Heat)
    .WithSharedTooltip("MY_MOD_HEAT", "res://MyMod/images/ui/heat.png");
```

This reads `static_hover_tips` keys:

```json
{
  "MY_MOD_HEAT.title": "Heat",
  "MY_MOD_HEAT.description": "Some cards care about the current Heat value."
}
```

For custom layouts, pass a factory to `.WithTooltip(var => new HoverTip(...))`.

:::

## 添加 Tooltip{lang="zh-CN"}

::: zh-CN

可以直接给动态变量挂 hover tip。

```csharp
ModCardVars.Int("heat", Heat)
    .WithSharedTooltip("MY_MOD_HEAT", "res://MyMod/images/ui/heat.png");
```

它读取 `static_hover_tips` 中的 key：

```json
{
  "MY_MOD_HEAT.title": "热量",
  "MY_MOD_HEAT.description": "部分卡牌会根据当前热量改变效果。"
}
```

需要自定义布局时，传入 `.WithTooltip(var => new HoverTip(...))` 工厂。

:::

## Read Values Safely{lang="en"}

::: en

Use extension helpers when a card or effect reads dynamic variables from another card:

```csharp
var amount = card.DynamicVars.GetIntOrDefault("damage");
var hasHeat = card.DynamicVars.HasPositiveValue("heat");
```

The helpers return defaults when the key is missing, which is usually better than assuming every card has your variable.

:::

## 安全读取{lang="zh-CN"}

::: zh-CN

当卡牌或效果需要读取另一张牌的动态变量时，使用扩展方法：

```csharp
var amount = card.DynamicVars.GetIntOrDefault("damage");
var hasHeat = card.DynamicVars.HasPositiveValue("heat");
```

变量不存在时这些方法会返回默认值，通常比假定每张牌都有你的变量更稳。

:::

## Preview Logic{lang="en"}

::: en

`ComputedDynamicVar` accepts a preview factory:

```csharp
ModCardVars.Computed(
    "damage",
    Damage,
    (card, target) => ResolveDamage(card, target),
    (card, mode, target, runGlobalHooks) => ResolvePreviewDamage(card, mode, target));
```

Use preview logic when card preview, target preview, or upgrade preview should show a value different from the current live card value.

:::

## 预览逻辑{lang="zh-CN"}

::: zh-CN

`ComputedDynamicVar` 可以传入 preview factory：

```csharp
ModCardVars.Computed(
    "damage",
    Damage,
    (card, target) => ResolveDamage(card, target),
    (card, mode, target, runGlobalHooks) => ResolvePreviewDamage(card, mode, target));
```

当卡牌预览、目标预览或升级预览需要显示不同于当前实卡的值时，再写 preview 逻辑。

:::
