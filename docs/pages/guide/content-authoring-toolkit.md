---
title:
  en: Content Authoring Toolkit
  zh-CN: 内容注册规则
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

This document is the overview for content authoring: registration entry points, model identity, localization coupling, and asset override basics.

Detailed registration mechanics live in [Content Packs & Registries](/guide/content-packs-and-registries). Detailed asset semantics live in [Asset Profiles & Fallbacks](/guide/asset-profiles-and-fallbacks).

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

本文是内容编写的总览文档，聚焦注册入口、模型身份、本地化耦合关系以及资源覆写基础规则。

更详细的注册机制见 [内容包与注册器](/guide/content-packs-and-registries)，更详细的资源语义见 [资源配置与回退规则](/guide/asset-profiles-and-fallbacks)。

---

:::

## Registration APIs{lang="en"}

::: en

| API | Purpose |
|---|---|
| `RitsuLibFramework.CreateContentPack(modId)` | Recommended entry point — fluent builder |
| `RitsuLibFramework.GetContentRegistry(modId)` | Low-level content registry |
| `RitsuLibFramework.GetKeywordRegistry(modId)` | Keyword registry |
| `RitsuLibFramework.GetTimelineRegistry(modId)` | Timeline (story / epoch) registry |
| `RitsuLibFramework.GetUnlockRegistry(modId)` | Unlock rule registry |

`CreateContentPack` wraps all of the above in a fluent builder that executes registered steps in insertion order when `Apply()` is called.

This document keeps the overview short. For builder surface, manifests, fixed-entry ownership, and freeze behavior, see [Content Packs & Registries](/guide/content-packs-and-registries).

---

:::

## 注册接口{lang="zh-CN"}

::: zh-CN

| 接口 | 说明 |
|---|---|
| `RitsuLibFramework.CreateContentPack(modId)` | 推荐入口：流式内容包构建器 |
| `RitsuLibFramework.GetContentRegistry(modId)` | 底层内容注册器 |
| `RitsuLibFramework.GetKeywordRegistry(modId)` | 关键词注册器 |
| `RitsuLibFramework.GetTimelineRegistry(modId)` | Timeline（故事/纪元）注册器 |
| `RitsuLibFramework.GetUnlockRegistry(modId)` | 解锁规则注册器 |

`CreateContentPack` 是推荐用法，将以上注册器封装为流式 API，调用 `Apply()` 时按添加顺序依次执行。

本文只保留总览层内容。关于构建器完整表面、清单式注册、固定条目标识归属和冻结机制，请阅读 [内容包与注册器](/guide/content-packs-and-registries)。

---

:::

## Content Pack Builder{lang="en"}

::: en

All builder methods are chainable. A representative example:

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>()
    .Card<MyCardPool, MyCard>()
    .Relic<MyRelicPool, MyRelic>()
    .CardKeywordOwnedByLocNamespace("my_keyword", iconPath: "res://MyMod/art/kw.png")
    .Story<MyStory>()
    .Epoch<MyEpoch>()
    .RequireEpoch<MyCard, MyEpoch>()
    .Custom(ctx => { /* ... */ })
    .Apply();
```

`Apply()` returns `ModContentPackContext` for further access to individual registries.

---

:::

## 内容包构建器{lang="zh-CN"}

::: zh-CN

所有方法都支持链式调用，下面给出一个代表性示例：

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>()
    .Card<MyCardPool, MyCard>()
    .Relic<MyRelicPool, MyRelic>()
    .CardKeywordOwnedByLocNamespace("my_keyword", iconPath: "res://MyMod/art/kw.png")
    .Story<MyStory>()
    .Epoch<MyEpoch>()
    .RequireEpoch<MyCard, MyEpoch>()
    .Custom(ctx => { /* 任意注册逻辑 */ })
    .Apply();
```

`Apply()` 返回 `ModContentPackContext`，可用于进一步访问各注册器。

---

:::

## Model ID Rule{lang="en"}

::: en

For any model registered through the RitsuLib content registry, `ModelId.Entry` uses:

```
<MODID>_<CATEGORY>_<TYPENAME>
```

All segments are normalized to **UPPER_SNAKE_CASE**.

### Examples (Mod id `MyMod`)

| C# Type | Category | ModelId.Entry |
|---|---|---|
| `MyStrike` | card | `MY_MOD_CARD_MY_STRIKE` |
| `MyStarterRelic` | relic | `MY_MOD_RELIC_MY_STARTER_RELIC` |
| `MyCharacter` | character | `MY_MOD_CHARACTER_MY_CHARACTER` |

> If two types under the same mod id and category share the same CLR name, they resolve to the same entry and must be renamed.

---

:::

## 模型 ID 规则{lang="zh-CN"}

::: zh-CN

通过 RitsuLib 注册的模型，其 `ModelId.Entry` 使用以下固定格式：

```
<MODID>_<CATEGORY>_<TYPENAME>
```

每个字段规范化为**全大写、以下划线分隔**的标识符。

### 示例（Mod id `MyMod`）

| C# 类型 | 类别 | ModelId.Entry |
|---|---|---|
| `MyStrike` | card | `MY_MOD_CARD_MY_STRIKE` |
| `MyStarterRelic` | relic | `MY_MOD_RELIC_MY_STARTER_RELIC` |
| `MyCharacter` | character | `MY_MOD_CHARACTER_MY_CHARACTER` |

> 同一 Mod、同一类别下两个 CLR 类型名相同的模型会产生 Entry 冲突，必须通过重命名解决。

---

:::

## Localization Rule{lang="en"}

::: en

Localization keys are written directly against the fixed `ModelId.Entry`:

```json
{
  "MY_MOD_CARD_MY_STRIKE.title": "My Strike",
  "MY_MOD_CARD_MY_STRIKE.description": "Deal {damage} damage.",
  "MY_MOD_RELIC_MY_STARTER_RELIC.title": "My Starter Relic"
}
```

`RitsuLibFramework.CreateModLocalization(...)` operates independently from the game's `LocString` pipeline.

---

:::

## 本地化规则{lang="zh-CN"}

::: zh-CN

游戏本地化 Key 直接基于固定 `ModelId.Entry` 编写：

```json
{
  "MY_MOD_CARD_MY_STRIKE.title": "我的打击",
  "MY_MOD_CARD_MY_STRIKE.description": "造成 {damage} 点伤害。",
  "MY_MOD_RELIC_MY_STARTER_RELIC.title": "我的起始遗物"
}
```

`RitsuLibFramework.CreateModLocalization(...)` 是独立的本地化工具，与游戏的 `LocString` 模型 Key 管线相互独立。

---

:::

## Asset Override Rule{lang="en"}

::: en

RitsuLib applies template-based asset overrides via interface matching at render time.

### Card Overrides

Inherit `ModCardTemplate` and override via `AssetProfile` (recommended) or individual properties:

```csharp
public class MyCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
    // Unified profile (recommended)
    public override CardAssetProfile AssetProfile => new()
    {
        PortraitPath      = "res://MyMod/art/my_card.png",
        FramePath         = "res://MyMod/art/frame.png",
        FrameMaterialPath = "res://MyMod/art/frame.material",
    };

    // Or override a single property directly
    public override string? CustomPortraitPath => "res://MyMod/art/my_card.png";
}
```

Supported card fields include portrait, frame, portrait border, energy icon, overlay, and banner-related assets.

### Other Content

| Content type | Supported override fields |
|---|---|
| Relic | icon, icon outline, big icon |
| Power | icon, big icon |
| Orb | icon, visuals scene |
| Potion | image, outline |

Override behavior:
1. The model must implement the matching override interface (directly or via `Mod*Template`)
2. The override member must return a non-empty path
3. If the resource path does not exist, RitsuLib emits a one-time warning and falls back to the base asset

This warning behavior is especially important for character assets because the base game has almost no safe fallback for missing paths.

For the full profile records, helper factories, placeholder behavior, and diagnostics policy, see [Asset Profiles & Fallbacks](/guide/asset-profiles-and-fallbacks).

---

:::

## 资源覆写规则{lang="zh-CN"}

::: zh-CN

RitsuLib 通过接口匹配，在渲染时将默认资源替换为 Mod 提供的资源。

### 卡牌资源覆写

继承 `ModCardTemplate` 后，通过 `AssetProfile`（推荐）或单独属性覆写：

```csharp
public class MyCard : ModCardTemplate(1, CardType.Attack, CardRarity.Common, TargetType.SingleEnemy)
{
    // 统一通过 AssetProfile 配置（推荐）
    public override CardAssetProfile AssetProfile => new()
    {
        PortraitPath      = "res://MyMod/art/my_card.png",
        FramePath         = "res://MyMod/art/frame.png",
        FrameMaterialPath = "res://MyMod/art/frame.material",
    };

    // 或单独覆写某一项
    public override string? CustomPortraitPath => "res://MyMod/art/my_card.png";
}
```

卡牌支持的覆写大致包括 portrait、frame、portrait border、energy icon、overlay 与 banner 相关资源。

### 其他内容资源覆写

| 内容类型 | 支持字段 |
|---|---|
| Relic | icon、icon outline、big icon |
| Power | icon、big icon |
| Orb | 图标、视觉场景 |
| Potion | image、outline |

覆写行为如下：
1. 模型必须实现对应的 override 接口（直接或通过 `Mod*Template`）
2. override 成员必须返回非空路径
3. 如果资源路径不存在，RitsuLib 会输出一次警告，并回退到原始资源

这点对角色资源尤其重要，因为原版游戏对缺失角色资源几乎没有安全兜底。

完整资源配置结构、路径工厂辅助方法、占位角色规则与诊断策略见 [资源配置与回退规则](/guide/asset-profiles-and-fallbacks)。

---

:::

## Registration Timing{lang="en"}

::: en

All content registration must be completed before the framework freezes content registration (during early game boot). Additional registration after the freeze is invalid and may throw.

The freeze is signaled by `ContentRegistrationClosedEvent`.

---

:::

## 注册时机{lang="zh-CN"}

::: zh-CN

所有内容注册必须在框架冻结内容注册之前完成（游戏早期引导阶段）。冻结后继续注册属于无效操作并可能抛出异常。

冻结时触发的事件：`ContentRegistrationClosedEvent`

---

:::

## Compatibility{lang="en"}

::: en

The fixed-entry rule applies only to model types explicitly registered through the RitsuLib content registry, at `ModelDb.GetEntry(Type)`. Models not registered through RitsuLib are unaffected.

---

:::

## 兼容规则{lang="zh-CN"}

::: zh-CN

固定 Entry 规则**只作用于**通过 RitsuLib 内容注册器显式注册的模型类型，处理点为 `ModelDb.GetEntry(Type)`。未经 RitsuLib 注册的模型不受影响。

---

:::

## Related Documents{lang="en"}

::: en

- [Getting Started](/guide/getting-started)
- [Content Packs & Registries](/guide/content-packs-and-registries)
- [Character & Unlock Templates](/guide/character-and-unlock-scaffolding)
- [Custom Events](/guide/custom-events)
- [Card Dynamic Variables](/guide/card-dynamic-var-toolkit)
- [Localization & Keywords](/guide/localization-and-keywords)
- [Framework Design](/guide/framework-design)
- [Asset Profiles & Fallbacks](/guide/asset-profiles-and-fallbacks)

:::

## 相关文档{lang="zh-CN"}

::: zh-CN

- [快速入门](/guide/getting-started)
- [内容包与注册器](/guide/content-packs-and-registries)
- [角色与解锁模板](/guide/character-and-unlock-scaffolding)
- [自定义事件](/guide/custom-events)
- [卡牌动态变量](/guide/card-dynamic-var-toolkit)
- [本地化与关键词](/guide/localization-and-keywords)
- [框架设计](/guide/framework-design)
- [资源配置与回退规则](/guide/asset-profiles-and-fallbacks)

:::
