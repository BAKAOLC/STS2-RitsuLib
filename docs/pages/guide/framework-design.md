---
title:
  en: Framework Design
  zh-CN: 框架设计
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

This document explains the architectural decisions behind RitsuLib and the constraints those decisions impose on mod code.

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

本文说明 RitsuLib 的核心架构决策，以及这些决策对 Mod 实现方式的影响。

---

:::

## Core Goals{lang="en"}

::: en

RitsuLib is built around a small set of explicit design priorities:

- explicit registration instead of opaque “magic” discovery
- fixed model identity instead of runtime name inference
- composable asset records instead of large inheritance hierarchies
- scene replacement instead of in-place mutation of vanilla assets
- compatibility fallbacks only where the base game has no safe extension point

The framework reduces repetitive authoring work, but it does not convert the mod into an implicit runtime graph.

Optional **attribute-based** registration is still explicit: only types in assemblies registered with `ModTypeDiscoveryHub.RegisterModAssembly` are considered, each attribute maps to ordinary registry calls, and `AutoRegistrationAttribute.Inherit` defaults to **off** so derived types do not pick up base annotations unless you opt in. Details: [Content Packs & Registries](ContentPacksAndRegistries.md#attribute-based-registration-optional).

---

:::

## 核心目标{lang="zh-CN"}

::: zh-CN

RitsuLib 以少量明确的设计原则为核心：

- 使用显式注册，而非不透明、无约束的「魔法」式发现
- 使用固定模型身份，而非运行时推断名称
- 使用可组合的资源记录，而非大型继承层级
- 使用场景替换，而非原版资源原地修改
- 仅在原版缺少安全扩展点时引入兼容回退

框架会减少重复性工作，但不会把 Mod 运行时结构隐藏为不可见行为。

可选的 **CLR 特性**注册仍然是显式的：只有通过 `ModTypeDiscoveryHub.RegisterModAssembly` 登记的程序集才会参与扫描；每条特性最终仍对应与普通代码相同的注册器调用；`AutoRegistrationAttribute.Inherit` 默认为 **关闭**，避免子类在未声明的情况下继承基类上的特性。说明见 [内容包与注册器](ContentPacksAndRegistries.md#clr-特性注册可选)。

---

:::

## Fixed Identity{lang="en"}

::: en

For models registered through the RitsuLib content registry, `ModelId.Entry` is deterministic:

```text
<MODID>_<CATEGORY>_<TYPENAME>
```

Why this matters:

- localization keys stay stable and predictable
- refactors are easier to reason about
- content registration conflicts are easier to detect
- migration between project structures does not depend on reflection order or class discovery behavior

The tradeoff is deliberate: renaming a published CLR type becomes a compatibility change.

---

:::

## 固定模型身份{lang="zh-CN"}

::: zh-CN

对通过 RitsuLib 内容注册器注册的模型，`ModelId.Entry` 是确定性的：

```text
<MODID>_<CATEGORY>_<TYPENAME>
```

这样做的好处：

- 本地化 Key 稳定且可预测
- 重构时更容易判断影响面
- 内容冲突更容易定位
- 不依赖反射顺序、自动扫描细节或类发现时机

这一取舍是明确的：已发布的 CLR 类型一旦改名，就属于兼容性变更。

---

:::

## Registration Before Use{lang="en"}

::: en

RitsuLib relies on explicit registration during early boot.

`CreateContentPack(modId)` is the convenience entry point, but the underlying registries remain first-class.

Registration is frozen during early boot to preserve:

- stable model identity
- stable model lists
- deterministic lookup and unlock behavior

The framework therefore fails fast instead of mutating the model graph after runtime systems have started consuming it.

See [Content Packs & Registries](/guide/content-packs-and-registries) for the concrete registration model.

---

:::

## 先注册，再使用{lang="zh-CN"}

::: zh-CN

RitsuLib 要求在早期引导阶段完成显式注册。

`CreateContentPack(modId)` 是便捷入口，但底层注册器仍然是第一层概念。

框架在早期引导阶段冻结注册，以保证：

- 稳定的模型身份
- 稳定的模型列表
- 可预测的查找与解锁行为

因此，框架选择尽早失败，而不是在运行时系统已开始消费模型后继续修改模型图。

具体注册模型可见 [内容包与注册器](/guide/content-packs-and-registries)。

---

:::

## Asset Profiles Instead Of Large Character Bases{lang="en"}

::: en

Character authoring is organized around asset profiles.

Instead of requiring a monolithic custom-character base type with unrelated virtual members, RitsuLib groups assets into records such as:

- `CharacterSceneAssetSet`
- `CharacterUiAssetSet`
- `CharacterVfxAssetSet`
- `CharacterAudioAssetSet`

This keeps responsibility boundaries explicit:

- scenes live together
- UI assets live together
- VFX tuning lives together
- audio overrides live together

This is more verbose than a single placeholder property, but it scales better because each asset category can evolve independently.

---

:::

## 资源配置，而不是大型角色基类{lang="zh-CN"}

::: zh-CN

角色内容编写围绕结构化资源配置展开。

RitsuLib 不要求把所有角色资源塞进一个单体基类，而是按职责分组：

- `CharacterSceneAssetSet`
- `CharacterUiAssetSet`
- `CharacterVfxAssetSet`
- `CharacterAudioAssetSet`

这样可以保持职责边界清晰：

- 场景资源放一起
- UI 放一起
- VFX 调整放一起
- 音效放一起

这种方式确实比单一占位属性更冗长，但更利于独立扩展各类资源能力。

---

:::

## Asset Safety Mechanisms{lang="en"}

::: en

The asset-profile system is paired with a small set of safety mechanisms:

- character placeholder fallback for missing character resources
- separate APIs for full energy-counter scenes versus pool-linked icons
- one-time warnings when explicit resource paths are missing

These behaviors are part of the same design: a structured asset API must remain usable during migration and partial-content development.

See [Asset Profiles & Fallbacks](/guide/asset-profiles-and-fallbacks) for the detailed behavior and API surface.

---

:::

## 资源安全机制{lang="zh-CN"}

::: zh-CN

资源配置体系配套了一组小范围的安全机制：

- 角色缺失资源时的占位角色回退
- 完整能量球场景与池级图标的分层 API
- 显式资源路径不存在时的一次性警告

这些行为属于同一设计目标的一部分，用于保证结构化资源 API 在迁移和未完成内容阶段仍然可用。

具体行为与 API 细节见 [资源配置与回退规则](/guide/asset-profiles-and-fallbacks)。

---

:::

## Compatibility Layers Stay Narrow{lang="en"}

::: en

RitsuLib includes compatibility-oriented patches, but they are intentionally narrow.

The framework does not hide every engine limitation behind automation. It adds fallbacks only where the game or modding surface would otherwise be unsafe or excessively repetitive.

Examples include `LocTable` and `THE_ARCHITECT` fallbacks under `debug_compatibility_mode`, ancient dialogue key injection, and unlock bridge patches for vanilla progression checks that skip mod characters.

See [Diagnostics & Compatibility](/guide/diagnostics-and-compatibility) for the concrete compatibility layers.

---

:::

## 兼容层保持收敛{lang="zh-CN"}

::: zh-CN

RitsuLib 提供兼容型补丁，但范围刻意保持收敛。

框架不会用自动化去覆盖所有引擎限制。只有在原版扩展点不安全，或重复劳动明显过高时，才会加入兼容回退。

典型例子包括：`debug_compatibility_mode` 下的 `LocTable` 与 `THE_ARCHITECT` 回退、Ancient 对话键注入，以及原版进度检查跳过 Mod 角色时使用的解锁桥接补丁。

具体兼容层可见 [诊断与兼容层](/guide/diagnostics-and-compatibility)。

---

:::

## Why The Patching Layer Exists{lang="en"}

::: en

Harmony is still the underlying patch engine, but RitsuLib wraps it with:

- typed patch declarations via `IPatchMethod`
- critical vs optional patch semantics
- ignore-if-missing targets
- grouped registration helpers
- dynamic patch application support

The goal is not to abstract Harmony away. The goal is to standardize patch declaration and failure handling so large mods remain maintainable.

See [Patching Guide](/guide/patching-guide) for the patching workflow.

---

:::

## 为什么要有自己的补丁层{lang="zh-CN"}

::: zh-CN

底层仍然是 Harmony，但 RitsuLib 在其上增加了一层统一约定：

- 用 `IPatchMethod` 声明补丁
- 区分 critical / optional
- 支持忽略缺失目标
- 支持分组注册
- 支持动态补丁

目的不是隐藏 Harmony，而是统一补丁声明、失败处理与日志行为，降低大型 Mod 的维护成本。

具体流程见 [补丁系统](/guide/patching-guide)。

---

:::

## Why Persistence Is Class-Based{lang="en"}

::: en

Persistent entries are registered as class types rather than loose primitives.

That choice enables:

- schema version fields
- structured migrations
- future expansion without breaking call sites
- safer serialization boundaries

This adds some upfront structure, but avoids primitive save keys that later need to carry schema growth.

See [Persistence Guide](/guide/persistence-guide) for the full data model.

---

:::

## 为什么持久化按类组织{lang="zh-CN"}

::: zh-CN

RitsuLib 的持久化条目是按类注册的，而不是随手塞原始值。

这样做可以自然支持：

- 数据版本字段
- 数据迁移
- 后续扩展字段
- 更清晰的序列化边界

前期会增加少量样板，但可以避免原始值存档在后期演化为复杂结构时的维护问题。

完整数据设计见 [持久化设计](/guide/persistence-guide)。

---

:::

## Recommended Reading Order{lang="en"}

::: en

- [Getting Started](/guide/getting-started)
- [Content Authoring Toolkit](/guide/content-authoring-toolkit)
- [Content Packs & Registries](/guide/content-packs-and-registries)
- [Character & Unlock Templates](/guide/character-and-unlock-scaffolding)
- [Timeline & Unlocks](/guide/timeline-and-unlocks)
- [Asset Profiles & Fallbacks](/guide/asset-profiles-and-fallbacks)
- [Patching Guide](/guide/patching-guide)
- [Persistence Guide](/guide/persistence-guide)
- [Localization & Keywords](/guide/localization-and-keywords)
- [Diagnostics & Compatibility](/guide/diagnostics-and-compatibility)

:::

## 推荐阅读顺序{lang="zh-CN"}

::: zh-CN

- [快速入门](/guide/getting-started)
- [内容注册规则](/guide/content-authoring-toolkit)
- [内容包与注册器](/guide/content-packs-and-registries)
- [角色与解锁模板](/guide/character-and-unlock-scaffolding)
- [时间线与解锁](/guide/timeline-and-unlocks)
- [资源配置与回退规则](/guide/asset-profiles-and-fallbacks)
- [补丁系统](/guide/patching-guide)
- [持久化设计](/guide/persistence-guide)
- [本地化与关键词](/guide/localization-and-keywords)
- [诊断与兼容层](/guide/diagnostics-and-compatibility)

:::
