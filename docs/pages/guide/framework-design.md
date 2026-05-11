---
title:
  en: How RitsuLib Is Organized
  zh-CN: 框架组织方式
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Choose An Entry Point{lang="en"}

::: en

Most mods only need five entry points.

| Need | Use |
| --- | --- |
| Register models, keywords, epochs, card piles, and top-bar buttons | `RitsuLibFramework.CreateContentPack(modId)` |
| Patch game methods | `RitsuLibFramework.CreatePatcher(modId, patcherName)` |
| React to game timing | `RitsuLibFramework.SubscribeLifecycle<TEvent>(...)` |
| Store JSON data | `RitsuLibFramework.BeginModDataRegistration(modId)` and `GetDataStore(modId)` |
| Add settings UI | `RitsuLibFramework.RegisterModSettings(modId, configure)` |

Use lower-level registries only when you need conditional registration, manifest-style arrays, or integration code shared across several mods.

:::

## 选择入口{lang="zh-CN"}

::: zh-CN

大多数 Mod 只需要五个入口。

| 需求 | 使用 |
| --- | --- |
| 注册模型、关键词、Epoch、卡堆和顶栏按钮 | `RitsuLibFramework.CreateContentPack(modId)` |
| Patch 游戏方法 | `RitsuLibFramework.CreatePatcher(modId, patcherName)` |
| 响应游戏时机 | `RitsuLibFramework.SubscribeLifecycle<TEvent>(...)` |
| 存储 JSON 数据 | `RitsuLibFramework.BeginModDataRegistration(modId)` 与 `GetDataStore(modId)` |
| 添加设置界面 | `RitsuLibFramework.RegisterModSettings(modId, configure)` |

只有在需要条件注册、数组清单式注册，或要把集成代码复用到多个 Mod 时，才直接使用底层注册器。

:::

## User API Layers{lang="en"}

::: en

RitsuLib's public API is split by the work a mod author is doing:

- `Scaffolding.Content` supplies templates and builder methods for game content.
- `Content`, `Keywords`, `CardTags`, `CardPiles`, `Timeline`, `Unlocks`, and `TopBar` hold registries.
- `Data` and `Utils.Persistence` handle mod data.
- `Settings.ModSettings` and `Settings.ModSettingsUi` build player-facing settings pages.
- `Patching` wraps Harmony registration and diagnostics.
- `Audio`, `RuntimeInput`, and `Ui` provide runtime helpers.

:::

## 用户 API 层级{lang="zh-CN"}

::: zh-CN

RitsuLib 的公开 API 按 Mod 作者正在做的事情划分：

- `Scaffolding.Content` 提供游戏内容模板和 builder 方法。
- `Content`、`Keywords`、`CardTags`、`CardPiles`、`Timeline`、`Unlocks`、`TopBar` 保存注册器。
- `Data` 和 `Utils.Persistence` 处理 Mod 数据。
- `Settings.ModSettings` 与 `Settings.ModSettingsUi` 构建玩家可见的设置页面。
- `Patching` 封装 Harmony 注册和诊断。
- `Audio`、`RuntimeInput`、`Ui` 提供运行时辅助能力。

:::

## Recommended Reading Order{lang="en"}

::: en

1. [Getting started](/guide/getting-started)
2. [Content authoring](/guide/content-authoring-toolkit)
3. One feature page: character, settings, persistence, audio, or patching
4. [Diagnostics and compatibility](/guide/diagnostics-and-compatibility) before release

:::

## 推荐阅读顺序{lang="zh-CN"}

::: zh-CN

1. [快速入门](/guide/getting-started)
2. [内容编写](/guide/content-authoring-toolkit)
3. 选择一个正在使用的专题：角色、设置、持久化、音频或补丁
4. 发布前阅读 [诊断与兼容](/guide/diagnostics-and-compatibility)

:::
