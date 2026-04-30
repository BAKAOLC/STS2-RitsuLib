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

## Quick examples

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
| [STS2_FModProject_Minimal](https://github.com/BAKAOLC/STS2_FModProject_Minimal) (community) | Minimal project template reconstructed from cleaned reverse-engineered game FMOD assets. Good for a safe "custom content bank + GUIDs.txt" workflow without changing Master bank semantics |
| Built-in Godot FMOD plugin in the game | Same class of integration as `utopia-rise/fmod-gdextension`; provides the **`FmodServer`** singleton at runtime |
| [sts2-fmod-tools](https://github.com/elliotttate/sts2-fmod-tools) (community) | Optional: align Studio projects/events from the game-data side |
| DAW export | Export WAV/OGG, etc.; if mixing with vanilla SFX, watch loudness and dynamic range |

> RitsuLib wires **guids.txt-style mappings** into **`NAudioManager`** for path-based Studio calls (one-shots, loops, music, stops, parameters, **`UpdateMusicParameter`**, etc.). After your mod loads its **`.bank`** and calls **`TryLoadStudioGuidMappings`**, **`event:/…`** paths keep using the same **`NAudioManager` → AudioManagerProxy** pipeline as vanilla. Custom Harmony that replaces or bypasses that chain must coordinate with other mods.

---

## Authoring an extra mod bank (recommended workflow)

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

## Troubleshooting

- **`FmodStudioServer.TryGet()` is null** — `FmodServer` not ready (scene, headless test, or extension failed to load); check the game log
- **`TryCheckEventPath` is false** — the **`.bank`** is missing or unloaded, the path is wrong, **`TryLoadStudioGuidMappings`** did not succeed, or the bank was unloaded (use **`FmodStudioServer.TryLoadBank`**, which **pins** the returned **`FmodBank`** reference)
- **No sound and no exception** — **`TestMode`** / **`NonInteractiveMode`** may suppress **`NAudioManager`**; direct **`FmodServer`** calls are not subject to those flags

---

## Related documentation

- [Diagnostics & Compatibility](DiagnosticsAndCompatibility.md)
- [Patching Guide](PatchingGuide.md)
