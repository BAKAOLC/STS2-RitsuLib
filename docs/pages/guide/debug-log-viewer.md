---
title:
  en: Debug Log Viewer
  zh-CN: 调试日志查看器
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## What It Is{lang="en"}

::: en

RitsuLib can start a browser-based live log viewer for the current game session. It is meant for development, compatibility checks, and fast inspection while the game is still running.

Use it when a normal `godot.log` tail is too slow, when you want to inspect the latest records from another screen, or when a tester needs a browser URL instead of a local file path.

:::

## 它是什么{lang="zh-CN"}

::: zh-CN

RitsuLib 可以为当前游戏会话启动一个基于浏览器的实时日志查看器。它主要用于开发调试、兼容排查，以及游戏仍在运行时快速查看最新日志。

当单纯查看 `godot.log` 不够方便，或者你希望在另一块屏幕、另一台设备上查看最近日志时，就适合使用它。

:::

## Enable And Open{lang="en"}

::: en

The viewer is configured from the built-in RitsuLib settings pages. After startup you can open the current viewer from the settings page, or from the dev console:

```text
openlogviewer
```

When auto-open is enabled, RitsuLib waits briefly after startup. If no browser page is already connected, it opens the viewer in the system browser.

:::

## 启用与打开{lang="zh-CN"}

::: zh-CN

查看器通过 RitsuLib 自带的设置页配置。游戏启动后，可以直接从设置页打开当前会话的查看器，也可以在 dev console 里执行：

```text
openlogviewer
```

如果启用了自动打开，RitsuLib 会在启动后短暂等待；若此时还没有浏览器页面连接，就会主动在系统浏览器中打开查看器。

:::

## LAN Access And Token{lang="en"}

::: en

By default the viewer listens on loopback only. Enable LAN access only when another device on the same network really needs to connect.

LAN mode exposes the viewer on all network interfaces and uses a tokenized browser URL. Treat that URL as a local secret for the session: share it only with the people or devices that actually need access.

Port and LAN binding changes apply on the next game launch.

:::

## 局域网访问与 Token{lang="zh-CN"}

::: zh-CN

默认情况下，查看器只监听本机 loopback。只有确实需要让同一局域网中的其他设备连接时，才启用 LAN 访问。

LAN 模式会把查看器绑定到所有网络接口，并通过带 token 的浏览器 URL 提供访问。这个 URL 应当视为当前会话的本地密钥，只分享给真正需要查看日志的人或设备。

端口和 LAN 监听方式的改动需要在下次启动游戏后生效。

:::

## What Shows Up{lang="en"}

::: en

The viewer keeps a recent in-memory ring buffer and streams new records live. It can also mirror game logger callbacks, so it is useful for checking both framework messages and ordinary runtime diagnostics in one place.

If you are debugging a noisy issue, keep the browser viewer for the live stream and still preserve normal log files for later attachments or release reports.

:::

## 查看器里会显示什么{lang="zh-CN"}

::: zh-CN

查看器会保留一段最近日志的内存 ring buffer，并持续推送新的记录。它还可以镜像游戏 logger 的回调，所以既适合看框架日志，也适合把普通运行时诊断集中到同一个界面里查看。

如果问题日志很多，建议把浏览器查看器当作实时观察窗口，同时保留常规日志文件，方便后续附在 issue、反馈或发布排查材料里。

:::

## Practical Notes{lang="en"}

::: en

- Use loopback mode for normal solo development.
- Use LAN mode only for short-lived debugging with a second device or remote tester on the same network.
- Keep the port stable once a team starts bookmarking the viewer URL.
- If the viewer is unavailable, fall back to the normal file logs and check whether the session started the viewer at all.

:::

## 实用注意点{lang="zh-CN"}

::: zh-CN

- 日常单机开发优先使用 loopback 模式。
- 只有在同网段第二设备或测试人员确实需要查看时，才临时开启 LAN 模式。
- 团队一旦开始收藏查看器地址，端口最好保持稳定。
- 如果查看器不可用，就回退到普通日志文件，并先确认本次会话是否真的启动了查看器。

:::
