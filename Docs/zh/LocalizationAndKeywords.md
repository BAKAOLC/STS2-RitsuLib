# 本地化与关键词

RitsuLib 将本地化明确分为两层：

- **游戏原版的 `LocString` 模型键管线** — 模型标题、描述等游戏内文本
- **框架自带的 `I18N` 辅助本地化** — Mod 自身的辅助文本

同时提供轻量关键词注册器，用来统一悬浮提示和关键词文本。

---

## 游戏原版模型本地化

> 以下描述游戏引擎自身的本地化机制，RitsuLib 不替换此系统。

游戏通过 `LocString` 和各本地化表来读取模型文本，常见表包括：

- `cards`、`relics`、`powers`、`characters`、`card_keywords`

这些键建立在 `ModelId.Entry` 之上。

RitsuLib 的作用仅限于让模型身份更稳定、更可预测，从而使键更容易编写。具体的模型 ID 规则见 [内容注册规则](ContentAuthoringToolkit.md)。

---

## `CreateLocalization` 与 `CreateModLocalization`

`I18N` 是 RitsuLib 提供的辅助文本本地化系统，独立于游戏的 `LocString`：

```csharp
var i18n = RitsuLibFramework.CreateModLocalization(
    modId: "MyMod",
    instanceName: "MyMod-I18N",
    resourceFolders: ["MyMod.localization"],
    pckFolders: ["res://MyMod/localization"]);
```

`CreateModLocalization` 是 `CreateLocalization` 的便捷包装。如果不传文件系统目录，默认使用：

```text
user://mod-configs/<modId>/localization
```

---

## 资源合并顺序

`I18N` 支持三类来源：

1. 文件系统目录
2. 嵌入资源
3. PCK 目录

合并策略是"先到先得"：

- 先加载文件系统目录
- 嵌入资源只补缺失键
- PCK 再补剩余缺失键

这样本地覆写可以自然优先于打包默认值。

---

## 语言代码归一化

`I18N` 在加载 JSON 之前会规范化语言代码：

| 输入 | 归一化结果 |
|---|---|
| `en`、`en_us`、`eng` | `eng` |
| `zh`、`zh_cn`、`zh_hans` | `zhs` |
| `ja`、`ja_jp` | `jpn` |

无法解析的语言默认回退到 `eng`。

---

## 运行时重载行为

`I18N` 会在可能的情况下订阅语言切换事件：

- 游戏语言改变时，辅助本地化自动重载
- 重载完成后触发 `Changed` 事件
- 如果当前阶段拿不到游戏本地化管理器，则退回懒检测模式

此行为与游戏原版 `LocString` 的解析相互独立。

---

## 调试兼容模式

> 此功能通过补丁修改游戏原版 `LocTable` 的行为，仅用于调试。

开启 `debug_compatibility_mode` 后：

- `LocTable.GetLocString(...)` 缺失键时不再抛异常，而是返回基于键的占位值
- `LocTable.GetRawText(...)` 缺失键时不再抛异常，而是直接返回键本身
- 同一个缺失键只会警告一次

目标是帮助排查问题，不是替代正确的本地化编写。

---

## 关键词注册器

`ModKeywordRegistry` 用于统一定义关键词及其悬浮提示：

```csharp
var keywords = RitsuLibFramework.GetKeywordRegistry("MyMod");

keywords.RegisterCardKeyword(
    id: "brew",
    locKeyPrefix: "my_mod_brew",
    iconPath: "res://MyMod/ui/keywords/brew.png");
```

注册后会生成规范化标识，并绑定标题/描述的本地化键。

---

## 在代码里使用关键词

常用辅助方法：

| 方法 | 说明 |
|---|---|
| `ModKeywordRegistry.CreateHoverTip(id)` | 创建悬浮提示 |
| `ModKeywordRegistry.GetTitle(id)` | 获取标题 |
| `ModKeywordRegistry.GetDescription(id)` | 获取描述 |
| `keywordId.GetModKeywordCardText()` | 获取卡牌文本 |
| `enumerable.ToHoverTips()` | 批量转换为悬浮提示 |

也可以通过 `ModKeywordExtensions` 把运行时关键词挂在任意对象上：

```csharp
card.AddModKeyword("brew");

if (card.HasModKeyword("brew"))
{
    // ...
}
```

适合"关键词是否存在由运行时状态决定"的场景。

---

## Ancient 对话本地化

RitsuLib 内置了 `AncientDialogueLocalization`，它有两个作用：

- 提供从本地化键扫描对话的辅助 API
- 在游戏原版 `AncientDialogueSet.PopulateLocKeys` 之前，自动为已注册的 Mod 角色追加基于本地化定义的 Ancient 对话

键格式与原版保持一致：

| 键组件 | 说明 |
|---|---|
| `<ancientEntry>.talk.<characterEntry>.<dialogueIndex>-<lineIndex>.ancient` | Ancient 台词 |
| `<ancientEntry>.talk.<characterEntry>.<dialogueIndex>-<lineIndex>.char` | 角色台词 |
| 可选后缀 `r` | 重复对话 |
| 可选后缀 `.sfx` | 音效 |
| 可选后缀 `-visit` | 访问覆盖 |
| 可选后缀 `-attack` | Architect 专用攻击者覆盖 |

作者只需编写本地化条目，即可为自定义角色补充 Ancient 对话，无需手动为每个 `AncientDialogueSet` 打补丁。

---

## 推荐分工

| 用途 | 工具 |
|---|---|
| 游戏模型的文本（标题、描述） | 游戏原版 `LocString` 表 |
| Mod 自有辅助文本（设置页、说明） | `I18N` |
| 可复用关键词定义 | `ModKeywordRegistry` |
| Ancient 对话 | 本地化 key + `AncientDialogueLocalization` |

---

## 相关文档

- [内容注册规则](ContentAuthoringToolkit.md)
- [角色与解锁脚手架](CharacterAndUnlockScaffolding.md)
- [诊断与兼容层](DiagnosticsAndCompatibility.md)
- [LocString 占位符解析](LocStringPlaceholderResolution.md)
- [Mod 设置界面](ModSettings.md)
