---
title:
  en: Diagnostics And Compatibility
  zh-CN: 诊断与兼容
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Use Warnings As Release Signals{lang="en"}

::: en

RitsuLib tries to log one clear warning for common authoring problems:

| Warning area | Usually means |
| --- | --- |
| Content registration | A model was registered too late, twice, or with a conflicting id. |
| Asset paths | A profile points at a missing resource. |
| Localization | A key is missing from a game table or I18N source. |
| Unlocks | A rule references an epoch or character that cannot resolve. |
| Patching | A required target method is missing or a patch class has no Harmony method. |
| Audio | A bank, event path, GUID mapping, bus, or loose audio file could not be resolved. |

Treat character asset warnings, required patch failures, and model-id conflicts as release blockers.

:::

## 把警告当作发布信号{lang="zh-CN"}

::: zh-CN

RitsuLib 会尽量为常见作者错误记录一次清楚的警告：

| 警告区域 | 通常表示 |
| --- | --- |
| 内容注册 | 模型注册太晚、重复注册，或 ID 冲突。 |
| 资源路径 | Profile 指向了不存在的资源。 |
| 本地化 | 游戏表或 I18N 来源缺 key。 |
| 解锁 | 规则引用了无法解析的 epoch 或角色。 |
| 补丁 | 必要目标方法缺失，或 patch 类没有 Harmony 方法。 |
| 音频 | Bank、事件路径、GUID 映射、bus 或散装音频文件无法解析。 |

角色资源警告、必要 patch 失败、模型 ID 冲突都应视为发布阻断问题。

:::

## Debug Compatibility Mode{lang="en"}

::: en

RitsuLib's own debug compatibility mode is off by default. When enabled in the RitsuLib settings page, it exposes fallback toggles for development and compatibility testing.

Use it to investigate missing localization, invalid unlock epochs, and missing Architect dialogue. Do not rely on debug fallbacks as the normal release path for your mod.

:::

## Debug 兼容模式{lang="zh-CN"}

::: zh-CN

RitsuLib 自带的 debug compatibility mode 默认关闭。开启后，RitsuLib 设置页会显示用于开发和兼容测试的回退开关。

它适合调查缺失本地化、无效解锁 epoch、建筑师缺少对话等问题。不要把 debug 回退当作 Mod 的正常发布路径。

:::

## Browser Debug Log Viewer{lang="en"}

::: en

For interactive debugging, RitsuLib can also host a browser-based live log viewer for the current session. It is useful when a tester needs a URL, when you want a second-screen log view, or when you need to inspect fresh warnings without leaving the running game.

Use loopback mode for normal development. Enable LAN access only when another device on the same network needs the viewer, and treat the tokenized URL as session-local access data.

See [Debug log viewer](/guide/debug-log-viewer) for setup and usage details.

:::

## 浏览器调试日志查看器{lang="zh-CN"}

::: zh-CN

在交互式排查阶段，RitsuLib 还可以为当前会话启动一个基于浏览器的实时日志查看器。它适合给测试人员提供一个 URL、在副屏上盯日志，或者在不离开运行中游戏的情况下查看最新警告。

普通开发优先使用 loopback 模式。只有同一局域网中的另一台设备确实需要查看时，才启用 LAN 访问，并把带 token 的 URL 当作本次会话的本地访问凭据。

具体配置和使用方式见 [调试日志查看器](/guide/debug-log-viewer)。

:::

## Game Source Notes{lang="en"}

::: en

When checking decompiled or reference game source, keep these rules in mind:

- Match the game API branch used by your package (`STS2.RitsuLib` or `STS2.RitsuLib.Compat.<api-version>`).
- Confirm method overloads before writing a `ModPatchTarget`; use `parameterTypes` when there is any ambiguity.
- Some lifecycle hooks differ by host API. Prefer RitsuLib events such as `CardsFlushedEvent` when they already bridge those differences.
- If a behavior depends on a private field or compiler-generated state machine, isolate it behind a small compatibility helper and add diagnostics.

:::

## 游戏源码注意点{lang="zh-CN"}

::: zh-CN

查看反编译或引用版游戏源码时，注意这些规则：

- 源码版本必须匹配你使用的包分支（`STS2.RitsuLib` 或 `STS2.RitsuLib.Compat.<api-version>`）。
- 写 `ModPatchTarget` 前确认目标方法重载；只要有歧义就填写 `parameterTypes`。
- 部分生命周期 hook 会随 host API 变化。RitsuLib 已经提供桥接事件时，优先使用例如 `CardsFlushedEvent` 这样的事件。
- 行为依赖私有字段或编译器生成状态机时，把它隔离成小兼容 helper，并添加诊断。

:::

## Release Checklist{lang="en"}

::: en

- Build against the intended game API branch.
- Start a new run and load an existing run.
- Switch profiles if the mod uses `SaveScope.Profile`.
- Verify both installed languages you ship.
- Open all settings pages from main menu and pause menu.
- Check logs for RitsuLib warnings after content registration and after first combat.
- Test optional compatibility patches with their target feature absent.

:::

## 发布检查清单{lang="zh-CN"}

::: zh-CN

- 使用目标游戏 API 分支构建。
- 测试新 run 和读取旧 run。
- 如果使用 `SaveScope.Profile`，测试切换档位。
- 验证随 Mod 发布的语言。
- 从主菜单和暂停菜单打开所有设置页。
- 内容注册后和第一次战斗后检查 RitsuLib 警告日志。
- 可选兼容 patch 需要测试目标功能不存在的情况。

:::
