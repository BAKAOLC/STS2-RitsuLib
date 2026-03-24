# 本地化与关键词

RitsuLib 很明确地把本地化分成两层：

- 游戏自己的 `LocString` 模型键管线
- 框架自带的 `I18N` 辅助本地化

同时，它还提供了一个轻量关键词注册器，用来统一悬浮提示和关键词文本生成。

---

## 游戏模型本地化

对通过 RitsuLib 注册的模型，游戏本身仍然通过正常的表来读取本地化，例如：

- `cards`
- `relics`
- `powers`
- `characters`
- `card_keywords`

这些键仍然建立在固定 `ModelId.Entry` 之上，规则见 [内容注册规则](ContentAuthoringToolkit.md)。

RitsuLib 不会替换这套系统，它只是让模型身份更稳定，从而让这些 Key 更容易编写。

---

## `CreateLocalization` 与 `CreateModLocalization`

如果你需要框架侧的辅助文本本地化，可以使用 `I18N`：

```csharp
var i18n = RitsuLibFramework.CreateModLocalization(
    modId: "MyMod",
    instanceName: "MyMod-I18N",
    resourceFolders: ["MyMod.localization"],
    pckFolders: ["res://MyMod/localization"]);
```

`CreateModLocalization` 是 `CreateLocalization` 的便捷包装。
如果不传文件系统目录，它会默认使用：

```text
user://mod-configs/<modId>/localization
```

---

## 资源合并顺序

`I18N` 支持三类来源：

1. 文件系统目录
2. 嵌入资源
3. PCK 目录

它的合并策略是“先到先得”，不是后来的覆盖前面的：

- 先加载文件系统目录
- 嵌入资源只补缺失键
- PCK 再补剩下还没有的键

这样本地覆写可以自然优先于打包默认值。

---

## 语言代码归一化

`I18N` 在加载 JSON 之前会先规范化语言代码。

例如：

- `en`、`en_us`、`eng` -> `eng`
- `zh`、`zh_cn`、`zh_hans` -> `zhs`
- `ja`、`ja_jp` -> `jpn`

如果无法解析语言，则默认回退到 `eng`。

---

## 运行时重载行为

`I18N` 会在可能的情况下订阅语言切换事件。

这意味着：

- 游戏语言改变时，辅助本地化会自动重载
- 重载完成后会触发 `Changed`
- 如果当前阶段拿不到游戏本地化管理器，则退回为懒检测模式

这套行为与普通 `LocString` 的解析是分开的。

---

## 调试兼容模式

RitsuLib 提供了一个只用于调试的缺失本地化兼容层。

开启 `debug_compatibility_mode` 后：

- `LocTable.GetLocString(...)` 缺失键时不再立刻抛异常
- `LocTable.GetRawText(...)` 缺失键时不再立刻抛异常
- 框架会返回基于键的占位值
- 同时输出一次警告

它的目标是帮助排查问题，不是替代正确的本地化编写。

---

## 关键词注册器

如果你希望关键词有统一定义、统一悬浮提示，可以使用 `ModKeywordRegistry`：

```csharp
var keywords = RitsuLibFramework.GetKeywordRegistry("MyMod");

keywords.RegisterCardKeyword(
    id: "brew",
    locKeyPrefix: "my_mod_brew",
    iconPath: "res://MyMod/ui/keywords/brew.png");
```

它会为关键词生成规范化标识，并绑定标题 / 描述的本地化键。

---

## 在代码里使用关键词

常用辅助方法包括：

- `ModKeywordRegistry.CreateHoverTip(id)`
- `ModKeywordRegistry.GetTitle(id)`
- `ModKeywordRegistry.GetDescription(id)`
- `keywordId.GetModKeywordCardText()`
- `enumerable.ToHoverTips()`

你也可以通过 `ModKeywordExtensions` 把运行时关键词挂在任意对象上：

```csharp
card.AddModKeyword("brew");

if (card.HasModKeyword("brew"))
{
    // ...
}
```

这很适合“关键词存在与否由运行时状态决定”的场景，而不只是静态卡牌文案。

---

## Ancient 对话本地化

RitsuLib 现在内置了 `AncientDialogueLocalization`。

它有两个作用：

- 提供从本地化键扫描对话的辅助 API
- 在 `AncientDialogueSet.PopulateLocKeys` 之前，自动把基于本地化定义的 Mod 角色 Ancient 对话追加进去

键格式与原版保持一致：

- `<ancientEntry>.talk.<characterEntry>.<dialogueIndex>-<lineIndex>.ancient`
- `<ancientEntry>.talk.<characterEntry>.<dialogueIndex>-<lineIndex>.char`
- 可选重复后缀：`r`
- 可选 `.sfx`
- 可选 `-visit`
- Architect 专用可选 `-attack`

也就是说，作者现在只靠写本地化条目，就可以给自定义角色补 Ancient 对话，而不必再手动给每个 `AncientDialogueSet` 打补丁。

---

## 推荐分工

建议按职责使用不同工具：

- 面向游戏模型的文本 -> `LocString` 表
- 框架自有辅助文本 -> `I18N`
- 可复用关键词定义 -> `ModKeywordRegistry`
- Ancient 对话 -> 本地化 Key + `AncientDialogueLocalization`

---

## 相关文档

- [内容注册规则](ContentAuthoringToolkit.md)
- [角色与解锁脚手架](CharacterAndUnlockScaffolding.md)
- [诊断与兼容层](DiagnosticsAndCompatibility.md)
- [LocString 占位符解析](LocStringPlaceholderResolution.md)
