---
title:
  en: Card Dynamic Var Toolkit
  zh-CN: 卡牌动态变量工具包
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

This document describes how RitsuLib creates card dynamic variables, how tooltip binding works, and how values are injected when a card is hovered.

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

本文介绍 RitsuLib 提供的卡牌动态变量创建方式、悬浮提示绑定规则及其在卡牌悬停时的注入机制。

---

:::

## Vanilla DynamicVar System{lang="en"}

::: en

> The following describes the game engine’s own dynamic variable system. RitsuLib builds convenience constructors on top of it.

The game’s `DynamicVar` system lets cards carry values that can change at runtime. Each `DynamicVar` subclass may carry extra metadata for formatters (for example `DamageVar` for highlighting, `EnergyVar` for colors). For the full list of subclasses, see [LocString Placeholder Resolution](/guide/loc-string-placeholder-resolution).

---

:::

## 游戏原版 DynamicVar 系统{lang="zh-CN"}

::: zh-CN

> 以下描述游戏引擎自身的动态变量系统，RitsuLib 在此基础上提供便捷构造器。

游戏的 `DynamicVar` 系统让卡牌在运行时携带可变数值。每个 `DynamicVar` 子类可携带额外元数据供格式化器读取（如 `DamageVar` 带高亮、`EnergyVar` 带颜色）。完整子类列表见 [LocString 占位符解析](/guide/loc-string-placeholder-resolution)。

---

:::

## RitsuLib Capabilities{lang="en"}

::: en

On top of the vanilla system, RitsuLib provides:

- **`ModCardVars`** — convenient variable constructors
- **`DynamicVarExtensions`** — each variable can bind its own tooltip independently
- **Automatic injection** — on card hover, all bound tooltips are appended automatically (implemented via patches; no extra setup)

---

:::

## RitsuLib 提供的能力{lang="zh-CN"}

::: zh-CN

在游戏原版基础上，RitsuLib 提供：

- **`ModCardVars`** — 便捷变量构造器
- **`DynamicVarExtensions`** — 每个变量可独立绑定悬浮提示
- **自动注入** — 卡牌悬停时自动注入所有已绑定悬浮提示（由补丁实现，无需额外配置）

---

:::

## Variable Construction{lang="en"}

::: en

Create variables with `ModCardVars` and add them to the card’s `DynamicVarSet`:

```csharp
public class MyCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
    private static readonly DynamicVar _charges =
        ModCardVars.Int("charges", amount: 3)
            .WithSharedTooltip("my_mod_charges");

    private static readonly DynamicVar _label =
        ModCardVars.String("flavor", value: "wine");

    public override DynamicVarSet CreateDynamicVars() =>
        new DynamicVarSet().Add(_charges).Add(_label);
}
```

| Method | Description |
|---|---|
| `ModCardVars.Int(name, amount)` | Creates a numeric variable (`decimal`) |
| `ModCardVars.String(name, value)` | Creates a string variable |
| `ModCardVars.Computed(...)` | Creates a computed variable |

RitsuLib does not assign gameplay semantics to these variables. Their meaning is entirely defined by the content author.

---

:::

## 变量构造{lang="zh-CN"}

::: zh-CN

通过 `ModCardVars` 创建变量，并在卡牌的 `DynamicVarSet` 中使用：

```csharp
public class MyCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
    private static readonly DynamicVar _charges =
        ModCardVars.Int("charges", amount: 3)
            .WithSharedTooltip("my_mod_charges");

    private static readonly DynamicVar _label =
        ModCardVars.String("flavor", value: "wine");

    public override DynamicVarSet CreateDynamicVars() =>
        new DynamicVarSet().Add(_charges).Add(_label);
}
```

| 方法 | 说明 |
|---|---|
| `ModCardVars.Int(name, amount)` | 创建数值变量（`decimal`） |
| `ModCardVars.String(name, value)` | 创建字符串变量 |
| `ModCardVars.Computed(...)` | 创建计算变量 |

RitsuLib 不为这些变量赋予玩法语义，变量的具体含义完全由内容作者定义。

---

:::

## Tooltip Binding{lang="en"}

::: en

Bind tooltips at definition time via chained extension methods:

### Shared tooltip (recommended)

Reads keys from the `static_hover_tips` table:

```csharp
var myVar = ModCardVars.Int("my_var", 2)
    .WithSharedTooltip("my_mod_my_var");
// Resolves:
//   static_hover_tips["my_mod_my_var.title"]
//   static_hover_tips["my_mod_my_var.description"]
```

### Explicit table / key

```csharp
var myVar = ModCardVars.Int("my_var", 2)
    .WithTooltip(
        titleTable: "card_keywords",
        titleKey:   "my_mod_my_var.title",
        iconPath:   "res://MyMod/art/kw.png");
```

### Custom factory

```csharp
var myVar = ModCardVars.Int("my_var", 2)
    .WithTooltip(var => new HoverTip(
        new LocString("my_table", "my_var.title"),
        new LocString("my_table", "my_var.description")));
```

---

:::

## 悬浮提示绑定{lang="zh-CN"}

::: zh-CN

在变量定义时通过扩展方法链式绑定：

### 绑定共享悬浮提示（推荐）

从 `static_hover_tips` 表读取键：

```csharp
var myVar = ModCardVars.Int("my_var", 2)
    .WithSharedTooltip("my_mod_my_var");
// 解析：
//   static_hover_tips["my_mod_my_var.title"]
//   static_hover_tips["my_mod_my_var.description"]
```

### 绑定指定表/键

```csharp
var myVar = ModCardVars.Int("my_var", 2)
    .WithTooltip(
        titleTable: "card_keywords",
        titleKey:   "my_mod_my_var.title",
        iconPath:   "res://MyMod/art/kw.png");
```

### 绑定自定义工厂方法

```csharp
var myVar = ModCardVars.Int("my_var", 2)
    .WithTooltip(var => new HoverTip(
        new LocString("my_table", "my_var.title"),
        new LocString("my_table", "my_var.description")));
```

---

:::

## Localization Example{lang="en"}

::: en

When using `WithSharedTooltip("my_mod_charges")`, provide entries in your `static_hover_tips` localization file:

```json
{
  "my_mod_charges.title": "Charges",
  "my_mod_charges.description": "Accumulated charges that deal extra damage."
}
```

RitsuLib does not ship built-in localization entries for these; if you use `WithSharedTooltip`, you must supply the strings yourself.

---

:::

## 本地化示例{lang="zh-CN"}

::: zh-CN

使用 `WithSharedTooltip("my_mod_charges")` 时，需在 `static_hover_tips` 本地化文件中提供：

```json
{
  "my_mod_charges.title": "充能",
  "my_mod_charges.description": "累积的充能层数，造成额外伤害。"
}
```

RitsuLib 不提供内置本地化词条，使用 `WithSharedTooltip` 时词条须由作者自行提供。

---

:::

## Card Hover Injection{lang="en"}

::: en

RitsuLib’s patches automatically append every dynamic variable in `CardModel.DynamicVars` that has a bound tooltip to the end of the hover-tip sequence. No extra configuration is required.

---

:::

## 卡牌悬浮提示注入{lang="zh-CN"}

::: zh-CN

RitsuLib 的补丁会在卡牌悬停时自动将 `CardModel.DynamicVars` 中所有已绑定悬浮提示的变量追加到提示序列末尾，无需额外配置。

---

:::

## Clone Behavior{lang="en"}

::: en

When `DynamicVar.Clone()` runs, tooltip metadata bound on the source variable is copied to the clone. Upgraded or duplicated cards in combat therefore behave correctly without extra handling.

---

:::

## 克隆行为{lang="zh-CN"}

::: zh-CN

调用 `DynamicVar.Clone()` 时，绑定在原变量上的悬浮提示元数据会一并复制到克隆对象。战斗中升级或复制卡牌时行为正确，无需额外处理。

---

:::

## Reading Variable Values at Runtime{lang="en"}

::: en

Read values through `DynamicVarExtensions`:

```csharp
int charges = card.DynamicVars.GetIntOrDefault("charges");
decimal val = card.DynamicVars.GetValueOrDefault("charges");
bool active = card.DynamicVars.HasPositiveValue("charges");
```

---

:::

## 运行时读取变量值{lang="zh-CN"}

::: zh-CN

通过 `DynamicVarExtensions` 扩展方法读取：

```csharp
int charges = card.DynamicVars.GetIntOrDefault("charges");
decimal val = card.DynamicVars.GetValueOrDefault("charges");
bool active = card.DynamicVars.HasPositiveValue("charges");
```

---

:::

## Related Documents{lang="en"}

::: en

- [Content Authoring Toolkit](/guide/content-authoring-toolkit)
- [Getting Started](/guide/getting-started)
- [LocString Placeholder Resolution](/guide/loc-string-placeholder-resolution)

:::

## 相关文档{lang="zh-CN"}

::: zh-CN

- [内容注册规则](/guide/content-authoring-toolkit)
- [快速入门](/guide/getting-started)
- [LocString 占位符解析](/guide/loc-string-placeholder-resolution)

:::
