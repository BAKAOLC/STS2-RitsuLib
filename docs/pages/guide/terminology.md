---
title:
  en: Terminology
  zh-CN: 术语
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Core Terms{lang="en"}

::: en

| Term | Meaning |
| --- | --- |
| Mod id | The manifest id of the owning mod, for example `MyMod`. It is used when RitsuLib builds stable ids. |
| Model | A game `AbstractModel` subtype: card, relic, potion, character, event, act, power, orb, and similar content. |
| Pool | A game pool model such as `CardPoolModel`, `RelicPoolModel`, or `PotionPoolModel`. Register cards, relics, and potions into a pool. |
| Content pack | A fluent batch created by `RitsuLibFramework.CreateContentPack(modId)`. It is the preferred registration entry point. |
| Registry | A per-mod API surface such as `ModContentRegistry`, `ModKeywordRegistry`, or `ModUnlockRegistry`. Use registries directly only when the builder does not fit. |
| Public entry | The stable `ModelId.Entry` string generated for RitsuLib-owned models. It is also the stem for most model localization keys. |
| Owned id | An id qualified with the mod id, for example `MY_MOD_KEYWORD_BURNING`. Prefer owned ids over flat global ids. |
| Lifecycle event | A typed event published through `RitsuLibFramework.SubscribeLifecycle<TEvent>(...)`. |
| Replayable event | A lifecycle event sent immediately to late subscribers when the event has already happened. |
| Scope | Persistence location: `Global`, `Profile`, `RunSidecar`, or `InMemory`. |

:::

## 核心术语{lang="zh-CN"}

::: zh-CN

| 术语 | 含义 |
| --- | --- |
| Mod id | Mod 清单中的 id，例如 `MyMod`。RitsuLib 用它生成稳定 ID。 |
| Model | 游戏里的 `AbstractModel` 子类：卡牌、遗物、药水、角色、事件、Act、能力、Orb 等。 |
| Pool | 游戏池模型，例如 `CardPoolModel`、`RelicPoolModel`、`PotionPoolModel`。卡牌、遗物、药水需要注册到池。 |
| Content pack | 由 `RitsuLibFramework.CreateContentPack(modId)` 创建的链式注册批次。推荐优先使用。 |
| Registry | 每个 Mod 独立的注册器，例如 `ModContentRegistry`、`ModKeywordRegistry`、`ModUnlockRegistry`。只有 builder 不合适时才直接使用。 |
| Public entry | RitsuLib 为自有模型生成的稳定 `ModelId.Entry`。大多数模型本地化 key 也以它为 stem。 |
| Owned id | 带 Mod 归属的 ID，例如 `MY_MOD_KEYWORD_BURNING`。优先使用 owned id，避免扁平全局 id 冲突。 |
| Lifecycle event | 通过 `RitsuLibFramework.SubscribeLifecycle<TEvent>(...)` 发布的强类型事件。 |
| Replayable event | 已经发生后仍会立即补发给新订阅者的生命周期事件。 |
| Scope | 持久化位置：`Global`、`Profile`、`RunSidecar` 或 `InMemory`。 |

:::

## Naming Rules{lang="en"}

::: en

RitsuLib normalizes public stems to uppercase snake case. Non-alphanumeric separators collapse to `_`, and camel-case names are split.

| Input | Normalized |
| --- | --- |
| `MyMod` | `MY_MOD` |
| `com.example.my-mod` | `COM_EXAMPLE_MY_MOD` |
| `StarterRelic` | `STARTER_RELIC` |

Use readable PascalCase for CLR model type names. Avoid all-uppercase names such as `TESTCARD`: the vanilla game regex can
split that name as `T_ES_TC_AR_D` in paths that do not use RitsuLib's fixed-entry override. Prefer `TestCard`, and prefer
`UrlParser` over `URLParser` when an acronym is part of the name.

Default model entry:

```text
<MODID>_<CATEGORY>_<TYPENAME>
```

Owned keyword, card tag, card pile, and top-bar button ids use the same pattern with a fixed middle segment such as `KEYWORD`, `CARDTAG`, `CARDPILE`, or `TOPBARBUTTON`.

:::

## 命名规则{lang="zh-CN"}

::: zh-CN

RitsuLib 会把公开 stem 规范化为全大写下划线格式。非字母数字分隔符合并成 `_`，驼峰名称会被拆开。

| 输入 | 规范化后 |
| --- | --- |
| `MyMod` | `MY_MOD` |
| `com.example.my-mod` | `COM_EXAMPLE_MY_MOD` |
| `StarterRelic` | `STARTER_RELIC` |

CLR 模型类型名请使用可读 PascalCase。避免 `TESTCARD` 这类全大写名称：未走 RitsuLib 固定 Entry 覆写的原版路径可能会把它拆成
`T_ES_TC_AR_D`。请写 `TestCard`；名称中有缩写时，也优先写 `UrlParser` 而不是 `URLParser`。

默认模型 Entry：

```text
<MODID>_<CATEGORY>_<TYPENAME>
```

关键词、卡牌标签、卡堆和顶栏按钮的 owned id 使用同一规则，中间段固定为 `KEYWORD`、`CARDTAG`、`CARDPILE` 或 `TOPBARBUTTON`。

:::
