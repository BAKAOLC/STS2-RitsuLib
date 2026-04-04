# LocString 占位符解析

本文档分为两部分：**游戏原版机制**（`LocString`、SmartFormat 配置、内置格式化器）和**扩展指南**（Mod 如何注册自定义 `IFormatter`）。

---

## 第一部分：游戏原版机制

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

## 第二部分：自定义格式化器（Mod）

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

## 相关文档

- [本地化与关键词](LocalizationAndKeywords.md)
- [卡牌动态变量](CardDynamicVarToolkit.md)
- [补丁系统](PatchingGuide.md)
- [内容注册规则](ContentAuthoringToolkit.md)
