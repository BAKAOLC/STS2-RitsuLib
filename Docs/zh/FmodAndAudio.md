# FMOD 与音频

本文档说明游戏的音频架构，以及 RitsuLib 在此基础上提供的分层 API。

---

## 游戏原版音频架构

> 以下描述杀戮尖塔 2 引擎自身的音频管线，帮助理解 RitsuLib 音频 API 的设计背景。

杀戮尖塔 2 通过 **Godot 的 FMOD Studio GDExtension**（`FmodServer` 单例）播放音频。C# 侧由 `NAudioManager` 封装，它通过 GDScript 代理 `AudioManagerProxy` 间接调用 `FmodServer`。

这意味着：

- 原版的音频播放最终都经过 `NAudioManager` → `AudioManagerProxy` → `FmodServer` 这条路径
- `NAudioManager` 包含 `TestMode` 静音、SFX 音量施加等行为
- 如果 Mod 希望音频行为"听起来和原版一样"，应该走同一条管线

---

## RitsuLib 音频 API

RitsuLib 将音频 API 分层，既能走与原版一致的管线，也能在需要时直连 FMOD Studio。

### 入口选择

| 需求 | 使用 |
|---|---|
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

## 简短示例

**与原版一致的 one-shot**

```csharp
using STS2RitsuLib.Audio;

Sts2SfxAlignedFmod.PlayOneShot("event:/sfx/heal");
GameFmod.Studio.PlayMusic("event:/music/menu_update");
```

**自定义 Studio Bank（需与游戏 FMOD 主版本线兼容）**

```csharp
FmodStudioServer.TryLoadBank("res://mods/MyMod/banks/MyMod.strings.bank");
FmodStudioServer.TryLoadBank("res://mods/MyMod/banks/MyMod.bank");
if (FmodStudioServer.TryCheckEventPath("event:/mods/mymod/hit") is true)
    FmodStudioDirectOneShots.TryPlay("event:/mods/mymod/hit");
```

**短音效文件（按 sound 加载）**

```csharp
var sfxPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "ping.wav");
FmodStudioStreamingFiles.TryPlaySoundFile(sfxPath, volume: 0.9f);
```

**流式音乐**

```csharp
var musicPath = ProjectSettings.GlobalizePath("user://mymod/loop.ogg");
FmodStudioStreamingFiles.TryPreloadAsStreamingMusic(musicPath);
var handle = FmodStudioStreamingFiles.TryCreateStreamingMusicInstance(musicPath);
handle?.Call("set_volume", 0.7f);
handle?.Call("play");
```

**触发过快时节流**

```csharp
if (FmodPlaybackThrottle.TryEnter("my_power_proc", cooldownMs: 120))
    Sts2SfxAlignedFmod.PlayOneShot("event:/sfx/buff");
```

---

## 辅助类型（`STS2RitsuLib.Audio`）

| 类型 | 说明 |
|---|---|
| `FmodEventPath` | `event:/…` 路径轻量封装 |
| `FmodStudioRouting` | 常用 Bus 路径常量 |
| `FmodParameterMap` | 为 `GameFmod.Studio` 构造参数字典 |

`STS2RitsuLib.Audio.Internal` 为内部实现，不作为稳定公共 API。

---

## 推荐外部工具链

RitsuLib 不包含下列工具，它们是常见的外部工作流：

| 工具 | 作用 |
|---|---|
| [FMOD Studio](https://www.fmod.com/) | 编辑 Bank / Event。务必与游戏所用 FMOD 主版本线一致（可参考游戏目录 `addons/fmod`） |
| 游戏内置 Godot FMOD 插件 | 与 `utopia-rise/fmod-gdextension` 同类集成，运行时提供 `FmodServer` 单例 |
| [sts2-fmod-tools](https://github.com/elliotttate/sts2-fmod-tools)（社区） | 可选：从游戏数据侧辅助对齐 Studio 工程/事件 |
| DAW 导出 | 导出 WAV/OGG 等；若与原版 SFX 混播，注意响度与动态范围 |

> RitsuLib 未内置基于 Harmony 的全局事件替换。若要在 `NAudioManager.PlayOneShot` 层拦截，请使用 `IPatchMethod` 自行实现，并注意与其它 Mod 的协调。

---

## 排错

- **`FmodStudioServer.TryGet()` 为 null** — `FmodServer` 未就绪（场景、无头测试或扩展加载失败），查游戏日志
- **`TryCheckEventPath` 为 false** — Bank 未加载或路径错误；Studio 工程通常需先加载 strings Bank
- **无声且无异常** — `TestMode` / `NonInteractiveMode` 可能抑制 `NAudioManager`；直连 `FmodServer` 不受这些标志约束

---

## 相关文档

- [诊断与兼容层](DiagnosticsAndCompatibility.md)
- [补丁系统](PatchingGuide.md)
