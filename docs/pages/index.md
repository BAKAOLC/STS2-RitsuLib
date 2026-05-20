---
title: RitsuLib
---

<template v-if="$i18n.locale.startsWith('zh')">

![Slay the Spire 2](https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp)

## 《杀戮尖塔 2》Mod 编写工具集

RitsuLib 是一个面向《杀戮尖塔 2》的 Mod 开发框架，目标是把“注册内容、接入游戏流程、保存状态、暴露设置、处理本地化与表现层”统一到一套可维护的工程约定中。

它以注册器和稳定 ID 为核心，强调显式声明、可迁移的数据结构与可复用的生命周期接入，减少分散在各处的临时兼容补丁。

### RitsuLib 提供什么

- 内容侧：卡牌、遗物、药水、角色、事件、时间线、关键词、卡牌标签等注册能力
- 运行时侧：生命周期事件订阅、补丁辅助、设置界面、持久化与迁移
- 表现层侧：FMOD、顶栏按钮、Toast、快捷键、Shell 主题与相关 UI 扩展
- 工程侧：统一术语、清晰模块边界、可诊断的兼容策略

### 你可以从这里开始

1. [快速入门](/guide/getting-started)
2. [术语与命名](/guide/terminology)
3. [框架组织方式](/guide/framework-design)

### 适用场景

- 你正在从零开发一个《杀戮尖塔 2》Mod，需要完整的内容与运行时能力
- 你已有 Mod，希望把分散逻辑迁移到更稳定、可维护的结构
- 你需要团队协作场景下更统一的 API 约定与文档化开发流程

### 功能总览

| 模块 | 说明 | 入口 |
| --- | --- | --- |
| 开始编写 | 添加 NuGet 包、声明运行时依赖、创建第一个内容包 | [/guide/getting-started](/guide/getting-started) |
| 注册内容 | 卡牌、遗物、药水、角色、事件、Epoch、关键词、卡牌标签、自定义卡堆 | [/guide/content-authoring-toolkit](/guide/content-authoring-toolkit) |
| 安全保存状态 | 作用域 JSON 存储、迁移、档位切换支持、设置绑定 | [/guide/persistence-guide](/guide/persistence-guide) |
| 连接游戏流程 | 生命周期事件、Harmony 补丁辅助、Godot 脚本注册、兼容注意事项 | [/guide/lifecycle-events](/guide/lifecycle-events) |
| 增加表现层能力 | FMOD 辅助、顶栏按钮、卡堆、Toast、运行时快捷键、Shell 主题 | [/guide/fmod-and-audio](/guide/fmod-and-audio) |

</template>

<template v-else>

![Slay the Spire 2](https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp)

## Mod Authoring For Slay The Spire 2

RitsuLib is a Slay the Spire 2 mod framework that unifies content registration, runtime integration, persistence, settings, localization, and presentation helpers under one maintainable workflow.

It is registry-first, id-stable, and explicit by design: define what you own, keep data migrations predictable, and reuse lifecycle hooks instead of scattering ad-hoc compatibility patches.

### What RitsuLib Provides

- Content: cards, relics, potions, characters, events, timelines, keywords, tags, and related registries
- Runtime: lifecycle subscriptions, patch helpers, settings UI, persistence stores, and migration support
- Presentation: FMOD helpers, top-bar buttons, toasts, hotkeys, shell themes, and runtime UI extensions
- Engineering: consistent terminology, modular boundaries, and diagnosable compatibility behavior

### Start Here

1. [Getting started](/guide/getting-started)
2. [Terminology](/guide/terminology)
3. [How RitsuLib is organized](/guide/framework-design)

### Typical Use Cases

- Building a new Slay the Spire 2 mod from scratch with a complete technical baseline
- Refactoring an existing mod into a cleaner and more maintainable architecture
- Standardizing APIs and development flow for team-based mod projects

### Feature Map

| Area | What you get | Entry |
| --- | --- | --- |
| Start building | Add package, declare runtime dependency, create first content pack | [/guide/getting-started](/guide/getting-started) |
| Register content | Cards, relics, potions, characters, events, epochs, keywords, tags, custom piles | [/guide/content-authoring-toolkit](/guide/content-authoring-toolkit) |
| Keep state safely | Scoped JSON stores, migrations, profile switching support, settings bindings | [/guide/persistence-guide](/guide/persistence-guide) |
| Work with the game | Lifecycle events, Harmony patch helpers, Godot script registration, compatibility notes | [/guide/lifecycle-events](/guide/lifecycle-events) |
| Add polish | FMOD helpers, top-bar buttons, card piles, toast messages, runtime hotkeys, shell themes | [/guide/fmod-and-audio](/guide/fmod-and-audio) |

</template>
