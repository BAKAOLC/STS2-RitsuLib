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

`ModCardVars` also provides typed shortcuts for common vanilla variables: `Bool`, `Cards`, `Damage`, `OstyDamage`, `Block`, `Gold`, `Heal`, `HpLoss`, `MaxHp`, `Repeat`, `Forge`, `Summon`, `Energy`, `Stars`, and `Power<T>`. Each variable with a vanilla default name also has a named overload.

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

`ModCardVars` 还为常见原版变量提供了强类型快捷方法：`Bool`、`Cards`、`Damage`、`OstyDamage`、`Block`、`Gold`、`Heal`、`HpLoss`、`MaxHp`、`Repeat`、`Forge`、`Summon`、`Energy`、`Stars` 和 `Power<T>`。拥有原版默认名称的变量也都有具名重载。

:::

## Context Factories{lang="en"}

::: en

Use the context overload when one value depends on several pieces of card, run, combat, preview, or target state:

```csharp
public override DynamicVarSet DynamicVars => new()
{
    ModCardVars.Int("Heat", Heat),
    ModCardVars.ComputedDamage(
        "Damage",
        static ctx =>
        {
            var heat = ctx.GetCardIntOrDefault("Heat");
            var targetBonus = ctx.HasTarget && ctx.IsInCombat
                ? ResolveTargetBonus(ctx.CombatState, ctx.Target)
                : 0m;

            return ctx.BaseValue + heat + targetBonus + (ctx.IsUpgraded ? 3m : 0m);
        },
        baseValue: 6),
};
```

The same `ComputedDynamicVarContext` is used for live values and previews. Its convenience members avoid repeatedly rebuilding guarded state checks:

- Evaluation: `IsCurrentValue`, `IsPreview`, `IsNormalPreview`, `IsUpgradePreview`, `IsMultiTargetPreview`, `ShouldRunGlobalHooks`.
- Card: `Card`, `ModelOwner`, `IsMutableCard`, `IsCanonicalCard`, `IsUpgraded`, `IsEnchantmentPreview`.
- Ownership and scope: `Player`, `SourceCreature`, `RunState`, `CombatState`, `CardScope`, plus matching `Has...` properties.
- Location: `IsInRun`, `IsInCombat`, and `IsCardInCombat`. `IsInCombat` falls back to the owner's active combat; `IsCardInCombat` is only true when the card itself reports combat scope.
- Variables: `TryGetCardVar<T>`, `GetRequiredCardVar<T>`, `GetCardBaseValueOrDefault`, `GetCardIntOrDefault`, and `EvaluateCardVarOrDefault`.

Context overloads are available directly on `Computed`, `ComputedEnergy`, `ComputedStars`, `ComputedPower<T>`, `ComputedPowerAmountGiven<T>`, `ComputedDamage`, `ComputedOstyDamage`, and `ComputedBlock`. Their factory is the second argument so existing `Computed(name, baseValue, card => ...)` calls remain unambiguous.

Prefer `static` factories. Dynamic vars are cloned with their cards; a static factory cannot accidentally retain the card instance from which the variable was created.

:::

## 上下文工厂{lang="zh-CN"}

::: zh-CN

当一个数值同时依赖卡牌、跑局、战斗、预览或目标等多种状态时，使用上下文重载：

```csharp
public override DynamicVarSet DynamicVars => new()
{
    ModCardVars.Int("Heat", Heat),
    ModCardVars.ComputedDamage(
        "Damage",
        static ctx =>
        {
            var heat = ctx.GetCardIntOrDefault("Heat");
            var targetBonus = ctx.HasTarget && ctx.IsInCombat
                ? ResolveTargetBonus(ctx.CombatState, ctx.Target)
                : 0m;

            return ctx.BaseValue + heat + targetBonus + (ctx.IsUpgraded ? 3m : 0m);
        },
        baseValue: 6),
};
```

实时值和预览共用同一个 `ComputedDynamicVarContext`。它提供的快捷成员可以避免用户反复拼装带空值保护的复杂判断：

- 求值状态：`IsCurrentValue`、`IsPreview`、`IsNormalPreview`、`IsUpgradePreview`、`IsMultiTargetPreview`、`ShouldRunGlobalHooks`。
- 卡牌状态：`Card`、`ModelOwner`、`IsMutableCard`、`IsCanonicalCard`、`IsUpgraded`、`IsEnchantmentPreview`。
- 拥有者和作用域：`Player`、`SourceCreature`、`RunState`、`CombatState`、`CardScope`，以及对应的 `Has...` 属性。
- 所处位置：`IsInRun`、`IsInCombat` 和 `IsCardInCombat`。`IsInCombat` 会回退到拥有者当前参与的战斗；只有卡牌自身报告战斗作用域时，`IsCardInCombat` 才为 true。
- 变量读取：`TryGetCardVar<T>`、`GetRequiredCardVar<T>`、`GetCardBaseValueOrDefault`、`GetCardIntOrDefault` 和 `EvaluateCardVarOrDefault`。

`Computed`、`ComputedEnergy`、`ComputedStars`、`ComputedPower<T>`、`ComputedPowerAmountGiven<T>`、`ComputedDamage`、`ComputedOstyDamage` 和 `ComputedBlock` 都直接提供上下文重载。上下文 factory 位于第二个参数，因此不会让已有的 `Computed(name, baseValue, card => ...)` 调用产生歧义。

建议使用 `static` factory。动态变量会随卡牌克隆；静态 factory 不会意外保留创建变量时的卡牌实例。

:::

## Damage And Block Wrappers{lang="en"}

::: en

Use `ComputedDamage` and `ComputedBlock` for computed values that should still match normal card preview rules for Strength, Dexterity, Vulnerable, Frail, and card enchantments:

```csharp
public override DynamicVarSet DynamicVars => new()
{
    ModCardVars.ComputedDamage("damage", 6, (card, target) => BaseDamage + BonusDamage(card, target)),
    ModCardVars.ComputedBlock("block", 5, card => card?.IsUpgraded == true ? 8 : 5),
};
```

For Osty attacks, use `ComputedOstyDamage` so damage preview modifiers see Osty as the dealer:

```csharp
ModCardVars.ComputedOstyDamage("damage", 7, (card, target) => ResolveOstyDamage(card, target));
```

When the preview base amount differs from the live amount, pass a preview base factory. The result still goes through normal damage or block hooks:

```csharp
ModCardVars.ComputedDamage(
    "damage",
    6,
    (card, target) => ResolveDamage(card, target),
    (card, mode, target, runGlobalHooks) => ResolvePreviewDamageBase(card, mode, target));
```

Use plain `Computed` when the value should not pass through damage or block hooks.

The context overload keeps the same behavior without requiring a separate preview delegate:

```csharp
ModCardVars.ComputedDamage(
    "damage",
    static ctx => ctx.BaseValue + ResolveBonus(ctx.Player, ctx.Target),
    baseValue: 6);
```

:::

## 伤害与格挡包装{lang="zh-CN"}

::: zh-CN

当计算值仍需要符合普通卡牌预览规则时，使用 `ComputedDamage` 和 `ComputedBlock`，这样力量、敏捷、易伤、脆弱和卡牌附魔都会参与预览计算：

```csharp
public override DynamicVarSet DynamicVars => new()
{
    ModCardVars.ComputedDamage("damage", 6, (card, target) => BaseDamage + BonusDamage(card, target)),
    ModCardVars.ComputedBlock("block", 5, card => card?.IsUpgraded == true ? 8 : 5),
};
```

奥斯蒂攻击使用 `ComputedOstyDamage`，这样伤害预览修正会把奥斯蒂视为伤害来源：

```csharp
ModCardVars.ComputedOstyDamage("damage", 7, (card, target) => ResolveOstyDamage(card, target));
```

当预览基础值和实卡当前值不同时，传入 preview base factory。它返回的结果仍会继续经过普通伤害或格挡 hook：

```csharp
ModCardVars.ComputedDamage(
    "damage",
    6,
    (card, target) => ResolveDamage(card, target),
    (card, mode, target, runGlobalHooks) => ResolvePreviewDamageBase(card, mode, target));
```

如果数值不应该经过伤害或格挡 hook，继续使用普通 `Computed`。

上下文重载不需要额外的 preview delegate，也会保留相同的预览修正规则：

```csharp
ModCardVars.ComputedDamage(
    "damage",
    static ctx => ctx.BaseValue + ResolveBonus(ctx.Player, ctx.Target),
    baseValue: 6);
```

:::

## Energy, Star, And Power Icon Counts{lang="en"}

::: en

Use the concrete icon variable helpers when a description needs a dynamic amount rendered through the game's icon formatters:

```csharp
public override DynamicVarSet DynamicVars => new()
{
    ModCardVars.ComputedEnergy("EnergyGain", 1, card => ResolveEnergyGain(card)),
    ModCardVars.ComputedStars("StarGain", 1, card => ResolveStarGain(card)),
    ModCardVars.ComputedPower<StrengthPower>("StrengthPower", 2, card => ResolveStrength(card)),
};
```

`ComputedPower<T>` keeps the typed `PowerVar<T>` shape for naming and display, and does not run power-amount hooks by default. For a computed amount that represents power being applied by the card and should use the same preview hook path as vanilla `PowerVar<T>`, use `ComputedPowerAmountGiven<T>`.

Then keep the formatter in localization:

```json
{
  "MY_CARD.description": "Gain {EnergyGain:energyIcons()}.\nGain {StarGain:starIcons()}.\nGain {StrengthPower:diff()} Strength."
}
```

For a literal single energy icon that follows the card pool color, use the vanilla extra argument:

```json
{
  "MY_CARD.description": "The next card costs 0{energyPrefix:energyIcons(1)}."
}
```

:::

## 能量、星星与能力层数图标数量{lang="zh-CN"}

::: zh-CN

当描述中需要通过游戏原生图标 formatter 显示动态数量时，使用具体图标变量辅助方法：

```csharp
public override DynamicVarSet DynamicVars => new()
{
    ModCardVars.ComputedEnergy("EnergyGain", 1, card => ResolveEnergyGain(card)),
    ModCardVars.ComputedStars("StarGain", 1, card => ResolveStarGain(card)),
    ModCardVars.ComputedPower<StrengthPower>("StrengthPower", 2, card => ResolveStrength(card)),
};
```

`ComputedPower<T>` 保留类型化的 `PowerVar<T>` 形状，用于命名和显示，默认不跑能力层数 hook。如果这个计算值表示卡牌将要施加的能力层数，并且需要走和原版 `PowerVar<T>` 相同的预览修正路径，使用 `ComputedPowerAmountGiven<T>`。

本地化里继续保留 formatter：

```json
{
  "MY_CARD.description": "获得{EnergyGain:energyIcons()}。\n获得{StarGain:starIcons()}。\n获得{StrengthPower:diff()}点力量。"
}
```

如果只是需要一个跟随卡池颜色的固定单个能量图标，使用原版额外参数：

```json
{
  "MY_CARD.description": "下一张牌耗能变为0{energyPrefix:energyIcons(1)}。"
}
```

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
var damageVar = card.DynamicVars.GetRequired<DamageVar>("Damage");
var displayedValue = card.DynamicVars.EvaluateValueOrDefault("ComputedDamage", target: target);
```

Use `TryGet<T>` for optional typed access, `GetRequired<T>` when absence is a content error, `TryComputeValue` or `GetComputedValue` for any RitsuLib computed-var subtype, and `EvaluateValueOrDefault` when callers should transparently accept either a fixed or computed variable.

The `...OrDefault` and `Try...` helpers do not throw for missing keys. Required helpers throw a descriptive exception for missing or mismatched variables.

:::

## 安全读取{lang="zh-CN"}

::: zh-CN

当卡牌或效果需要读取另一张牌的动态变量时，使用扩展方法：

```csharp
var amount = card.DynamicVars.GetIntOrDefault("damage");
var hasHeat = card.DynamicVars.HasPositiveValue("heat");
var damageVar = card.DynamicVars.GetRequired<DamageVar>("Damage");
var displayedValue = card.DynamicVars.EvaluateValueOrDefault("ComputedDamage", target: target);
```

可选的强类型访问使用 `TryGet<T>`；变量缺失属于内容错误时使用 `GetRequired<T>`；任意 RitsuLib 计算变量可用 `TryComputeValue` 或 `GetComputedValue`；调用方需要同时兼容固定变量和计算变量时使用 `EvaluateValueOrDefault`。

`...OrDefault` 与 `Try...` 方法在 key 缺失时不会抛出异常。Required 方法会在变量缺失或类型不匹配时抛出带具体信息的异常。

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
