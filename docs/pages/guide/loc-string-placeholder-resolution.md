---
title:
  en: LocString Placeholder Resolution
  zh-CN: LocString 占位符解析
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

This document covers two topics: the **game-native** localization system (`LocString`, SmartFormat configuration, built-in formatters) and the **extension guide** for registering custom `IFormatter` implementations from mods.

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

本文档分为两部分：**游戏原版机制**（`LocString`、SmartFormat 配置、内置格式化器）和**扩展指南**（Mod 如何注册自定义 `IFormatter`）。

---

:::

## Part 1: Game-native system{lang="en"}

::: en

> The following describes the Slay the Spire 2 engine's own localization mechanism, not RitsuLib functionality.

### Core components

- **`LocString`**: holds a localization table id, entry key, and variable dictionary; `GetFormattedText()` triggers formatting.
- **`LocManager.SmartFormat`**: retrieves the raw template from `LocTable`, selects `CultureInfo` based on whether the key is localized, then calls `SmartFormatter.Format(...)`.
- **`LocManager.LoadLocFormatters`**: constructs `SmartFormatter`, registers data sources and formatter extensions.

### Variable binding

Variables are written to `LocString` via `Add`. **Spaces in variable names are replaced with hyphens.**

```csharp
var locString = new LocString("cards", "strike");
locString.Add("damage", 6);
string result = locString.GetFormattedText();
```

### Placeholder syntax

Game localization JSON uses SmartFormat placeholders.

**Variable only** — outputs the formatted value of the variable:

```
{VariableName}
```

**With formatter** — the formatter is specified after a colon using function-call syntax. The content inside `( )` is passed to the formatter as `IFormattingInfo.FormatterOptions`:

```
{VariableName:formatterName()}
{VariableName:formatterName(options)}
```

Formatters are matched by `IFormatter.Name`. The parentheses are a required part of the invocation syntax.

**Formatters with format segments** (e.g. `show`, `choose`, `cond`) receive additional text after a second colon, split by `|`. See individual formatter notes and the advanced examples below.

**Example:**

```json
{
  "damage_text": "Deal {Damage:diff()} damage to all enemies.",
  "energy_text": "Gain {Energy:energyIcons()} this turn."
}
```

### SmartFormat built-in extensions

Standard SmartFormat extensions registered by the game (non-exhaustive):

| Type | Role |
|------|------|
| `ListFormatter` | List formatting |
| `DictionarySource` | Keyed variable lookup |
| `ValueTupleSource` | Value tuples |
| `ReflectionSource` | Reflection-based property access |
| `DefaultSource` | Fallback source |
| `PluralLocalizationFormatter` | Locale-sensitive pluralization |
| `ConditionalFormatter` | Conditional formatting |
| `ChooseFormatter` | `choose(...)` |
| `SubStringFormatter` | Substrings |
| `IsMatchFormatter` | Regex matching |
| `LocaleNumberFormatter` | Locale number formatting |
| `DefaultFormatter` | Fallback when no formatter matches |

### Game-specific formatters

The game registers the following `IFormatter` types in `MegaCrit.Sts2.Core.Localization.Formatters`:

| `IFormatter.Name` | Placeholder | `FormatterOptions` | Notes |
|-------------------|-----------|--------------------|-------|
| `abs` | `{v:abs()}` | unused | Outputs the absolute value of a number |
| `energyIcons` | `{Energy:energyIcons()}` or `{energyPrefix:energyIcons(n)}` | Required as integer icon count when `CurrentValue` is `string` | Renders a value as energy icon glyphs; see details below |
| `starIcons` | `{v:starIcons()}` | unused | Renders a value as star icon glyphs |
| `diff` | `{v:diff()}` | unused | Highlights value changes (green for upgrades); requires `DynamicVar` |
| `inverseDiff` | `{v:inverseDiff()}` | unused | Same as `diff` with inverted color direction; requires `DynamicVar` |
| `percentMore` | `{v:percentMore()}` | unused | Converts a multiplier to a percent increase, e.g. `1.25` → `25` |
| `percentLess` | `{v:percentLess()}` | unused | Converts a multiplier to a percent decrease, e.g. `0.75` → `25` |
| `show` | `{v:show:upgrade text\|normal text}` | unused (options come from the format segment split on `|`) | Conditionally shows text based on upgrade state; requires `IfUpgradedVar` |

**`energyIcons` details**

The source of the icon count depends on `CurrentValue`:

- `EnergyVar`: uses `PreviewValue` and an optional color prefix. Use `{Energy:energyIcons()}`.
- `CalculatedVar` or numeric type: uses the numeric value directly. Use `{Energy:energyIcons()}`.
- `string` (e.g. the `energyPrefix` variable used in fixed-cost text): count is read from `FormatterOptions` and must be an integer literal, e.g. `{energyPrefix:energyIcons(1)}`.

Rendering rule: counts 1–3 repeat the icon glyph; counts ≤0 or ≥4 output the digit followed by one icon.

**`show` details**

The format segment after `show:` is split on `|` into one or two child formats:

- `Upgraded`: renders the first segment.
- `Normal`: renders the second segment; if only one segment is provided, nothing is rendered.
- `UpgradePreview`: renders the first segment wrapped in `[green]...[/green]`.

### DynamicVar types

`DynamicVar` subclasses carry metadata consumed by formatters such as `diff` and `inverseDiff`:

| Type | Description |
|------|-------------|
| `DamageVar` | Damage value with highlight metadata |
| `BlockVar` | Block value |
| `EnergyVar` | Energy value with color information |
| `CalculatedVar` | Base class for calculated values |
| `CalculatedDamageVar` / `CalculatedBlockVar` | Calculated damage / block |
| `ExtraDamageVar` | Extra damage |
| `BoolVar` / `IntVar` / `StringVar` | Primitive types |
| `GoldVar` / `HealVar` / `HpLossVar` / `MaxHpVar` | Resource types |
| `PowerVar<T>` | Power value (generic) |
| `StarsVar` / `CardsVar` | Stars / card references |
| `IfUpgradedVar` | Upgrade UI display state |
| `ForgeVar` / `RepeatVar` / `SummonVar` | Other card variables |

### Formatting pipeline

1. `LocString.GetFormattedText()` is called
2. `LocManager.SmartFormat` retrieves the raw template from `LocTable`
3. `CultureInfo` is selected based on whether the key is localized
4. `SmartFormatter.Format` evaluates placeholders and dispatches to matching formatters
5. On failure (`FormattingException` or `ParsingErrors`): error is logged and the raw template is returned

### Advanced examples

**Conditional** (`ConditionalFormatter`)

```json
{ "text": "{HasRider:This card has a rider effect|This card has no rider}" }
```

**Choose** (`ChooseFormatter`)

```json
{ "text": "{CardType:choose(Attack|Skill|Power):Attack text|Skill text|Power text}" }
```

**Nested formatters**

```json
{
  "text": "{Violence:Deal {Damage:diff()} damage {ViolenceHits:diff()} times|Deal {Damage:diff()} damage}"
}
```

**BBCode color tags**

```json
{ "text": "Gain [gold]{Gold}[/gold] gold. Current HP: [green]{Hp}[/green]." }
```

Common tags: `[gold]`, `[green]`, `[red]`, `[blue]`.

---

:::

## 第一部分：游戏原版机制{lang="zh-CN"}

::: zh-CN

> 以下内容描述的是杀戮尖塔 2 引擎自身的本地化解析机制，不是 RitsuLib 提供的功能。

### 核心组件

- **`LocString`**：持有本地化表 id、条目键与变量字典，调用 `GetFormattedText()` 执行格式化。
- **`LocManager.SmartFormat`**：从 `LocTable` 取原始模板，根据键是否已本地化选择 `CultureInfo`，再由 `SmartFormatter.Format(...)` 解析。
- **`LocManager.LoadLocFormatters`**：初始化 `SmartFormatter`，注册数据源与格式化器扩展。

### 变量绑定

变量通过 `LocString.Add` 写入字典，**名称中的空格会被替换为连字符**。

```csharp
var locString = new LocString("cards", "strike");
locString.Add("damage", 6);
string result = locString.GetFormattedText();
```

### 占位符语法

游戏本地化 JSON 中使用 SmartFormat 占位符。

**仅变量名** — 直接输出变量值：

```
{VariableName}
```

**指定格式化器** — 格式化器以函数调用形式写在冒号后，括号内内容（`FormatterOptions`）由格式化器自行解读：

```
{VariableName:formatterName()}
{VariableName:formatterName(options)}
```

格式化器由 `IFormatter.Name` 匹配。`(` `)` 是调用语法的必要组成部分，不可省略。

**带额外格式段的格式化器**（如 `show`、`choose`、`cond`）在调用后通过第二个冒号传递格式文本，详见后续各格式化器说明及高级示例。

**示例：**

```json
{
  "damage_text": "对所有敌人造成 {Damage:diff()} 点伤害。",
  "energy_text": "本回合获得 {Energy:energyIcons()}。"
}
```

### SmartFormat 内置扩展

游戏注册的标准 SmartFormat 扩展（节选）：

| 类型 | 作用 |
|------|------|
| `ListFormatter` | 列表格式化 |
| `DictionarySource` | 按键读取变量 |
| `ValueTupleSource` | 值元组 |
| `ReflectionSource` | 反射访问属性 |
| `DefaultSource` | 默认数据源 |
| `PluralLocalizationFormatter` | 语言环境复数 |
| `ConditionalFormatter` | 条件格式化 |
| `ChooseFormatter` | `choose(...)` |
| `SubStringFormatter` | 子字符串 |
| `IsMatchFormatter` | 正则匹配 |
| `LocaleNumberFormatter` | 区域数字格式 |
| `DefaultFormatter` | 无匹配时的回退 |

### 游戏自定义格式化器

游戏在 `MegaCrit.Sts2.Core.Localization.Formatters` 中注册了以下 `IFormatter`：

| `IFormatter.Name` | 占位符写法 | `FormatterOptions` | 说明 |
|-------------------|-----------|--------------------|------|
| `abs` | `{v:abs()}` | 不使用 | 输出数值的绝对值 |
| `energyIcons` | `{Energy:energyIcons()}` 或 `{energyPrefix:energyIcons(n)}` | `CurrentValue` 为 `string` 时，必须提供整数参数作为图标个数 | 将数值渲染为能量图标，详见下方说明 |
| `starIcons` | `{v:starIcons()}` | 不使用 | 将数值渲染为星星图标 |
| `diff` | `{v:diff()}` | 不使用 | 以绿色（升级）高亮显示数值变化，需传入 `DynamicVar` |
| `inverseDiff` | `{v:inverseDiff()}` | 不使用 | 与 `diff` 相同但颜色方向相反，需传入 `DynamicVar` |
| `percentMore` | `{v:percentMore()}` | 不使用 | 将乘数转换为增加百分比，例如 `1.25` 输出 `25` |
| `percentLess` | `{v:percentLess()}` | 不使用 | 将乘数转换为减少百分比，例如 `0.75` 输出 `25` |
| `show` | `{v:show:升级文案\|普通文案}` | 不使用（选项由格式段 `|` 分隔提供） | 根据升级状态条件显示文案，需传入 `IfUpgradedVar` |

**`energyIcons` 用法补充**

`CurrentValue` 决定图标个数的来源：

- `EnergyVar`：使用 `PreviewValue` 与可选颜色前缀，使用 `{Energy:energyIcons()}`。
- `CalculatedVar` 或数值类型：直接使用数值，使用 `{Energy:energyIcons()}`。
- `string`（如固定文本中的 `energyPrefix` 变量）：个数由 `FormatterOptions` 提供，必须写 `energyIcons(n)`，例如 `{energyPrefix:energyIcons(1)}`。

图标渲染规则：个数 1–3 重复单独图标；个数 ≤0 或 ≥4 输出数字加单个图标。

**`show` 用法补充**

`show:` 后的格式文本按 `|` 拆分为一至两段：

- 升级状态（`Upgraded`）：渲染第一段。
- 普通状态（`Normal`）：渲染第二段；若只有一段则输出空白。
- 升级预览（`UpgradePreview`）：以绿色渲染第一段。

### DynamicVar 类型

`DynamicVar` 子类携带格式化元数据，是 `diff`、`inverseDiff` 等格式化器的必要输入：

| 类型 | 说明 |
|------|------|
| `DamageVar` | 伤害值，携带高亮元数据 |
| `BlockVar` | 格挡值 |
| `EnergyVar` | 能量值，携带颜色信息 |
| `CalculatedVar` | 计算值基类 |
| `CalculatedDamageVar` / `CalculatedBlockVar` | 计算后的伤害/格挡 |
| `ExtraDamageVar` | 额外伤害 |
| `BoolVar` / `IntVar` / `StringVar` | 基础类型 |
| `GoldVar` / `HealVar` / `HpLossVar` / `MaxHpVar` | 资源类型 |
| `PowerVar<T>` | 能力值（泛型） |
| `StarsVar` / `CardsVar` | 星/牌引用 |
| `IfUpgradedVar` | 升级显示状态 |
| `ForgeVar` / `RepeatVar` / `SummonVar` | 其它卡牌变量 |

### 格式化流程

1. 调用 `LocString.GetFormattedText()`
2. `LocManager.SmartFormat` 从 `LocTable` 取原始模板
3. 根据键是否已本地化选择 `CultureInfo`
4. `SmartFormatter.Format` 解析占位符并调用匹配的格式化器
5. 若格式化失败（`FormattingException` 或 `ParsingErrors`），记录错误并返回原始模板

### 高级示例

**条件格式**（`ConditionalFormatter`）

```json
{ "text": "{HasRider:此卡有附加效果|此卡无附加效果}" }
```

**选择格式**（`ChooseFormatter`）

```json
{ "text": "{CardType:choose(Attack|Skill|Power):攻击文本|技能文本|能力文本}" }
```

**嵌套格式化器**

```json
{
  "text": "{Violence:造成 {Damage:diff()} 点伤害 {ViolenceHits:diff()} 次|造成 {Damage:diff()} 点伤害}"
}
```

**BBCode 颜色标签**

```json
{ "text": "获得 [gold]{Gold}[/gold] 金币，当前生命 [green]{Hp}[/green]。" }
```

常用标签：`[gold]`、`[green]`、`[red]`、`[blue]`。

---

:::

## Part 2: Custom formatters (mods){lang="en"}

::: en

> The following describes how to register additional formatters via the RitsuLib patching system.

A `Postfix` patch on `LocManager.LoadLocFormatters` provides access to the `SmartFormatter` instance, which accepts additional `IFormatter` implementations.

**Implementing `IFormatter`:**

```csharp
public class MyCustomFormatter : IFormatter
{
    public string Name { get => "myCustom"; set { } }
    public bool CanAutoDetect { get; set; }

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        formattingInfo.Write($"Custom output: {formattingInfo.CurrentValue}");
        return true;
    }
}
```

- `Name` is the formatter identifier matched in placeholder strings (the `myCustom` in `{Var:myCustom()}`).
- Access `formattingInfo.FormatterOptions` to read any text supplied inside the parentheses.

**Registration patch:**

```csharp
public class RegisterMyFormatterPatch : IPatchMethod
{
    public static string PatchId => "register_my_formatter";
    public static string Description => "Register custom SmartFormat formatter";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
        => [new(typeof(LocManager), "LoadLocFormatters")];

    public static void Postfix(SmartFormatter ____smartFormatter)
        => ____smartFormatter.AddExtensions(new MyCustomFormatter());
}
```

Once registered, invoke the formatter in JSON as `{SomeVar:myCustom()}` or `{SomeVar:myCustom(args)}`.

---

:::

## 第二部分：自定义格式化器（Mod）{lang="zh-CN"}

::: zh-CN

> 以下内容描述如何通过 RitsuLib 补丁系统为游戏注册自定义格式化器。

通过对 `LocManager.LoadLocFormatters` 打 `Postfix` 补丁，可在 `SmartFormatter` 中注册额外的 `IFormatter` 实现。

**实现 `IFormatter`：**

```csharp
public class MyCustomFormatter : IFormatter
{
    public string Name { get => "myCustom"; set { } }
    public bool CanAutoDetect { get; set; }

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        formattingInfo.Write($"自定义输出: {formattingInfo.CurrentValue}");
        return true;
    }
}
```

- `Name` 是格式化器标识符，对应 JSON 中 `{Var:myCustom()}` 的 `myCustom` 部分。
- 若需要参数，通过 `formattingInfo.FormatterOptions` 读取括号内的字符串。

**注册补丁：**

```csharp
public class RegisterMyFormatterPatch : IPatchMethod
{
    public static string PatchId => "register_my_formatter";
    public static string Description => "Register custom SmartFormat formatter";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
        => [new(typeof(LocManager), "LoadLocFormatters")];

    public static void Postfix(SmartFormatter ____smartFormatter)
        => ____smartFormatter.AddExtensions(new MyCustomFormatter());
}
```

注册后，在 JSON 中通过 `{SomeVar:myCustom()}` 或 `{SomeVar:myCustom(args)}` 调用。

---

:::

## Related documents{lang="en"}

::: en

- [Localization & Keywords](/guide/localization-and-keywords)
- [Card Dynamic Variables](/guide/card-dynamic-var-toolkit)
- [Patching Guide](/guide/patching-guide)
- [Content Authoring Toolkit](/guide/content-authoring-toolkit)

:::

## 相关文档{lang="zh-CN"}

::: zh-CN

- [本地化与关键词](/guide/localization-and-keywords)
- [卡牌动态变量](/guide/card-dynamic-var-toolkit)
- [补丁系统](/guide/patching-guide)
- [内容注册规则](/guide/content-authoring-toolkit)

:::
