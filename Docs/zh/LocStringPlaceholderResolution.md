# LocString 占位符解析机制

游戏的本地化系统使用一套复杂的占位符解析机制，用于在运行时将动态值插入到本地化文本中。本文档解释该系统的工作原理以及如何扩展自定义格式化器。

---

## 概述

LocString 是游戏核心的本地化类型。它持有本地化表和键的引用，以及一个变量字典，这些变量可以在运行时插入到文本中。

实际的占位符解析由 `SmartFormat` 库处理，该库配置了针对杀戮尖塔2需求的自定义格式化器。

---

## 基本占位符语法

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

---

## 变量存储

变量存储在 LocString 实例的字典中：

```csharp
var locString = new LocString("cards", "strike");
locString.Add("damage", 6);
locString.Add("target", "enemy");
string result = locString.GetFormattedText();
```

`Add` 方法存储带名称的值。变量名中的空格会被替换为连字符进行规范化。

---

## 自定义格式化器

游戏在 `LocManager.LoadLocFormatters` 中注册了多个自定义格式化器：

### SmartFormat 内置格式化器

这些是标准的 SmartFormat 扩展：

- **ListFormatter** - 处理列表格式化
- **DictionarySource** - 从字典读取
- **ValueTupleSource** - 处理值元组
- **ReflectionSource** - 使用反射访问属性
- **DefaultSource** - 默认源处理器
- **PluralLocalizationFormatter** - 基于语言环境的复数处理
- **ConditionalFormatter** - 条件格式化
- **ChooseFormatter** - 选择格式化
- **SubStringFormatter** - 子字符串提取
- **IsMatchFormatter** - 正则匹配
- **DefaultFormatter** - 默认格式化处理器

### 游戏特定格式化器

这些是杀戮尖塔2的自定义格式化器：

#### AbsoluteValueFormatter (`abs`)
将数值格式化为其绝对值。

```json
{
  "text": "失去 {damage:abs} 点生命值。"
}
```

#### EnergyIconsFormatter (`energyIcons`)
将能量值转换为能量图标图片。

```json
{
  "text": "本回合获得 {energy:energyIcons}。"
}
```

- 值 1-3 显示为独立图标
- 值 ≥4 显示数字后跟单个图标
- 优先使用角色特定的能量图标颜色

#### StarIconsFormatter (`starIcons`)
将数值转换为星星图标图片。

```json
{
  "text": "升级 {count:starIcons} 张卡牌。"
}
```

#### HighlightDifferencesFormatter (`diff`)
使用颜色编码高亮显示值变化（升级通常为绿色）。

```json
{
  "text": "伤害：{damage:diff}"
}
```

#### HighlightDifferencesInverseFormatter (`inverseDiff`)
使用反向颜色编码高亮显示值变化。

```json
{
  "text": "费用：{cost:inverseDiff}"
}
```

#### PercentMoreFormatter (`percentMore`)
将乘数转换为百分比增加。

```json
{
  "text": "造成 {multiplier:percentMore}% 更多伤害。"
}
```

对于值 1.25，输出 "25"。

#### PercentLessFormatter (`percentLess`)
将乘数转换为百分比减少。

```json
{
  "text": "费用减少 {discount:percentLess}%。"
}
```

对于值 0.75，输出 "25"。

#### ShowIfUpgradedFormatter (`show`)
基于升级状态条件性地显示内容。使用管道符 `|` 作为分隔符。

```json
{
  "text": "{var:show:升级文本|普通文本}"
}
```

- 升级时：显示第一段（`|` 之前）
- 普通时：显示第二段（`|` 之后）
- 预览升级时：显示绿色的第一段

---

## 动态变量

游戏使用专门的变量类型（`DynamicVar` 子类），这些类型携带额外的元数据用于格式化：

- **DamageVar** - 带高亮的伤害值
- **BlockVar** - 格挡值
- **EnergyVar** - 带颜色信息的能量值
- **CalculatedVar** - 计算值
- **CalculatedDamageVar** - 计算伤害
- **CalculatedBlockVar** - 计算格挡
- **BoolVar** - 布尔值
- **IntVar** - 整数值
- **StringVar** - 字符串值
- **GoldVar** - 金币数量
- **HealVar** - 治疗量
- **MaxHpVar** - 最大生命值
- **PowerVar** - 能力值
- **StarsVar** - 星星数量
- **CardsVar** - 卡牌引用
- **IfUpgradedVar** - 升级状态指示器

这些 DynamicVar 类型使格式化器能够访问简单值之外的额外上下文。

---

## 格式化流程

1. 调用 `LocString.GetFormattedText()`
2. `LocManager.SmartFormat()` 从本地化表获取原始文本
3. 根据键是否已本地化选择合适的 `CultureInfo`
4. `SmartFormatter.Format()` 使用变量处理文本
5. 根据格式字符串中的指定应用自定义格式化器
6. 如果格式化失败，返回原始文本并记录错误

---

## 如何添加自定义格式化器

要添加新的自定义格式化器：

1. 创建一个实现 `SmartFormat.Core.Extensions.IFormatter` 的类
2. 设置 `Name` 属性为格式化器的标识符
3. 实现 `TryEvaluateFormat` 来处理格式化逻辑
4. 在 `LocManager.LoadLocFormatters` 中注册格式化器

示例：

```csharp
public class MyCustomFormatter : IFormatter
{
    public string Name
    {
        get => "myCustom";
        set => throw new NotImplementedException();
    }

    public bool CanAutoDetect { get; set; }

    public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
    {
        var value = formattingInfo.CurrentValue;
        // 处理值并写入输出
        formattingInfo.Write($"处理后: {value}");
        return true;
    }
}
```

然后在 `LocManager.LoadLocFormatters` 中注册：

```csharp
_smartFormatter.AddExtensions(new MyCustomFormatter());
```

---

## 错误处理

当格式化失败时：

1. 捕获异常（FormattingException 或 ParsingErrors）
2. 记录包含表、键和变量的错误消息
3. 基于错误模式创建 Sentry 事件指纹
4. 返回原始文本（未格式化）作为回退

这确保本地化错误不会导致游戏崩溃。

---

## 高级语法

游戏支持复杂的嵌套格式化模式，适用于多效果卡牌：

### 条件格式化

使用布尔变量条件性地显示文本：

```json
{
  "text": "{HasRider:此卡有附加效果|此卡无附加效果}"
}
```

- 如果 `HasRider` 为 true：显示"此卡有附加效果"
- 如果 `HasRider` 为 false：显示"此卡无附加效果"

### 选择格式化

使用 `choose` 根据变量值选择文本：

```json
{
  "text": "{CardType:choose(Attack|Skill|Power):攻击文本|技能文本|能力文本}"
}
```

- 如果 `CardType` 是 "Attack"：显示"攻击文本"
- 如果 `CardType` 是 "Skill"：显示"技能文本"
- 如果 `CardType` 是 "Power"：显示"能力文本"

### 管道符分隔符

管道符 `|` 在条件和选择格式化器中分隔选项：

```json
{
  "text": "{condition:真文本|假文本}"
}
```

### 嵌套格式化器

格式化器可以嵌套以实现复杂逻辑：

```json
{
  "text": "{Violence:造成 {Damage:diff()} 点伤害 {ViolenceHits:diff()} 次|造成 {Damage:diff()} 点伤害}"
}
```

当 `Violence` 为 true 时显示伤害和攻击次数，否则只显示伤害。

### BBCode 颜色标签

使用 BBCode 风格的标签为文本着色：

```json
{
  "text": "获得等于 [gold]格挡[/gold] [green]{value}[/green]"
}
```

常用颜色标签：
- `[gold]...[/gold]` - 金色/黄色高亮
- `[green]...[/green]` - 绿色高亮（增益）
- `[red]...[/red]` - 红色高亮（减益）

---

## 疯狂科学卡牌示例

"疯狂科学"卡牌展示了复杂的占位符解析：

```json
{
  "MAD_SCIENCE.description": "{CardType:choose(Attack|Skill|Power):造成{Damage:diff()}点伤害{Violence:{ViolenceHits:diff()}次|}。|获得{Block:diff()}点[gold]格挡[/gold]。|}{HasRider:{Sapping:\n给予{SappingWeak:diff()}层[gold]虚弱[/gold]。\n给予{SappingVulnerable:diff()}层[gold]易伤[/gold]。|}{Choking:\n本回合，你每打出一张牌，该敌人失去{ChokingDamage:diff()}点生命。|}{Energized:\n获得{EnergizedEnergy:energyIcons()}。|}{Wisdom:\n抽{WisdomCards:diff()}张牌|}{Chaos:\n将一张随机牌放入你的[gold]手牌[/gold]，这张牌在本回合耗能变为0{energyPrefix:energyIcons(1)}。|}{Expertise:获得{ExpertiseStrength:diff()}点[gold]力量[/gold]。\n获得{ExpertiseDexterity:diff()}点[gold]敏捷[/gold]。|}{Curious:能力牌的耗能减少{CuriousReduction:diff()}{energyPrefix:energyIcons(1)}。|}{Improvement:在战斗结束时，[gold]升级[/gold]你牌组中的一张随机牌。|}|{CardType:choose(Attack|Skill|Power):\n？？？|\n？？？|？？？}}"
}
```

### 分析

此卡牌显示：
1. **基础效果**基于 `CardType`（攻击/技能/能力）
   - 攻击：造成伤害，可选暴力效果多次攻击
   - 技能：获得格挡
   - 能力：基础效果为空（由附加效果决定）
2. **可选附加效果**基于 `HasRider` 布尔值
3. **多种附加效果类型**：
   - **Sapping**：给予虚弱和易伤
   - **Choking**：敌人在打出卡牌时失去生命
   - **Energized**：获得能量图标
   - **Wisdom**：抽牌
   - **Chaos**：获得随机牌且耗能为0
   - **Expertise**：获得力量和敏捷
   - **Curious**：能力牌耗能减少
   - **Improvement**：战斗结束时升级随机牌
4. **嵌套 diff 格式化器**用于值高亮
5. **能量图标**使用 `energyIcons()` 格式化器
6. **BBCode 颜色**用于关键词

### 变量如何添加

在 `MadScience.AddExtraArgsToDescription` 中：

```csharp
protected override void AddExtraArgsToDescription(LocString description)
{
    description.Add("CardType", TinkerTimeType.ToString());
    description.Add("HasRider", TinkerTimeRider != TinkerTime.RiderEffect.None);
    
    // 为每种附加效果添加布尔值
    TinkerTime.RiderEffect[] values = Enum.GetValues<TinkerTime.RiderEffect>();
    for (int i = 0; i < values.Length; i++)
    {
        TinkerTime.RiderEffect riderEffect = values[i];
        description.Add(riderEffect.ToString(), TinkerTimeRider == riderEffect);
    }
}
```

这添加了：
- `CardType` - 卡牌类型的字符串变量
- `HasRider` - 布尔值，指示是否有任何附加效果
- `Sapping`、`Choking`、`Energized`、`Wisdom`、`Chaos`、`Expertise`、`Curious`、`Improvement` - 每种附加效果类型的布尔值

---

## 相关文档

- [本地化与关键词](LocalizationAndKeywords.md)
- [内容注册规则](ContentAuthoringToolkit.md)
- [卡牌动态变量](CardDynamicVarToolkit.md)