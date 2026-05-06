---
title:
  en: Localization & Keywords
  zh-CN: 本地化与关键词
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

RitsuLib separates localization into two distinct layers:

- **The base game's `LocString` model-key pipeline** — in-game text such as model titles and descriptions
- **Framework-provided `I18N` helper localization** — auxiliary text for the mod itself

It also provides a lightweight keyword registry to unify hover tips and keyword text.

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

RitsuLib 将本地化明确分为两层：

- **游戏原版的 `LocString` 模型键管线** — 模型标题、描述等游戏内文本
- **框架自带的 `I18N` 辅助本地化** — Mod 自身的辅助文本

同时提供轻量关键词注册器，用来统一悬浮提示和关键词文本。

---

:::

## Game Model Localization{lang="en"}

::: en

> The following describes the game engine's own localization mechanism; RitsuLib does not replace this system.

The game reads model text through `LocString` and various localization tables, commonly including:

- `cards`
- `relics`
- `powers`
- `characters`
- `card_keywords`

Those keys are built on `ModelId.Entry`.

RitsuLib's role is limited to making model identity more stable and predictable so keys are easier to author. For concrete model ID rules, see [Content Authoring Toolkit](/guide/content-authoring-toolkit).

---

:::

## 游戏原版模型本地化{lang="zh-CN"}

::: zh-CN

> 以下描述游戏引擎自身的本地化机制，RitsuLib 不替换此系统。

游戏通过 `LocString` 和各本地化表来读取模型文本，常见表包括：

- `cards`、`relics`、`powers`、`characters`、`card_keywords`

这些键建立在 `ModelId.Entry` 之上。

RitsuLib 的作用仅限于让模型身份更稳定、更可预测，从而使键更容易编写。具体的模型 ID 规则见 [内容注册规则](/guide/content-authoring-toolkit)。

---

:::

## `CreateLocalization` And `CreateModLocalization`{lang="en"}

::: en

`I18N` is RitsuLib's helper-text localization system, independent of the game's `LocString`:

```csharp
var i18n = RitsuLibFramework.CreateModLocalization(
    modId: "MyMod",
    instanceName: "MyMod-I18N",
    resourceFolders: ["MyMod.localization"],
    pckFolders: ["res://MyMod/localization"]);
```

`CreateModLocalization` is a convenience wrapper over `CreateLocalization`.
If you do not provide file-system folders, it defaults to:

```text
user://mod-configs/<modId>/localization
```

---

:::

## `CreateLocalization` 与 `CreateModLocalization`{lang="zh-CN"}

::: zh-CN

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

:::

## Source Merge Order{lang="en"}

::: en

`I18N` can merge translations from three source kinds:

1. file system folders
2. embedded resources
3. PCK folders

Merge behavior is first-wins:

- file-system entries are loaded first
- embedded entries only fill missing keys
- PCK entries only fill keys still missing after that

This lets local overrides take priority over packaged defaults.

---

:::

## 资源合并顺序{lang="zh-CN"}

::: zh-CN

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

:::

## Language Normalization{lang="en"}

::: en

`I18N` normalizes locale names before loading JSON files:

| Input | Normalized |
|---|---|
| `en`, `en_us`, `eng` | `eng` |
| `zh`, `zh_cn`, `zh_hans` | `zhs` |
| `ja`, `ja_jp` | `jpn` |

If no language can be resolved, it falls back to `eng`.

---

:::

## 语言代码归一化{lang="zh-CN"}

::: zh-CN

`I18N` 在加载 JSON 之前会规范化语言代码：

| 输入 | 归一化结果 |
|---|---|
| `en`、`en_us`、`eng` | `eng` |
| `zh`、`zh_cn`、`zh_hans` | `zhs` |
| `ja`、`ja_jp` | `jpn` |

无法解析的语言默认回退到 `eng`。

---

:::

## Runtime Reload Behavior{lang="en"}

::: en

`I18N` subscribes to locale changes when possible:

- when the game language changes, helper localization reloads automatically
- `Changed` is raised after reload completes
- if the game localization manager is unavailable at that moment, `I18N` falls back to lazy detection

This behavior is independent of base-game `LocString` resolution.

---

:::

## 运行时重载行为{lang="zh-CN"}

::: zh-CN

`I18N` 会在可能的情况下订阅语言切换事件：

- 游戏语言改变时，辅助本地化自动重载
- 重载完成后触发 `Changed` 事件
- 如果当前阶段拿不到游戏本地化管理器，则退回懒检测模式

此行为与游戏原版 `LocString` 的解析相互独立。

---

:::

## Debug Compatibility Mode{lang="en"}

::: en

`LocTable` placeholder resolution is part of RitsuLib’s debug compatibility fallbacks. See [Diagnostics & Compatibility](/guide/diagnostics-and-compatibility) for the master toggle, the **LocTable missing keys** toggle, and one-time `[Localization][DebugCompat]` warnings.

Use this for troubleshooting, not as a substitute for authoring real keys.

---

:::

## 调试兼容模式{lang="zh-CN"}

::: zh-CN

`LocTable` 占位值解析属于 RitsuLib 调试兼容回退之一：总开关、**LocTable** 子项与一次性 `[Localization][DebugCompat]` 警告见 [诊断与兼容层](/guide/diagnostics-and-compatibility)。

用于排障，不能代替补全真实键。

---

:::

## Keyword Registry{lang="en"}

::: en

Use `ModKeywordRegistry` when you want reusable keyword definitions and hover tips:

```csharp
var keywords = RitsuLibFramework.GetKeywordRegistry("MyMod");

keywords.RegisterCardKeywordOwnedByLocNamespace(
    localKeywordStem: "brew",
    iconPath: "res://MyMod/ui/keywords/brew.png");
```

This creates a normalized keyword id and binds it to title / description localization keys.

---

:::

## 关键词注册器{lang="zh-CN"}

::: zh-CN

`ModKeywordRegistry` 用于统一定义关键词及其悬浮提示：

```csharp
var keywords = RitsuLibFramework.GetKeywordRegistry("MyMod");

keywords.RegisterCardKeywordOwnedByLocNamespace(
    localKeywordStem: "brew",
    iconPath: "res://MyMod/ui/keywords/brew.png");
```

注册后会生成规范化标识，并绑定标题/描述的本地化键。

---

:::

## Automatic keyword registration (optional: CLR attributes){lang="en"}

::: en

If you already use `ModTypeDiscoveryHub.RegisterModAssembly(...)` to let RitsuLib scan your assemblies, you can declare keyword registration with CLR attributes:

```csharp
using STS2RitsuLib.Interop.AutoRegistration;

[RegisterOwnedCardKeyword("brew", LocNamespace = "my_mod", IconPath = "res://MyMod/ui/keywords/brew.png")]
public sealed class BrewKeywordMarker;
```

`LocNamespace` only affects the localization namespace (the `modid` portion). The keyword stem (`brew`) participates in the default rule `<namespace>_<keyword>`, producing:

- `<namespace>_<keyword>.title`
- `<namespace>_<keyword>.description`

> Compatibility note: the legacy `LocKeyPrefix` / `locKeyPrefix` historically represents the **full stem** and is easy to misread as a prefix + keyword composition, so it is now obsolete. Use `LocNamespace` for new code.

---

:::

## 自动注册关键词（可选：CLR 特性）{lang="zh-CN"}

::: zh-CN

如果你已经使用 `ModTypeDiscoveryHub.RegisterModAssembly(...)` 让 RitsuLib 扫描你的程序集，也可以用特性声明关键词注册：

```csharp
using STS2RitsuLib.Interop.AutoRegistration;

[RegisterOwnedCardKeyword("brew", LocNamespace = "my_mod", IconPath = "res://MyMod/ui/keywords/brew.png")]
public sealed class BrewKeywordMarker;
```

这里 `LocNamespace` 只影响本地化键的 namespace（即 `modid` 部分）。关键词 stem（`brew`）会自动参与默认生成规则：`<namespace>_<keyword>`，并形成：

- `<namespace>_<keyword>.title`
- `<namespace>_<keyword>.description`

> 兼容性说明：旧字段 `LocKeyPrefix`/`locKeyPrefix` 历史上实际代表“完整 stem”，容易误解为 prefix + keyword，已标记为过时；新代码请使用 `LocNamespace`。

---

:::

## Using Keywords In Code{lang="en"}

::: en

Common helpers:

| Method | Description |
|---|---|
| `ModKeywordRegistry.CreateHoverTip(id)` | Create hover tip |
| `ModKeywordRegistry.GetTitle(id)` | Get title |
| `ModKeywordRegistry.GetDescription(id)` | Get description |
| `keywordId.GetModKeywordCardText()` | Get card text |
| `enumerable.ToHoverTips()` | Batch-convert to hover tips |

You can also attach runtime keywords to arbitrary objects via `ModKeywordExtensions`:

```csharp
card.AddModKeyword("brew");

if (card.HasModKeyword("brew"))
{
    // ...
}
```

This is useful when keyword presence is driven by runtime state rather than static card text.

---

:::

## 在代码里使用关键词{lang="zh-CN"}

::: zh-CN

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

:::

## Ancient Dialogue Localization{lang="en"}

::: en

RitsuLib includes `AncientDialogueLocalization`. It serves two roles:

- helper API for scanning dialogue from localization keys
- automatic append of localization-defined mod-character ancient dialogues before `AncientDialogueSet.PopulateLocKeys` runs

The key format matches the base game:

| Key component | Description |
|---|---|
| `<ancientEntry>.talk.<characterEntry>.<dialogueIndex>-<lineIndex>.ancient` | Ancient line |
| `<ancientEntry>.talk.<characterEntry>.<dialogueIndex>-<lineIndex>.char` | Character line |
| Optional suffix `r` | Repeated dialogue |
| Optional suffix `.sfx` | Sound effect |
| Optional suffix `-visit` | Visit override |
| Optional suffix `-attack` | Architect-only attacker override |

Authors only need to write localization entries to add ancient dialogue for custom characters, without manually patching each `AncientDialogueSet`.

If **no** keys exist for an ancient, vanilla may still show `PROCEED` for `THE_ARCHITECT` while `WinRun` assumes `Dialogue` is non-null. RitsuLib adds a narrow compatibility fallback (empty `Lines`, safe attackers) for `ModContentRegistry` characters **only** when the debug compatibility master toggle and the **THE_ARCHITECT missing dialogue** toggle are enabled, with a one-time `[Ancient]` warning.

---

:::

## Ancient 对话本地化{lang="zh-CN"}

::: zh-CN

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

作者只需编写本地化条目，即可为自定义角色补充 Ancient 对话，无需手动为每个 `AncientDialogueSet` 添加补丁。

若某个 Ancient **完全没有**对应键，原版仍可能在 `THE_ARCHITECT` 显示 `PROCEED`，但 `WinRun` 会假定 `Dialogue` 非空。RitsuLib 仅在调试**总开关 + 建筑师子项**开启时，对 `ModContentRegistry` 角色注入窄范围兼容回退（空 `Lines`、安全的攻击方枚举），并记录一次 `[Ancient]` 警告。

---

:::

## Recommended Split{lang="en"}

::: en

| Use case | Tool |
|---|---|
| Game model text (titles, descriptions) | Base game `LocString` tables |
| Mod-owned auxiliary text (settings, explanations) | `I18N` |
| Reusable keyword definitions | `ModKeywordRegistry` |
| Ancient dialogue | Localization keys + `AncientDialogueLocalization` |

---

:::

## 推荐分工{lang="zh-CN"}

::: zh-CN

| 用途 | 工具 |
|---|---|
| 游戏模型的文本（标题、描述） | 游戏原版 `LocString` 表 |
| Mod 自有辅助文本（设置页、说明） | `I18N` |
| 可复用关键词定义 | `ModKeywordRegistry` |
| Ancient 对话 | 本地化键 + `AncientDialogueLocalization` |

---

:::

## Related Documents{lang="en"}

::: en

- [Content Authoring Toolkit](/guide/content-authoring-toolkit)
- [Character & Unlock Templates](/guide/character-and-unlock-scaffolding)
- [Diagnostics & Compatibility](/guide/diagnostics-and-compatibility)
- [LocString Placeholder Resolution](/guide/loc-string-placeholder-resolution)
- [Mod Settings UI](/guide/mod-settings)

:::

## 相关文档{lang="zh-CN"}

::: zh-CN

- [内容注册规则](/guide/content-authoring-toolkit)
- [角色与解锁模板](/guide/character-and-unlock-scaffolding)
- [诊断与兼容层](/guide/diagnostics-and-compatibility)
- [LocString 占位符解析](/guide/loc-string-placeholder-resolution)
- [Mod 设置界面](/guide/mod-settings)

:::
