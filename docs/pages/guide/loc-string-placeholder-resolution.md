---
title:
  en: LocString Placeholders
  zh-CN: LocString 占位符
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Use The Game Pipeline When You Can{lang="en"}

::: en

Model titles, descriptions, event options, hover tips, and most game UI text should use the game's `LocString` pipeline. RitsuLib's `I18N` is useful for mod UI and custom text, but content that the base game already localizes should stay in the matching game table.

```json
{
  "MY_MOD_CARD_MY_STRIKE.title": "Measured Strike",
  "MY_MOD_CARD_MY_STRIKE.description": "Deal {damage} damage."
}
```

For values that come from `DynamicVars`, keep the placeholder name in the description and expose the variable from the card.

:::

## 优先使用游戏管线{lang="zh-CN"}

::: zh-CN

模型标题、描述、事件选项、hover tip 和大多数游戏 UI 文本应使用游戏原生 `LocString` 管线。RitsuLib 的 `I18N` 适合 Mod UI 和自定义文本；已经有原生本地化表的内容，应留在对应游戏表里。

```json
{
  "MY_MOD_CARD_MY_STRIKE.title": "精准打击",
  "MY_MOD_CARD_MY_STRIKE.description": "造成 {damage} 点伤害。"
}
```

来自 `DynamicVars` 的值，在描述中保留占位符名称，并从卡牌暴露对应变量。

:::

## Placeholder Sources{lang="en"}

::: en

Common placeholder sources:

| Placeholder kind | Where to define it |
| --- | --- |
| Card numeric value | `DynamicVarSet` on the card |
| Keyword title / description | `card_keywords` or `static_hover_tips` key |
| Event option text | Event or ancient localization entry built from `InitialOptionKey(...)` / `ModOptionKey(...)` |
| Settings UI text | `ModSettingsText.I18N(...)`, `ModSettingsText.LocString(...)`, or literal text |
| Custom formatter | `RitsuLibFramework.GetSmartFormatRegistry(modId)` or `.SmartFormatter<T>()` in a content pack |

Keep placeholder names lowercase and descriptive in author-facing JSON. Avoid encoding implementation names into user text.

:::

## 占位符来源{lang="zh-CN"}

::: zh-CN

常见占位符来源：

| 占位符类型 | 定义位置 |
| --- | --- |
| 卡牌数值 | 卡牌上的 `DynamicVarSet` |
| 关键词标题 / 描述 | `card_keywords` 或 `static_hover_tips` key |
| 事件选项文本 | 由 `InitialOptionKey(...)` / `ModOptionKey(...)` 生成的事件或 ancient 本地化 entry |
| 设置 UI 文本 | `ModSettingsText.I18N(...)`、`ModSettingsText.LocString(...)` 或 literal text |
| 自定义 formatter | `RitsuLibFramework.GetSmartFormatRegistry(modId)` 或 content pack 的 `.SmartFormatter<T>()` |

面向作者的 JSON 里，占位符名称尽量小写且语义清晰。不要把实现类名塞进用户文本。

:::

## SmartFormat Extensions{lang="en"}

::: en

Register SmartFormat extensions when a placeholder needs custom formatting behavior.

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SmartFormatter<MyFormatter>()
    .SmartFormatSource<MySource>()
    .Apply();
```

Use this for reusable formatting rules. For a single card value, a dynamic variable is simpler and easier for other authors to read.

:::

## SmartFormat 扩展{lang="zh-CN"}

::: zh-CN

占位符需要自定义格式化行为时，注册 SmartFormat 扩展。

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SmartFormatter<MyFormatter>()
    .SmartFormatSource<MySource>()
    .Apply();
```

可复用格式化规则适合走这条路。单张卡牌的数值直接用 dynamic variable 更简单，也更容易让其他作者理解。

:::

## Checklist{lang="en"}

::: en

- Keep localization keys based on stable public entries.
- Keep user-facing text free of class names, method names, and patch names.
- Add fallbacks for mod settings text.
- Test `eng` and `zhs` at minimum when the mod ships both.
- Register keywords and SmartFormat extensions before model text is first resolved.

:::

## 检查清单{lang="zh-CN"}

::: zh-CN

- 本地化 key 基于稳定公开 Entry。
- 用户可见文本里不要出现类名、方法名和 patch 名。
- Mod 设置文本提供 fallback。
- 同时发布英文和中文时，至少测试 `eng` 与 `zhs`。
- 关键词和 SmartFormat 扩展应在模型文本首次解析前注册。

:::
