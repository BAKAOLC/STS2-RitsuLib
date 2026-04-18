# FMOD & Audio

This document describes the game's audio architecture and the layered API that RitsuLib provides on top of it.

---

## Game-native audio architecture

> The following describes Slay the Spire 2 engine's own audio pipeline, to help explain the design background of RitsuLib's audio API.

Slay the Spire 2 plays audio through **Godot's FMOD Studio GDExtension** (`FmodServer` singleton). On the C# side this is wrapped by **`NAudioManager`**, which indirectly calls `FmodServer` via the GDScript proxy **`AudioManagerProxy`**.

This means:

- All vanilla audio playback ultimately goes through **`NAudioManager` → `AudioManagerProxy` → `FmodServer`**
- **`NAudioManager`** applies **`TestMode`** muting, SFX volume scaling, and related behaviour
- If a mod wants audio to **sound like the base game**, it should use the same pipeline

---

## RitsuLib audio API

RitsuLib layers the audio API so you can use the vanilla-aligned pipeline or talk to FMOD Studio directly when needed.

### Entry selection

| Need | Use |
|------|-----|
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

## Quick examples

**Vanilla-aligned one-shot**

```csharp
using STS2RitsuLib.Audio;

Sts2SfxAlignedFmod.PlayOneShot("event:/sfx/heal");
GameFmod.Studio.PlayMusic("event:/music/menu_update");
```

**Custom Studio bank (must match the game's FMOD Studio major version line)**

```csharp
FmodStudioServer.TryLoadBank("res://mods/MyMod/banks/MyMod.strings.bank");
FmodStudioServer.TryLoadBank("res://mods/MyMod/banks/MyMod.bank");
if (FmodStudioServer.TryCheckEventPath("event:/mods/mymod/hit") is true)
    FmodStudioDirectOneShots.TryPlay("event:/mods/mymod/hit");
```

**Loose file (short SFX — loaded as sound)**

```csharp
var sfxPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, "ping.wav");
FmodStudioStreamingFiles.TryPlaySoundFile(sfxPath, volume: 0.9f);
```

**Streaming music file**

```csharp
var musicPath = ProjectSettings.GlobalizePath("user://mymod/loop.ogg");
FmodStudioStreamingFiles.TryPreloadAsStreamingMusic(musicPath);
var handle = FmodStudioStreamingFiles.TryCreateStreamingMusicInstance(musicPath);
handle?.Call("set_volume", 0.7f);
handle?.Call("play");
```

**Throttle rapid triggers**

```csharp
if (FmodPlaybackThrottle.TryEnter("my_power_proc", cooldownMs: 120))
    Sts2SfxAlignedFmod.PlayOneShot("event:/sfx/buff");
```

---

## Auxiliary types (`STS2RitsuLib.Audio`)

| Type | Description |
|------|-------------|
| `FmodEventPath` | Lightweight wrapper for `event:/…` paths |
| `FmodStudioRouting` | Common bus path constants |
| `FmodParameterMap` | Builds parameter dictionaries for **`GameFmod.Studio`** |

**`STS2RitsuLib.Audio.Internal`** is internal implementation and is not a stable public API.

---

## Recommended external toolchain

RitsuLib does not include the following; they are common external workflows:

| Tool | Role |
|------|------|
| [FMOD Studio](https://www.fmod.com/) | Edit banks and events. **Match the game's FMOD Studio major version line** (see the game's `addons/fmod` directory) |
| Built-in Godot FMOD plugin in the game | Same class of integration as `utopia-rise/fmod-gdextension`; provides the **`FmodServer`** singleton at runtime |
| [sts2-fmod-tools](https://github.com/elliotttate/sts2-fmod-tools) (community) | Optional: align Studio projects/events from the game-data side |
| DAW export | Export WAV/OGG, etc.; if mixing with vanilla SFX, watch loudness and dynamic range |

> RitsuLib does not ship Harmony-based global event replacement. To intercept at the **`NAudioManager.PlayOneShot`** layer, implement it yourself with **`IPatchMethod`** and coordinate with other mods.

---

## Troubleshooting

- **`FmodStudioServer.TryGet()` is null** — `FmodServer` not ready (scene, headless test, or extension failed to load); check the game log
- **`TryCheckEventPath` is false** — bank not loaded or wrong path; Studio projects usually need the **strings** bank loaded first
- **No sound and no exception** — **`TestMode`** / **`NonInteractiveMode`** may suppress **`NAudioManager`**; direct **`FmodServer`** calls are not subject to those flags

---

## Related documentation

- [Diagnostics & Compatibility](DiagnosticsAndCompatibility.md)
- [Patching Guide](PatchingGuide.md)
