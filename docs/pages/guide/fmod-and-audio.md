---
title:
  en: FMOD And Audio
  zh-CN: FMOD 与音频
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Play Audio{lang="en"}

::: en

Use `GameAudioService.Shared` for new code. It accepts event paths, GUIDs, loose sound files, streaming music files, and snapshots through `AudioSource`.

```csharp
using STS2RitsuLib.Audio;

GameAudioService.Shared.PlayOneShot(
    AudioSource.Event("event:/MyMod/ui/click"),
    new AudioPlaybackOptions
    {
        Volume = 0.8f,
        Parameters = FmodParameterMap.Set(("intensity", 1f)),
        Scope = AudioLifecycleScope.Room,
    });
```

For quick compatibility with existing path-based calls, `GameFmod.Studio` and `Sts2SfxAlignedFmod` remain available.

:::

## 播放音频{lang="zh-CN"}

::: zh-CN

新代码优先使用 `GameAudioService.Shared`。它通过 `AudioSource` 接收事件路径、GUID、散装音效文件、流式音乐文件和 snapshot。

```csharp
using STS2RitsuLib.Audio;

GameAudioService.Shared.PlayOneShot(
    AudioSource.Event("event:/MyMod/ui/click"),
    new AudioPlaybackOptions
    {
        Volume = 0.8f,
        Parameters = FmodParameterMap.Set(("intensity", 1f)),
        Scope = AudioLifecycleScope.Room,
    });
```

已有 path-based 调用可以继续使用 `GameFmod.Studio` 和 `Sts2SfxAlignedFmod`。

:::

## Loops And Music{lang="en"}

::: en

Keep the returned handle when you need to stop or adjust playback later.

```csharp
var loop = GameAudioService.Shared.PlayLoop(
    AudioSource.Event("event:/MyMod/ambience/engine"),
    new AudioPlaybackOptions
    {
        Routing = new AudioRoutingOptions(Channel: "my_mod_ambience"),
        Scope = AudioLifecycleScope.Run,
    });

loop?.TrySetParameter("danger", 0.5f);
loop?.TryStop();
```

Use `PlayMusic(...)` for music handles and `FollowAdaptiveMusic(...)` for room / combat / victory plans.

:::

## 循环与音乐{lang="zh-CN"}

::: zh-CN

之后需要停止或调整播放时，保留返回的 handle。

```csharp
var loop = GameAudioService.Shared.PlayLoop(
    AudioSource.Event("event:/MyMod/ambience/engine"),
    new AudioPlaybackOptions
    {
        Routing = new AudioRoutingOptions(Channel: "my_mod_ambience"),
        Scope = AudioLifecycleScope.Run,
    });

loop?.TrySetParameter("danger", 0.5f);
loop?.TryStop();
```

音乐使用 `PlayMusic(...)`，房间 / 战斗 / 胜利切换使用 `FollowAdaptiveMusic(...)`。

:::

## Audio Files{lang="en"}

::: en

`AudioSource.File(...)` and `AudioSource.StreamingMusic(...)` take an absolute path, a `user://` path, or a raw `res://` file. Set the file's import mode to "Keep File (No Import)" so it is packed as-is. `AudioSource.ResourceFile(...)` and `AudioSource.StreamingResourceMusic(...)` also accept imported audio: WAV, MP3, and Ogg Vorbis data is extracted into a private cache. Imported IMA ADPCM and QOA WAV resources are decoded to PCM WAV.

The cache uses ASCII virtual paths, so non-ASCII user names and resource names do not prevent playback. Loose files with non-ASCII paths are localized or copied into the cache before loading. Each source file and cached result is limited to 256 MiB. Compressed WAV decoding is limited to 600 seconds. Dynamic and interactive streams are not supported by these file APIs.

:::

## 音频文件{lang="zh-CN"}

::: zh-CN

`AudioSource.File(...)` 和 `AudioSource.StreamingMusic(...)` 接受绝对路径、`user://` 路径或原始 `res://` 文件。音频文件的导入方式要设为“Keep File (No Import)”，这样才会原样打进包。`AudioSource.ResourceFile(...)` 和 `AudioSource.StreamingResourceMusic(...)` 也接受导入后的音频：WAV、MP3 和 Ogg Vorbis 数据会提取到私有缓存，导入后的 IMA ADPCM 和 QOA WAV 资源会解码为 PCM WAV。

缓存使用 ASCII 虚拟路径，用户名和资源名称含非 ASCII 字符也能播放。含非 ASCII 字符的松散文件路径会先转换为虚拟路径或复制到缓存，再加载。每个源文件和缓存结果上限为 256 MiB，压缩 WAV 解码时长上限为 600 秒。这些文件接口不支持动态或交互式音频流。

:::

## Banks And GUID Mappings{lang="en"}

::: en

Load banks before using their events:

```csharp
FmodStudioDeferredBankRegistration.RegisterBank("res://MyMod/audio/MyMod.bank");
FmodStudioDeferredBankRegistration.RegisterStudioGuidMappings("res://MyMod/audio/guid_map.json");
```

If a bank is optional, use the lower-level `FmodStudioServer.TryLoadBank(...)` and handle failure gracefully.

:::

## Bank 与 GUID 映射{lang="zh-CN"}

::: zh-CN

使用事件前先加载 bank：

```csharp
FmodStudioDeferredBankRegistration.RegisterBank("res://MyMod/audio/MyMod.bank");
FmodStudioDeferredBankRegistration.RegisterStudioGuidMappings("res://MyMod/audio/guid_map.json");
```

可选 bank 使用更底层的 `FmodStudioServer.TryLoadBank(...)`，并处理加载失败。

:::

## Lifecycle And Routing{lang="en"}

::: en

| Option | Use |
| --- | --- |
| `Scope` | Stops audio automatically with room, combat, run, or manual lifetime. |
| `ScopeToken` | Groups handles under a manual scope. |
| `Routing.Channel` | Allows one active handle per channel, optionally replacing the old one. |
| `Routing.Tag` | Groups several handles for `StopTag(...)`. |
| `CooldownMs` | Prevents rapid repeated playback. |
| `UseVanillaRouting` | Lets one-shot and music event paths route through vanilla where applicable. |

Use routing for UI and looping ambience. Avoid global stop calls unless the mod owns all audio in that group.

:::

## 生命周期与路由{lang="zh-CN"}

::: zh-CN

| 选项 | 用途 |
| --- | --- |
| `Scope` | 随房间、战斗、run 或手动生命周期自动停止音频。 |
| `ScopeToken` | 把多个 handle 放进手动 scope。 |
| `Routing.Channel` | 每个 channel 只保留一个活动 handle，可选择替换旧 handle。 |
| `Routing.Tag` | 给多个 handle 分组，之后用 `StopTag(...)` 停止。 |
| `CooldownMs` | 避免高频重复播放。 |
| `UseVanillaRouting` | 适用时让 one-shot 和 music event path 走原版路由。 |

UI 和循环环境音适合使用 routing。除非该组音频都由你的 Mod 拥有，否则不要随意做全局停止。

:::
