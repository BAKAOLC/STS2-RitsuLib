---
title:
  en: FMOD & Audio
  zh-CN: FMOD 与音频
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

This document describes the game's audio architecture and the layered API that RitsuLib provides on top of it.

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

本文档说明游戏的音频架构，以及 RitsuLib 在此基础上提供的分层 API。

---

:::

## Game-native audio architecture{lang="en"}

::: en

> The following describes Slay the Spire 2 engine's own audio pipeline, to help explain the design background of RitsuLib's audio API.

Slay the Spire 2 plays audio through **Godot's FMOD Studio GDExtension** (`FmodServer` singleton). On the C# side this is wrapped by **`NAudioManager`**, which indirectly calls `FmodServer` via the GDScript proxy **`AudioManagerProxy`**.

This means:

- All vanilla audio playback ultimately goes through **`NAudioManager` → `AudioManagerProxy` → `FmodServer`**
- **`NAudioManager`** applies **`TestMode`** muting, SFX volume scaling, and related behaviour
- If a mod wants audio to **sound like the base game**, it should use the same pipeline

---

:::

## 游戏原版音频架构{lang="zh-CN"}

::: zh-CN

> 以下描述杀戮尖塔 2 引擎自身的音频管线，帮助理解 RitsuLib 音频 API 的设计背景。

杀戮尖塔 2 通过 **Godot 的 FMOD Studio GDExtension**（`FmodServer` 单例）播放音频。C# 侧由 `NAudioManager` 封装，它通过 GDScript 代理 `AudioManagerProxy` 间接调用 `FmodServer`。

这意味着：

- 原版的音频播放最终都经过 `NAudioManager` → `AudioManagerProxy` → `FmodServer` 这条路径
- `NAudioManager` 包含 `TestMode` 静音、SFX 音量施加等行为
- 如果 Mod 希望音频行为"听起来和原版一样"，应该走同一条管线

---

:::

## RitsuLib audio API{lang="en"}

::: en

RitsuLib layers the audio API so you can use the vanilla-aligned pipeline or talk to FMOD Studio directly when needed.

### Entry selection

| Need | Use |
|------|-----|
| Easier high-level playback, typed handles, lifecycle cleanup | **`GameFmod.Playback`** |
| Same routing / `TestMode` behaviour as vanilla | **`GameFmod.Studio`** → `NAudioManager` |
| Same guards as `SfxCmd` (non-interactive, combat ending, etc.) | **`Sts2SfxAlignedFmod`** |
| Load/unload Studio banks, check paths | **`FmodStudioServer`** |
| Fire-and-forget one-shots on `FmodServer` **without** going through `NAudioManager` | **`FmodStudioDirectOneShots`** |
| Bus volume/mute/pause, global parameters, DSP, performance data | **`FmodStudioBusAccess`**, **`FmodStudioMixerGlobals`** |
| Snapshots (`snapshot:/…`) | **`FmodStudioSnapshots`** |
| Long-lived `create_event_instance` handles | **`FmodStudioEventInstances`** |
| WAV/OGG/MP3 via plugin loaders | **`FmodStudioStreamingFiles`** |
| Cooldown / random pool helpers (no audio by themselves) | **`FmodPlaybackThrottle`**, **`FmodPathRoundRobinPool`** |

### Direct FMOD vs vanilla pipeline

- **`GameFmod.Studio`** and **`Sts2SfxAlignedFmod`** go through **`NAudioManager`** and share the game's GDScript proxy (including **`TestMode`**, SFX volume, etc.)
- **`FmodStudioDirectOneShots`** and most **`FmodStudio*`** helpers call **`FmodServer`** directly—good for custom banks, loose files, and bus debugging; one-shots are not guaranteed to match every subtlety of the in-game SFX bus path
- For **“sounds like vanilla”**, prefer **`GameFmod`** or **`Sts2SfxAlignedFmod`**

---

:::

## RitsuLib 音频 API{lang="zh-CN"}

::: zh-CN

RitsuLib 将音频 API 分层，既能走与原版一致的管线，也能在需要时直连 FMOD Studio。

### 入口选择

| 需求 | 使用 |
|---|---|
| 更易用的高层播放、返回 handle、自动生命周期清理 | **`GameFmod.Playback`** |
| 与原版相同的路由 / `TestMode` 行为 | **`GameFmod.Studio`** → `NAudioManager` |
| 与 `SfxCmd` 相同的防护（非交互、战斗结束等） | **`Sts2SfxAlignedFmod`** |
| 加载/卸载 Studio Bank、检查路径 | **`FmodStudioServer`** |
| 在 `FmodServer` 上直接 one-shot（不经过 `NAudioManager`） | **`FmodStudioDirectOneShots`** |
| Bus 音量/静音/暂停、全局参数、DSP、性能数据 | **`FmodStudioBusAccess`**、**`FmodStudioMixerGlobals`** |
| Snapshot（`snapshot:/…`） | **`FmodStudioSnapshots`** |
| 长期持有的 `create_event_instance` | **`FmodStudioEventInstances`** |
| 通过插件加载 WAV/OGG/MP3 | **`FmodStudioStreamingFiles`** |
| 冷却、随机池（本身不发声） | **`FmodPlaybackThrottle`**、**`FmodPathRoundRobinPool`** |

### 直连 FMOD 与原版管线的区别

- `GameFmod.Studio` 和 `Sts2SfxAlignedFmod` 走 `NAudioManager`，与原版游戏共享 GDScript 代理（含 `TestMode`、SFX 音量等）
- `FmodStudioDirectOneShots` 及多数 `FmodStudio*` 直接调用 `FmodServer`，适合自定义 Bank、散文件、Bus 调试；但 one-shot 不保证与游戏 SFX Bus 处理完全一致
- 如果要"听起来和原版一样"，优先使用 `GameFmod` 或 `Sts2SfxAlignedFmod`

---

:::

## Quick examples{lang="en"}

::: en

**Vanilla-aligned one-shot**

```csharp
using STS2RitsuLib.Audio;

Sts2SfxAlignedFmod.PlayOneShot("event:/sfx/heal");
GameFmod.Studio.PlayMusic("event:/music/menu_update");
```

**Mod content bank + `guids.txt` (must match the game's FMOD Studio major version line)**

```csharp
FmodStudioServer.TryLoadBank("res://mods/MyMod/banks/MyMod.bank");
FmodStudioServer.TryWaitForAllLoads();
if (!FmodStudioServer.TryLoadStudioGuidMappings("res://mods/MyMod/banks/MyMod.guids.txt"))
    return;
if (FmodStudioServer.TryCheckEventPath("event:/mods/mymod/hit") is true)
    GameFmod.Studio.PlayOneShot("event:/mods/mymod/hit");
```

**Loose file (short SFX — loaded as sound)**

```csharp
var sfxPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "ping.wav");
FmodStudioStreamingFiles.TryPlaySoundFile(sfxPath, volume: 0.9f);
```

**Streaming music file (recommended: Playback/Handle API)**

```csharp
var musicPath = ProjectSettings.GlobalizePath("user://mymod/loop.ogg");
var handle = GameFmod.Playback.PlayMusic(
    AudioSource.StreamingMusic(musicPath),
    new AudioPlaybackOptions { Volume = 0.7f, Scope = AudioLifecycleScope.Room }
);
```

**Common adaptive music flow (room / combat / victory)**

```csharp
var adaptive = GameFmod.Playback.FollowAdaptiveMusic(
    AudioAdaptivePlans.FullRunOverride(
        roomSource: AudioSource.StreamingMusic(roomLoopPath),
        combatSource: AudioSource.StreamingMusic(combatLoopPath),
        victorySource: AudioSource.StreamingMusic(victoryStingerPath)
    )
);
```

**Throttle rapid triggers**

```csharp
if (FmodPlaybackThrottle.TryEnter("my_power_proc", cooldownMs: 120))
    Sts2SfxAlignedFmod.PlayOneShot("event:/sfx/buff");
```

**Singleton channel: replace the current playback**

```csharp
GameFmod.Playback.PlayMusic(
    AudioSource.StreamingMusic(nextMusicPath),
    new AudioPlaybackOptions
    {
        Volume = 0.8f,
        Routing = new AudioRoutingOptions
        {
            Channel = "my-mod/music",
            ChannelMode = AudioChannelMode.ReplaceExisting,
            AllowFadeOutOnReplace = true,
        },
    }
);
```

**Tagged group: replace an entire UI cue group**

```csharp
GameFmod.Playback.Play(
    AudioSource.File(uiCuePath),
    new AudioPlaybackOptions
    {
        Routing = new AudioRoutingOptions
        {
            Tag = "my-mod/ui-tooltips",
            ReplaceTaggedGroup = true,
        },
    }
);
```

---

:::

## 简短示例{lang="zh-CN"}

::: zh-CN

**与原版一致的 one-shot**

```csharp
using STS2RitsuLib.Audio;

Sts2SfxAlignedFmod.PlayOneShot("event:/sfx/heal");
GameFmod.Studio.PlayMusic("event:/music/menu_update");
```

**模组内容 Bank + `guids.txt`（须与游戏 FMOD 主版本线兼容）**

```csharp
FmodStudioServer.TryLoadBank("res://mods/MyMod/banks/MyMod.bank");
FmodStudioServer.TryWaitForAllLoads();
if (!FmodStudioServer.TryLoadStudioGuidMappings("res://mods/MyMod/banks/MyMod.guids.txt"))
    return;
if (FmodStudioServer.TryCheckEventPath("event:/mods/mymod/hit") is true)
    GameFmod.Studio.PlayOneShot("event:/mods/mymod/hit");
```

**短音效文件（按 sound 加载）**

```csharp
var sfxPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "ping.wav");
FmodStudioStreamingFiles.TryPlaySoundFile(sfxPath, volume: 0.9f);
```

**流式音乐（推荐：新 Playback/Handle API）**

```csharp
var musicPath = ProjectSettings.GlobalizePath("user://mymod/loop.ogg");
var handle = GameFmod.Playback.PlayMusic(
    AudioSource.StreamingMusic(musicPath),
    new AudioPlaybackOptions { Volume = 0.7f, Scope = AudioLifecycleScope.Room }
);
```

**跟随游戏自动切换的常见三段式音乐（房间 / 战斗 / 胜利）**

```csharp
var adaptive = GameFmod.Playback.FollowAdaptiveMusic(
    AudioAdaptivePlans.FullRunOverride(
        roomSource: AudioSource.StreamingMusic(roomLoopPath),
        combatSource: AudioSource.StreamingMusic(combatLoopPath),
        victorySource: AudioSource.StreamingMusic(victoryStingerPath)
    )
);
```

**触发过快时节流**

```csharp
if (FmodPlaybackThrottle.TryEnter("my_power_proc", cooldownMs: 120))
    Sts2SfxAlignedFmod.PlayOneShot("event:/sfx/buff");
```

**单例频道：替换当前播放**

```csharp
GameFmod.Playback.PlayMusic(
    AudioSource.StreamingMusic(nextMusicPath),
    new AudioPlaybackOptions
    {
        Volume = 0.8f,
        Routing = new AudioRoutingOptions
        {
            Channel = "my-mod/music",
            ChannelMode = AudioChannelMode.ReplaceExisting,
            AllowFadeOutOnReplace = true,
        },
    }
);
```

**标签分组：替换整组 UI 提示音**

```csharp
GameFmod.Playback.Play(
    AudioSource.File(uiCuePath),
    new AudioPlaybackOptions
    {
        Routing = new AudioRoutingOptions
        {
            Tag = "my-mod/ui-tooltips",
            ReplaceTaggedGroup = true,
        },
    }
);
```

---

:::

## Auxiliary types (`STS2RitsuLib.Audio`){lang="en"}

::: en

| Type | Description |
|------|-------------|
| `FmodEventPath` | Lightweight wrapper for `event:/…` paths |
| `FmodStudioRouting` | Common bus path constants |
| `FmodParameterMap` | Builds parameter dictionaries for **`GameFmod.Studio`** |

**`STS2RitsuLib.Audio.Internal`** is internal implementation and is not a stable public API.

---

:::

## 辅助类型（`STS2RitsuLib.Audio`）{lang="zh-CN"}

::: zh-CN

| 类型 | 说明 |
|---|---|
| `FmodEventPath` | `event:/…` 路径轻量封装 |
| `FmodStudioRouting` | 常用 Bus 路径常量 |
| `FmodParameterMap` | 为 `GameFmod.Studio` 构造参数字典 |

`STS2RitsuLib.Audio.Internal` 为内部实现，不作为稳定公共 API。

---

:::

## Recommended external toolchain{lang="en"}

::: en

RitsuLib does not include the following; they are common external workflows:

| Tool | Role |
|------|------|
| [FMOD Studio](https://www.fmod.com/) | Edit banks and events. **Match the game's FMOD Studio major version line** (see the game's `addons/fmod` directory) |
| Built-in Godot FMOD plugin in the game | Same class of integration as `utopia-rise/fmod-gdextension`; provides the **`FmodServer`** singleton at runtime |
| [sts2-fmod-tools](https://github.com/elliotttate/sts2-fmod-tools) (community) | Optional: align Studio projects/events from the game-data side |
| DAW export | Export WAV/OGG, etc.; if mixing with vanilla SFX, watch loudness and dynamic range |

> RitsuLib wires **guids.txt-style mappings** into **`NAudioManager`** for path-based Studio calls (one-shots, loops, music, stops, parameters, **`UpdateMusicParameter`**, etc.). After your mod loads its **`.bank`** and calls **`TryLoadStudioGuidMappings`**, **`event:/…`** paths keep using the same **`NAudioManager` → AudioManagerProxy** pipeline as vanilla. Custom Harmony that replaces or bypasses that chain must coordinate with other mods.

---

:::

## 推荐外部工具链{lang="zh-CN"}

::: zh-CN

RitsuLib 不包含下列工具，它们是常见的外部工作流：

| 工具 | 作用 |
|---|---|
| [FMOD Studio](https://www.fmod.com/) | 编辑 Bank / Event。务必与游戏所用 FMOD 主版本线一致（可参考游戏目录 `addons/fmod`） |
| 游戏内置 Godot FMOD 插件 | 与 `utopia-rise/fmod-gdextension` 同类集成，运行时提供 `FmodServer` 单例 |
| [sts2-fmod-tools](https://github.com/elliotttate/sts2-fmod-tools)（社区） | 可选：从游戏数据侧辅助对齐 Studio 工程/事件 |
| DAW 导出 | 导出 WAV/OGG 等；若与原版 SFX 混播，注意响度与动态范围 |

> RitsuLib 已对 **`NAudioManager`** 中与路径相关的 Studio 调用（OneShot / Loop / Music / Stop / SetParam / `UpdateMusicParameter` 等）接入 **guids.txt 映射**：模组在加载 **`.bank`** 后调用 **`TryLoadStudioGuidMappings`**，即可继续用 **`event:/…`** 字符串走与原版相同的 **`NAudioManager` → AudioManagerProxy** 管线。自定义替换或绕过该链路的 Harmony 补丁需自行与其它 Mod 协调。

---

:::

## Authoring an extra mod bank (recommended workflow){lang="en"}

::: en

Use this workflow when you ship **only your own `.bank`** plus a **`*.guids.txt`** from the **same Studio build**.

### 1. Bank type and naming

- **Do not replace or overwrite** the shipped **`Master.bank`**.
- Ship a **separately named content bank** (sometimes called a sidecar / child bank). Its file name and the **Bank** name inside FMOD Studio should be **globally unique** among mods and future official banks to avoid **naming collisions**.
- That bank holds **your** events and media; the **mixer / Master routing** still comes from the game's already-loaded vanilla banks.

### 2. Bus / Master alignment (match vanilla mixing)

- At runtime, **`AudioManagerProxy`** expects buses such as **`bus:/master`**, **`bus:/master/sfx`**, **`bus:/master/music`**, **`bus:/master/ambience`** (consistent with the desktop bank load order).
- **For vanilla-like loudness slider and bus behaviour**, route your events to those **`bus:/…`** paths—the same hierarchy **defined by the game's Master-side data**—instead of publishing a competing top-level Master bank that replaces the official one.
- **When you must verify identifiers**: compare **Bus / VCA** paths and GUIDs against the game's **GUIDs.txt** or tools like **`sts2-fmod-tools`**. Export **GUIDs.txt** from **the same FMOD Studio build** as your **`.bank`** so text and binary never drift apart.

### 3. Export GUIDs and ship them with the mod

1. **Build** your bank in FMOD Studio.
2. Take **`GUIDs.txt`** from the build output (or export a GUID list).
3. Ship it as a text resource (e.g. **`YourMod.guids.txt`**): keep every **`event:/…`** line (`{guid} event:/…`, one record per line); you may keep other lines for debugging.
4. After **`TryLoadBank`** + **`TryWaitForAllLoads`**, call **`FmodStudioServer.TryLoadStudioGuidMappings("res://…/YourMod.guids.txt")`**. That fills the path → GUID table and logs success/failure; together with RitsuLib's **`NAudioManager`** Harmony prefixes, **`event:/…`** paths keep resolving through **`NAudioManager`**.

### 4. Runtime order and stability

- Load your mod bank **after** the game's FMOD bootstrap and **`NAudioManager`** are ready (for example from a deferred-init callback); loading too early can leave the Studio cache in a bad state for probes.
- Use **`FmodStudioServer.TryLoadBank`**: the implementation **pins** the returned **`FmodBank`** reference so it is not finalized immediately (the GDExtension **`FmodBank`** destructor calls **`unload_bank`**).

### 5. Toolchain version and artefact pairing

- Match the **FMOD Studio major line** to the game's **`addons/fmod`** / runtime.
- Always ship **`.bank`** and **`GUIDs.txt` slice** from the **same build**. Mixing an old bank with a new GUID file (or vice versa) breaks **`check_event_guid`** / path resolution at runtime.

---

:::

## 模组附加 Bank 制作（推荐流程）{lang="zh-CN"}

::: zh-CN

模组仅发布 **自建 `.bank`** 与同次构建导出的 **`*.guids.txt`** 时，建议按下述方式制作与接入。

### 1. Bank 类型与命名

- **不要替换或改名覆盖**游戏自带的 **`Master.bank`**。
- 模组应使用 **独立命名的内容 Bank**（又称「子 Bank」、Sidecar Bank），文件名与 Studio 内 **Bank 名称**在游戏内全局 **唯一**，避免与其它模组或未来的官方 Bank **重名冲突**。
- 该 Bank 仅承载你的 Event / 采样；混音树上的 **Master / Routing** 仍依赖游戏已加载的原版 Master 管线。

### 2. Bus / Master 对齐（与原版混音一致）

- 游戏里 **`AudioManagerProxy`** 使用的典型路径包括 **`bus:/master`**、**`bus:/master/sfx`**、**`bus:/master/music`**、**`bus:/master/ambience`**（与原版 `banks/desktop` 加载顺序下的 Studio 缓存一致）。
- **若希望模组音效/音乐的分轨、衰减、音量滑条行为与原版一致**：在 FMOD Studio 中为 Event 指定的 **Routing / Output**，应落到上述 **已与原版一致的 `bus:/…`** 路径上（即路由到游戏里已经存在、GUID 由原版 Master 侧定义的 Bus），而不要自造一套与原版无关的顶层 Master Bank 去顶替官方。
- **需要逐项对齐时**：可从官方/解包工程的 **GUIDs.txt**、`sts2-fmod-tools` 等对照 **Bus / VCA** 的路径与 GUID；同一 **FMOD Studio 主版本线** 前提下，与你的模组 Bank **同一次构建**导出 **GUIDs.txt**，避免 txt 与 `.bank` 二进制不一致。

### 3. 导出 GUID 并导入模组资源

1. 在 FMOD Studio 中 **Build** 你的模组 Bank。
2. 在生成目录中取 **`GUIDs.txt`**（或由 Studio 导出 **GUID List**）。
3. 拷贝为模组中的文本资源（例如 **`Evil.guids.txt`**）：至少保留全部 **`event:/…`** 行（格式为 **`{xxxxxxxx-…} event:/…`**，一行一条）；可按需保留与其它对象相关的行便于自查。
4. 游戏初始化时在 **`TryLoadBank`**（或你的封装）加载 **`.bank`**、`TryWaitForAllLoads` 之后调用 **`FmodStudioServer.TryLoadStudioGuidMappings("res://…/YourMod.guids.txt")`**：框架会写入路径 → GUID 表并打日志；与 **RitsuLib** 内对 **`NAudioManager`** 的 Harmony 前缀配合后，即可用 **`event:/…`** 字符串走 **`NAudioManager`** 原版入口。

### 4. 运行时顺序与稳定性

- **在游戏的 FMOD 启动流程与 `NAudioManager` 已就绪之后**再 `TryLoadBank` 你的模组 Bank（例如在延迟初始化回调中）；过早加载时 Studio 侧缓存可能尚未稳定，探测易失败。
- 使用 **`FmodStudioServer.TryLoadBank`** 加载模组 Bank：实现会 **保留返回的 `FmodBank` 引用**，避免仅校验返回值后引用被回收导致 **引擎侧自动 unload**（见 GDExtension `FmodBank` 析构行为）。

### 5. 版本与产物一致性

- **FMOD Studio 主版本**须与游戏内 **`addons/fmod`** 所用库一致（或官方文档允许的兼容范围）。
- **同一次 Build** 产出的 **`.bank`** 与 **`GUIDs.txt`** 必须成对发布；任意一侧来自旧构建都会导致 **`check_event_guid` / 路径解析失败**。

---

:::

## Troubleshooting{lang="en"}

::: en

- **`FmodStudioServer.TryGet()` is null** — `FmodServer` not ready (scene, headless test, or extension failed to load); check the game log
- **`TryCheckEventPath` is false** — the **`.bank`** is missing or unloaded, the path is wrong, **`TryLoadStudioGuidMappings`** did not succeed, or the bank was unloaded (use **`FmodStudioServer.TryLoadBank`**, which **pins** the returned **`FmodBank`** reference)
- **No sound and no exception** — **`TestMode`** / **`NonInteractiveMode`** may suppress **`NAudioManager`**; direct **`FmodServer`** calls are not subject to those flags

---

:::

## 排错{lang="zh-CN"}

::: zh-CN

- **`FmodStudioServer.TryGet()` 为 null** — `FmodServer` 未就绪（场景、无头测试或扩展加载失败），查游戏日志
- **`TryCheckEventPath` 为 false** — 对应 **`.bank` 未加载**、路径写错、**`TryLoadStudioGuidMappings` 未成功**，或 **Bank 已被卸载**（须使用会 **pin `FmodBank` 引用** 的 `TryLoadBank` 封装）
- **无声且无异常** — `TestMode` / `NonInteractiveMode` 可能抑制 `NAudioManager`；直连 `FmodServer` 不受这些标志约束

---

:::

## Related documentation{lang="en"}

::: en

- [Diagnostics & Compatibility](/guide/diagnostics-and-compatibility)
- [Patching Guide](/guide/patching-guide)

:::

## 相关文档{lang="zh-CN"}

::: zh-CN

- [诊断与兼容层](/guide/diagnostics-and-compatibility)
- [补丁系统](/guide/patching-guide)

:::
