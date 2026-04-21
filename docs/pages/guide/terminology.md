---
title:
  en: Terminology
  zh-CN: 术语表
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

This document defines the canonical terms used across the RitsuLib documentation.

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

本文定义 RitsuLib 文档中统一使用的核心术语及推荐译法。

---

:::

## Core Terms{lang="en"}

::: en

| Term | Preferred usage | Notes |
|---|---|---|
| settings UI | settings UI | Use for the mod configuration interface as a whole. |
| settings page | page | A single registered page in the settings UI. |
| section | section | A structured group within a page. |
| entry | entry | One visible row or control within a section. |
| binding | binding | The read/write link between UI and stored or in-memory state. |
| persistence | persistence | The storage layer and save lifecycle. |
| persisted | persisted | Use for values written through the persistence layer. |
| preview-only | preview-only | Use for controls or bindings that never persist data. |
| fallback | fallback | Preferred over `shim` for compatibility behavior. |
| compatibility fallback | compatibility fallback | A narrowly scoped behavior used when vanilla data or APIs are incomplete. |
| bridge patch | bridge patch | A patch that forwards mod content into vanilla logic that would otherwise skip it. |
| registry | registry | The runtime registration container for a content type. |
| content pack | content pack | The convenience entry point that writes into multiple registries. |
| builder | builder | Use for fluent page, section, or content construction APIs. |
| override | override | Use for replacing an asset path, behavior, or value source. |
| placeholder | placeholder | A temporary fallback value used when data is missing. |
| scope | scope | The storage scope of a persisted value. |
| profile | profile | Per-profile save scope. |
| global | global | Cross-profile save scope. |
| epoch | epoch | Keep the game term `epoch` in English. |
| story | story | Keep the game term `story` in English. |
| Ancient dialogue | Ancient dialogue | Use this spelling for the game system and related keys. |

---

:::

## 核心术语{lang="zh-CN"}

::: zh-CN

| 英文术语 | 推荐中文 | 说明 |
|---|---|---|
| settings UI | 设置界面 | 指整体玩家配置界面。 |
| page | 页面 | 设置界面中的单个已注册页面。 |
| section | 分区 | 页面中的结构化分组。 |
| entry | 条目 | 分区中的单行可见控件或文本项。 |
| binding | 绑定 | UI 与存储值或内存状态之间的读写连接。 |
| persistence | 持久化 | 存储层与保存生命周期。 |
| persisted | 已持久化 / 会持久化 | 用于描述会写入持久化层的值。 |
| preview-only | 仅预览 | 指不会写入持久化层的控件或绑定。 |
| fallback | 回退 | 兼容或缺失数据场景下的回退行为。 |
| compatibility fallback | 兼容回退 | 优先使用该术语，避免使用“垫片”。 |
| bridge patch | 桥接补丁 | 将 Mod 内容转发到原版逻辑检查点的补丁。 |
| registry | 注册器 | 某类内容的运行时注册容器。 |
| content pack | 内容包 | 向多个注册器写入内容的便捷入口。 |
| builder | 构建器 | 用于链式构造页面、分区或内容的 API。 |
| override | 覆写 | 对资源路径、行为或值来源进行替换。 |
| placeholder | 占位值 | 数据缺失时使用的临时值。 |
| scope | 作用域 | 持久化值的存储范围。 |
| profile | 档位 | 按玩家档位区分的保存范围。 |
| global | 全局 | 跨档位共享的保存范围。 |
| epoch | 纪元（Epoch） | 中文文档首次出现可带英文，后续可简称“纪元”。 |
| story | 故事（Story） | 中文文档首次出现可带英文，后续可简称“故事”。 |
| Ancient dialogue | Ancient 对话 | 与游戏系统保持一致，不改写为其他称呼。 |

---

:::

## Related Documents{lang="en"}

::: en

- [Framework Design](/guide/framework-design)
- [Mod Settings](/guide/mod-settings)
- [Diagnostics & Compatibility](/guide/diagnostics-and-compatibility)
- [Localization & Keywords](/guide/localization-and-keywords)

:::

## 相关文档{lang="zh-CN"}

::: zh-CN

- [框架设计](/guide/framework-design)
- [Mod 设置界面](/guide/mod-settings)
- [诊断与兼容层](/guide/diagnostics-and-compatibility)
- [本地化与关键词](/guide/localization-and-keywords)

:::
