# 角色与解锁脚手架

本文介绍 RitsuLib 提供的角色创建模板、内容池定义、纪元模板及解锁规则注册方式，并附完整示例。

---

## 概览

一个完整的角色 Mod 通常包含以下部分：

| 内容 | 基类 | 示例 |
|---|---|---|
| 卡池 | `TypeListCardPoolModel` | `WineFoxCardPool` |
| 遗物池 | `TypeListRelicPoolModel` | `WineFoxRelicPool` |
| 药水池 | `TypeListPotionPoolModel` | `WineFoxPotionPool` |
| 角色 | `ModCharacterTemplate<TCard, TRelic, TPotion>` | `WineFoxCharacter` |
| 故事 | `ModStoryTemplate` | `WineFoxStory` |
| 纪元 | `CharacterUnlockEpochTemplate<T>` 或自定义 | `WineFoxEpoch2` |

---

## 内容池定义

使用 `TypeList*PoolModel` 通过类型列表声明池内容，无需手动处理 `ModelId`：

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
    // 无专属药水时留空
    protected override IEnumerable<Type> PotionTypes => [];
}
```

### 配置卡牌边框颜色（HSV）

`TypeListCardPoolModel` 支持直接覆盖 `PoolFrameMaterial`。当该属性返回非空材质时，会优先使用这个材质渲染卡牌边框，不再依赖 `CardFrameMaterialPath`。

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

    // 直接用 HSV 生成边框材质：H=0.55, S=0.45, V=0.95
    public override Material? PoolFrameMaterial =>
        MaterialUtils.CreateHsvShaderMaterial(0.55f, 0.45f, 0.95f);
}
```

若你希望继续走资源路径模式，也可以不覆盖 `PoolFrameMaterial`，仅覆盖 `CardFrameMaterialPath`。

### 配置池能量图标

`TypeList*PoolModel` 现在也支持统一配置能量图标：

- `BigEnergyIconPath`：通过 `EnergyIconHelper` 解析的大图标
- `TextEnergyIconPath`：卡牌描述富文本里使用的小图标

```csharp
public class WineFoxCardPool : TypeListCardPoolModel
{
    protected override IEnumerable<Type> CardTypes =>
    [
        typeof(WineFoxStrike),
        typeof(WineFoxDefend),
    ];

    public override string? BigEnergyIconPath => "res://WineFox/ui/energy/winefox_energy_big.png";
    public override string? TextEnergyIconPath => "res://WineFox/ui/energy/winefox_energy_text.png";
}
```

---

## 角色模板

继承 `ModCharacterTemplate<TCardPool, TRelicPool, TPotionPool>`，指定三个池类型参数，并声明初始牌组以及你真正想替换的资源即可。

未填写的角色资源会自动回退到 `PlaceholderCharacterId`，默认值为 `ironclad`。

```csharp
public class WineFoxCharacter : ModCharacterTemplate<WineFoxCardPool, WineFoxRelicPool, WineFoxPotionPool>
{
    // 初始牌组（框架自动按类型解析为 ModelId）
    protected override IEnumerable<Type> StartingDeckTypes =>
    [
        typeof(WineFoxStrike), typeof(WineFoxStrike), typeof(WineFoxStrike),
        typeof(WineFoxDefend), typeof(WineFoxDefend),
    ];

    // 起始遗物
    protected override IEnumerable<Type> StartingRelicTypes =>
    [
        typeof(WineFoxStarterRelic),
    ];

    public override string? PlaceholderCharacterId => "ironclad";

    // 资源路径（使用 AssetProfile 统一配置）
    public override CharacterAssetProfile AssetProfile => new(
        Spine: new(
            CombatSkeletonDataPath: "res://WineFox/spine/wine_fox.tres"),
        Ui: new(
            IconTexturePath: "res://WineFox/art/icon.png",
            CharacterSelectBgPath: "res://WineFox/art/select_bg.tscn"),
        Scenes: new(
            RestSiteAnimPath: "res://WineFox/scenes/rest_site/winefox_rest_site.tscn"));
}
```

如果你更想继承 `silent`、`defect` 等角色的商人 / 休息点 / 小地图 / 默认音效风格，可以改写 `PlaceholderCharacterId`。若你想关闭这层兜底，可返回 `null`。

---

## 故事模板

继承 `ModStoryTemplate`，定义一个故事节点以及它在时间线中展开的纪元序列：

```csharp
public class WineFoxStory : ModStoryTemplate
{
    protected override string StoryKey => "wine-fox";

    protected override IEnumerable<Type> EpochTypes =>
    [
        typeof(WineFoxCharacterEpoch),
        typeof(WineFoxEpoch2),
    ];
}
```

### Ancient 对话本地化

RitsuLib 现在会在 `AncientDialogueSet.PopulateLocKeys` 之前，自动为已注册的 Mod 角色追加基于本地化定义的 Ancient 对话。

Key 格式与原版保持一致：

- 对话行：`<ancientEntry>.talk.<characterEntry>.<dialogueIndex>-<lineIndex>[r].ancient|char`
- 可选音效：末尾追加 `.sfx`
- 可选 visit 覆盖：末尾追加 `-visit`
- Architect 专用攻击者覆盖：末尾追加 `-attack`

如果你需要直接操作这些工具方法，可使用 `STS2RitsuLib.Localization.AncientDialogueLocalization`。

---

## 纪元模板

RitsuLib 提供预置的纪元模板，用于常见解锁目标：

| 模板 | 说明 |
|---|---|
| `CharacterUnlockEpochTemplate<TCharacter>` | 解锁角色本身的纪元 |
| `CardUnlockEpochTemplate` | 解锁额外卡牌的纪元 |
| `RelicUnlockEpochTemplate` | 解锁额外遗物的纪元 |
| `PotionUnlockEpochTemplate` | 解锁额外药水的纪元 |

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

## 完整注册示例

```csharp
RitsuLibFramework.CreateContentPack("STS2-WineFox")
    // 卡牌（指定所属池）
    .Card<WineFoxCardPool, WineFoxStrike>()
    .Card<WineFoxCardPool, WineFoxDefend>()
    .Card<WineFoxCardPool, WineFoxSignatureCard>()
    .Card<WineFoxCardPool, WineFoxAdvancedCard>()

    // 遗物
    .Relic<WineFoxRelicPool, WineFoxStarterRelic>()

    // 角色
    .Character<WineFoxCharacter>()

    // 故事与纪元
    .Story<WineFoxStory>()
    .Epoch<WineFoxEpoch2>()

    // 解锁规则
    .RequireEpoch<WineFoxAdvancedCard, WineFoxEpoch2>()       // 纪元 2 才显示该卡
    .UnlockEpochAfterRunAs<WineFoxCharacter, WineFoxEpoch2>() // 用酒狐跑完一局解锁纪元 2

    .Apply();
```

---

## 模型 ID 与本地化

通过 RitsuLib 注册的角色模型遵循与其他内容相同的 `ModelId.Entry` 规则（参见 [ContentAuthoringToolkit.md](ContentAuthoringToolkit.md)）。

示例（Mod id `STS2-WineFox`，类型 `WineFoxCharacter`）：
- `ModelId.Entry` → `STS2_WINE_FOX_CHARACTER_WINE_FOX`
- 本地化 Key → `STS2_WINE_FOX_CHARACTER_WINE_FOX.title`

> 重命名 CLR 类型会改变其推导出的 Entry，影响存档兼容性。发布后请勿随意重命名。

---

## 依赖规则

- 卡牌/遗物/药水类型必须在运行时模型查找发生前完成注册
- 角色引用的池类型必须已经注册
- 所有模型（包括受解锁条件限制的内容）均必须完成注册，解锁规则**不**替代注册

---

## 相关文档

- [内容注册规则](ContentAuthoringToolkit.md)
- [快速入门](GettingStarted.md)
