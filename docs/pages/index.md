---
title:
  en: RitsuLib
  zh-CN: RitsuLib

features:
  title:
    en: Overview
    zh-CN: 概览
  subtitle:
    en: Slay the Spire 2
    zh-CN: 杀戮尖塔 2
  text:
    en: >-
      A shared mod framework for Slay the Spire 2: registry-driven content, Harmony patching helpers, persistence stores,
      lifecycle events, localization (`I18N` / keywords), Godot scene registration, FMOD helpers, and diagnostics that
      align with vanilla progression checks.
    zh-CN: >-
      面向《杀戮尖塔 2》的共享模组框架：注册器驱动的内容、Harmony 补丁封装、持久化存储与迁移、生命周期事件、
      本地化（`I18N` / 关键词）、Godot 场景注册、FMOD 辅助接口以及与原版进度节点对齐的诊断与兼容层。

  cards:
    - title:
        en: Content & registries
        zh-CN: 内容与注册器
      details:
        en: >-
          Fixed identity, content packs, character/unlock scaffolding, custom events, timelines, and placeholder content rules.
        zh-CN: >-
          固定身份、内容包、角色与解锁装配、自定义事件、时间线以及占位内容等注册与约束
    - title:
        en: Patching & lifecycle
        zh-CN: 补丁与生命周期
      details:
        en: >-
          `ModPatcher`, `IPatchMethod`, grouped targets, plus lifecycle subscriptions and replay semantics for engine events.
        zh-CN: >-
          `ModPatcher`、`IPatchMethod`、分组目标，以及针对引擎事件的订阅与可重放语义
    - title:
        en: Persistence & settings
        zh-CN: 持久化与设置
      details:
        en: >-
          Scoped mod data stores with migrations and profile switching; optional settings UI bound to `ModDataStore`.
        zh-CN: >-
          带迁移与档位切换的作用域存储；可选的设置界面并与 `ModDataStore` 绑定
    - title:
        en: Localization & audio
        zh-CN: 本地化与音频
      details:
        en: >-
          `I18N`, keyword registry, LocString tooling, and FMOD Studio path → GUID mapping helpers on top of the vanilla audio pipeline.
        zh-CN: >-
          `I18N`、关键词注册、LocString 工具，以及在原版音频管线之上提供 FMOD Studio 路径映射等能力
    - title:
        en: Godot & diagnostics
        zh-CN: Godot 与诊断
      details:
        en: >-
          Scene script registration, asset profile fallbacks, narrow compatibility patches, and one-time diagnostic warnings.
        zh-CN: >-
          场景脚本注册、资源配置回退、窄兼容补丁以及一次性诊断警告策略
    - title:
        en: Documentation
        zh-CN: 文档
      details:
        en: >-
          Guides are maintained as Markdown under `docs/pages/guide/` in this repository and built with Valaxy.
        zh-CN: >-
          指南文档在仓库 `docs/pages/guide/` 中维护，并由 Valaxy 构建为站点
---
