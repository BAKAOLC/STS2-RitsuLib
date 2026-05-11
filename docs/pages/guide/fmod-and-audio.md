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
