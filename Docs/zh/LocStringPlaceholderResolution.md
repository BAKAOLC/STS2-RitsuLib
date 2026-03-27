# LocString 占位符解析

本文档分为两部分：

1. **游戏原版**的占位符解析机制——帮助 Mod 作者理解运行时文本是如何被格式化的
2. **扩展指南**——如何通过补丁注册自定义格式化器

---

## 第一部分：游戏原版占位符系统

> 以下内容描述的是杀戮尖塔 2 引擎自身的本地化解析机制，不是 RitsuLib 提供的功能。

### LocString 基础

`LocString` 是游戏核心的本地化类型。它持有本地化表和键的引用，以及一组变量字典；在运行时调用 `GetFormattedText()` 时，变量会被插入到文本中。

实际的占位符解析由 `SmartFormat` 库处理，游戏在 `LocManager.LoadLocFormatters` 中注册了一组自定义格式化器。

### 占位符语法

本地化文本中的占位符遵循 SmartFormat 语法：

- 简单变量：`{variableName}`
- 格式化变量：`{variableName:formatterName}`
- 带选项的格式化：`{variableName:formatterName:options}`

示例：

```json
{
  "damage_text": "对所有敌人造成 {damage} 点伤害。",
  "energy_text": "本回合获得 {energy:energyIcons}。"
}
```

### 变量存储

变量存储在 `LocString` 实例的字典中：

```csharp
var locString = new LocString("cards", "strike");
locString.Add("damage", 6);
locString.Add("target", "enemy");
string result = locString.GetFormattedText();
```

`Add` 方法存储命名值。变量名中的空格会被替换为连字符。

### SmartFormat 内置格式化器

以下是游戏注册的标准 SmartFormat 扩展：

| 格式化器 | 说明 |
|---|---|
| `ListFormatter` | 列表格式化 |
| `DictionarySource` | 从字典读取 |
| `ValueTupleSource` | 值元组处理 |
| `ReflectionSource` | 反射访问属性 |
| `DefaultSource` | 默认源处理器 |
| `PluralLocalizationFormatter` | 基于语言环境的复数处理 |
| `ConditionalFormatter` | 条件格式化 |
| `ChooseFormatter` | 选择格式化 |
| `SubStringFormatter` | 子字符串提取 |
| `IsMatchFormatter` | 正则匹配 |
| `DefaultFormatter` | 默认格式化处理器 |

### 游戏自定义格式化器

以下是杀戮尖塔 2 自己注册的格式化器：

#### `abs` — AbsoluteValueFormatter

将数值格式化为绝对值。

```json
{ "text": "失去 {damage:abs} 点生命值。" }
```

#### `energyIcons` — EnergyIconsFormatter

将能量值转换为能量图标。

```json
{ "text": "本回合获得 {energy:energyIcons}。" }
```

- 值 1–3：显示为独立图标
- 值 ≥ 4：显示数字后跟单个图标
- 优先使用角色特定的能量图标颜色

#### `starIcons` — StarIconsFormatter

将数值转换为星星图标。

```json
{ "text": "升级 {count:starIcons} 张卡牌。" }
```

#### `diff` — HighlightDifferencesFormatter

使用颜色编码高亮值变化（升级通常为绿色）。

```json
{ "text": "伤害：{damage:diff}" }
```

#### `inverseDiff` — HighlightDifferencesInverseFormatter

使用反向颜色编码高亮值变化。

```json
{ "text": "费用：{cost:inverseDiff}" }
```

#### `percentMore` — PercentMoreFormatter

将乘数转换为百分比增加。对于值 `1.25`，输出 `25`。

```json
{ "text": "造成 {multiplier:percentMore}% 更多伤害。" }
```

#### `percentLess` — PercentLessFormatter

将乘数转换为百分比减少。对于值 `0.75`，输出 `25`。

```json
{ "text": "费用减少 {discount:percentLess}%。" }
```

#### `show` — ShowIfUpgradedFormatter

基于升级状态条件性地显示内容，以管道符 `|` 分隔选项。

```json
{ "text": "{var:show:升级文本|普通文本}" }
```

- 升级时：显示 `|` 之前的内容
- 普通时：显示 `|` 之后的内容
- 预览升级时：以绿色显示升级文本

### DynamicVar 类型

游戏使用 `DynamicVar` 子类携带额外元数据，以供格式化器读取：

| 类型 | 说明 |
|---|---|
| `DamageVar` | 带高亮的伤害值 |
| `BlockVar` | 格挡值 |
| `EnergyVar` | 带颜色信息的能量值 |
| `CalculatedVar` | 计算值（中间基类） |
| `CalculatedDamageVar` | 计算伤害 |
| `CalculatedBlockVar` | 计算格挡 |
| `ExtraDamageVar` | 额外伤害值 |
| `BoolVar` | 布尔值 |
| `IntVar` | 整数值 |
| `StringVar` | 字符串值 |
| `GoldVar` | 金币数量 |
| `HealVar` | 治疗量 |
| `HpLossVar` | 生命损失 |
| `MaxHpVar` | 最大生命值 |
| `PowerVar<T>` | 能力值（泛型） |
| `StarsVar` | 星星数量 |
| `CardsVar` | 卡牌引用 |
| `IfUpgradedVar` | 升级状态指示器 |
| `ForgeVar` | 锻造值 |
| `RepeatVar` | 重复次数 |
| `SummonVar` | 召唤值 |

### 格式化流程

1. 调用 `LocString.GetFormattedText()`
2. `LocManager.SmartFormat()` 从本地化表获取原始文本
3. 根据键是否已本地化选择合适的 `CultureInfo`
4. `SmartFormatter.Format()` 使用变量处理文本
5. 根据格式字符串中的指定应用自定义格式化器
6. 格式化失败时，返回原始文本并记录错误

### 错误处理

格式化失败时：

1. 捕获 `FormattingException` 或 `ParsingErrors`
2. 记录包含表、键和变量的错误消息
3. 基于错误模式创建 Sentry 事件指纹
4. 返回原始文本作为回退

这确保本地化错误不会导致游戏崩溃。

### 高级语法

游戏支持复杂的嵌套格式化模式：

#### 条件格式化

```json
{ "text": "{HasRider:此卡有附加效果|此卡无附加效果}" }
```

#### 选择格式化

```json
{ "text": "{CardType:choose(Attack|Skill|Power):攻击文本|技能文本|能力文本}" }
```

#### 嵌套格式化器

```json
{
  "text": "{Violence:造成 {Damage:diff()} 点伤害 {ViolenceHits:diff()} 次|造成 {Damage:diff()} 点伤害}"
}
```

#### BBCode 颜色标签

```json
{ "text": "获得等于 [gold]格挡[/gold] [green]{value}[/green]" }
```

常用颜色标签：

- `[gold]...[/gold]` — 金色高亮
- `[green]...[/green]` — 绿色高亮（增益）
- `[red]...[/red]` — 红色高亮（减益）

---

## 第二部分：为 Mod 添加自定义格式化器

> 以下内容描述如何通过补丁扩展游戏的格式化器注册，需要使用 RitsuLib 的补丁系统。

### 实现步骤

1. 创建一个实现 `SmartFormat.Core.Extensions.IFormatter` 的类
2. 设置 `Name` 为格式化器标识符
3. 实现 `TryEvaluateFormat` 处理格式化逻辑
4. 通过补丁在 `LocManager.LoadLocFormatters` 中注册

示例：

```csharp
public class MyCustomFormatter : IFormatter
{
    public string Name { get => "myCustom"; set { } }
    public bool CanAutoDetect { get; set; }

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        var value = formattingInfo.CurrentValue;
        formattingInfo.Write($"处理后: {value}");
        return true;
    }
}
```

注册补丁示例（使用 RitsuLib 补丁系统）：

```csharp
public class RegisterMyFormatterPatch : IPatchMethod
{
    public static string PatchId => "register_my_formatter";
    public static string Description => "Register custom SmartFormat formatter";
    public static bool IsCritical => true;

    public static ModPatchTarget[] GetTargets()
    {
        return [new(typeof(LocManager), "LoadLocFormatters")];
    }

    public static void Postfix(SmartFormatter ____smartFormatter)
    {
        ____smartFormatter.AddExtensions(new MyCustomFormatter());
    }
}
```

---

## 相关文档

- [本地化与关键词](LocalizationAndKeywords.md)
- [卡牌动态变量](CardDynamicVarToolkit.md)
- [补丁系统](PatchingGuide.md)
- [内容注册规则](ContentAuthoringToolkit.md)
