---
title:
  en: Persistence
  zh-CN: 持久化
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Register Data{lang="en"}

::: en

Register each saved concept as a class. Do it inside `BeginModDataRegistration` so initialization happens after the batch is complete.

```csharp
public sealed class MySettings
{
    public bool Enabled { get; set; } = true;
    public int Volume { get; set; } = 80;
}

using (RitsuLibFramework.BeginModDataRegistration("MyMod"))
{
    var store = RitsuLibFramework.GetDataStore("MyMod");
    store.Register(
        key: "settings",
        fileName: "settings.json",
        scope: SaveScope.Global,
        defaultFactory: () => new MySettings(),
        autoCreateIfMissing: true);
}
```

Use classes rather than primitive values so you can add fields later without changing the storage slot.

:::

## 注册数据{lang="zh-CN"}

::: zh-CN

每个需要保存的概念定义为一个 class。放在 `BeginModDataRegistration` 里注册，这样整批完成后再初始化。

```csharp
public sealed class MySettings
{
    public bool Enabled { get; set; } = true;
    public int Volume { get; set; } = 80;
}

using (RitsuLibFramework.BeginModDataRegistration("MyMod"))
{
    var store = RitsuLibFramework.GetDataStore("MyMod");
    store.Register(
        key: "settings",
        fileName: "settings.json",
        scope: SaveScope.Global,
        defaultFactory: () => new MySettings(),
        autoCreateIfMissing: true);
}
```

不要直接保存裸基础类型。使用 class 后，未来新增字段不需要更换存储槽。

:::

## Choose A Scope{lang="en"}

::: en

| Scope | Use for |
| --- | --- |
| `SaveScope.Global` | Mod settings, account-wide preferences, caches shared by all game profiles. |
| `SaveScope.Profile` | Progression, unlock-like data, and anything tied to the current game profile. |
| `SaveScope.InMemory` | Temporary process-local data that should use the same store API but never writes to disk. |

Use `RitsuLibFramework.GetRunSavedDataStore(modId)` for data that belongs inside a run save. Run saved data is embedded into the run snapshot and follows the run through save/load and multiplayer synchronization.

:::

## 选择作用域{lang="zh-CN"}

::: zh-CN

| Scope | 适合保存 |
| --- | --- |
| `SaveScope.Global` | Mod 设置、账号级偏好、所有游戏档位共享的缓存。 |
| `SaveScope.Profile` | 进度、类似解锁的数据、和当前游戏档位绑定的内容。 |
| `SaveScope.InMemory` | 临时进程内数据：复用 store API，但不写盘。 |

属于一次跑局存档的数据请使用 `RitsuLibFramework.GetRunSavedDataStore(modId)`。Run saved data 会嵌入跑局快照，并随存档读取、写入和多人同步一起流转。

:::

## Run Saved Data{lang="en"}

::: en

Use `RunSavedData` for values that are part of a specific run: challenge settings chosen in the start-run lobby, run counters, draft state, or per-player state that must survive save/load and multiplayer rejoin. Register slots early and keep their keys stable after release.

```csharp
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using STS2RitsuLib.RunData;

public sealed class ChallengeRunState
{
    public string? ChallengeId { get; set; }
    public int ElitesKilled { get; set; }
}

public sealed class PlayerRunState
{
    public string? LoadoutId { get; set; }
}

private static RunSavedData<ChallengeRunState> ChallengeData = null!;
private static PlayerRunSavedData<PlayerRunState> PlayerData = null!;

using (RitsuLibFramework.BeginModDataRegistration("MyMod"))
{
    var runStore = RitsuLibFramework.GetRunSavedDataStore("MyMod");

    ChallengeData = runStore.Register(
        key: "challenge",
        defaultFactory: () => new ChallengeRunState(),
        options: new RunSavedDataOptions
        {
            WritePolicy = RunSavedDataWritePolicy.WhenNonDefault,
            SyncLobbyOnChange = true,
        });

    PlayerData = runStore.RegisterPerPlayer(
        key: "player",
        defaultFactory: () => new PlayerRunState(),
        options: new RunSavedDataOptions
        {
            SyncLobbyOnChange = true,
        });
}
```

The registered handles are what you keep and use later. `RunSavedData<T>` stores one shared value for the run; `PlayerRunSavedData<T>` stores one value per player net id.

```csharp
public static void RecordEliteKilled(RunState runState)
{
    ChallengeData.Modify(runState, data =>
    {
        data.ElitesKilled++;
    });
}

public static string? GetPlayerLoadout(RunState runState, ulong netId)
{
    return PlayerData.TryGet(runState, netId, out var data)
        ? data.LoadoutId
        : null;
}
```

For values chosen before the run starts, write through the slot's `Lobby` accessor. Lobby values are committed into the run snapshot when the new run begins. If `SyncLobbyOnChange` is enabled, `Set` and `Modify` also push the local contribution in multiplayer.

```csharp
public static void SetLobbyChallenge(StartRunLobby lobby, string challengeId)
{
    ChallengeData.Lobby.Modify(lobby, data =>
    {
        data.ChallengeId = challengeId;
    });
}

public static void SetLocalLoadout(StartRunLobby lobby, string loadoutId)
{
    PlayerData.Lobby.Modify(lobby, lobby.NetService.NetId, data =>
    {
        data.LoadoutId = loadoutId;
    });
}
```

Use `RunSavedDataPreparingEvent` when data should be finalized just before a new run snapshot is exported.

```csharp
RitsuLibFramework.SubscribeLifecycle<RunSavedDataPreparingEvent>(evt =>
{
    ChallengeData.Modify(evt.RunState, data =>
    {
        data.ChallengeId ??= "standard";
    });
});
```

Keep the payload types as plain JSON-serializable classes with public properties. Prefer adding nullable or defaulted properties over changing a slot key.

:::

## 跑局保存数据{lang="zh-CN"}

::: zh-CN

`RunSavedData` 用于属于某一次跑局的值：开局大厅选择的挑战参数、跑局计数器、草稿状态，或者需要随存档读取、多人重连一起保留的玩家数据。槽位应尽早注册，并且发布后保持 key 稳定。

```csharp
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;
using STS2RitsuLib.RunData;

public sealed class ChallengeRunState
{
    public string? ChallengeId { get; set; }
    public int ElitesKilled { get; set; }
}

public sealed class PlayerRunState
{
    public string? LoadoutId { get; set; }
}

private static RunSavedData<ChallengeRunState> ChallengeData = null!;
private static PlayerRunSavedData<PlayerRunState> PlayerData = null!;

using (RitsuLibFramework.BeginModDataRegistration("MyMod"))
{
    var runStore = RitsuLibFramework.GetRunSavedDataStore("MyMod");

    ChallengeData = runStore.Register(
        key: "challenge",
        defaultFactory: () => new ChallengeRunState(),
        options: new RunSavedDataOptions
        {
            WritePolicy = RunSavedDataWritePolicy.WhenNonDefault,
            SyncLobbyOnChange = true,
        });

    PlayerData = runStore.RegisterPerPlayer(
        key: "player",
        defaultFactory: () => new PlayerRunState(),
        options: new RunSavedDataOptions
        {
            SyncLobbyOnChange = true,
        });
}
```

保存好注册返回的句柄，后续通过它读写。`RunSavedData<T>` 为整局保存一个共享值；`PlayerRunSavedData<T>` 按玩家 net id 分别保存值。

```csharp
public static void RecordEliteKilled(RunState runState)
{
    ChallengeData.Modify(runState, data =>
    {
        data.ElitesKilled++;
    });
}

public static string? GetPlayerLoadout(RunState runState, ulong netId)
{
    return PlayerData.TryGet(runState, netId, out var data)
        ? data.LoadoutId
        : null;
}
```

跑局开始前已经确定的值，通过槽位的 `Lobby` 访问器写入。大厅暂存值会在新跑局开始时提交进跑局快照。启用 `SyncLobbyOnChange` 后，`Set` 和 `Modify` 还会在多人模式下推送本机贡献。

```csharp
public static void SetLobbyChallenge(StartRunLobby lobby, string challengeId)
{
    ChallengeData.Lobby.Modify(lobby, data =>
    {
        data.ChallengeId = challengeId;
    });
}

public static void SetLocalLoadout(StartRunLobby lobby, string loadoutId)
{
    PlayerData.Lobby.Modify(lobby, lobby.NetService.NetId, data =>
    {
        data.LoadoutId = loadoutId;
    });
}
```

如果需要在新跑局快照导出前补齐默认值或最终值，可以订阅 `RunSavedDataPreparingEvent`。

```csharp
RitsuLibFramework.SubscribeLifecycle<RunSavedDataPreparingEvent>(evt =>
{
    ChallengeData.Modify(evt.RunState, data =>
    {
        data.ChallengeId ??= "standard";
    });
});
```

载荷类型建议保持为普通 JSON 可序列化 class，并使用 public property。数据结构演进时优先新增可空或带默认值的属性，不要轻易更换槽位 key。

:::

## Read And Write{lang="en"}

::: en

```csharp
var store = RitsuLibFramework.GetDataStore("MyMod");

var settings = store.Get<MySettings>("settings");

store.Modify<MySettings>("settings", data =>
{
    data.Volume = 60;
});

store.Save("settings");
```

`Get<T>` returns the live object. `Modify<T>` mutates that object. Saving is explicit unless another layer, such as a settings binding, calls `Save()` for you.

:::

## 读取与写入{lang="zh-CN"}

::: zh-CN

```csharp
var store = RitsuLibFramework.GetDataStore("MyMod");

var settings = store.Get<MySettings>("settings");

store.Modify<MySettings>("settings", data =>
{
    data.Volume = 60;
});

store.Save("settings");
```

`Get<T>` 返回活动对象。`Modify<T>` 修改这个对象。保存默认是显式的，除非设置绑定等上层能力替你调用 `Save()`。

:::

## Migrate Formats{lang="en"}

::: en

Add migrations before publishing a breaking data shape.

```csharp
store.Register<MySettings>(
    "settings",
    "settings.json",
    SaveScope.Global,
    defaultFactory: () => new MySettings(),
    migrationConfig: new ModDataMigrationConfig(
        currentDataVersion: 2,
        minimumSupportedDataVersion: 1),
    migrations:
    [
        new SettingsV1ToV2Migration(),
    ]);
```

Keep `fileName` and `key` stable after release. Change the schema version when the JSON shape changes in a way old files cannot deserialize directly.

:::

## 迁移格式{lang="zh-CN"}

::: zh-CN

发布破坏性数据结构前，先准备迁移。

```csharp
store.Register<MySettings>(
    "settings",
    "settings.json",
    SaveScope.Global,
    defaultFactory: () => new MySettings(),
    migrationConfig: new ModDataMigrationConfig(
        currentDataVersion: 2,
        minimumSupportedDataVersion: 1),
    migrations:
    [
        new SettingsV1ToV2Migration(),
    ]);
```

发布后保持 `fileName` 和 `key` 稳定。当 JSON 结构变化到旧文件不能直接反序列化时，提升 schema version。

:::

## Attached State{lang="en"}

::: en

Use `AttachedState<TKey,TValue>` for runtime-only state attached to reference objects. Use `SavedAttachedState<TKey,TValue>` only for model objects that already pass through the game's `SavedProperties` serialization.

```csharp
private static readonly SavedAttachedState<CardModel, int> BonusDamage =
    new("bonus_damage", () => 0);

BonusDamage[card] = 3;
```

For normal mod settings, progression, and feature data, prefer `ModDataStore`.

:::

## 附加状态{lang="zh-CN"}

::: zh-CN

`AttachedState<TKey,TValue>` 用于挂在引用对象上的运行时状态。`SavedAttachedState<TKey,TValue>` 只适合本来就经过游戏 `SavedProperties` 序列化的模型对象。

```csharp
private static readonly SavedAttachedState<CardModel, int> BonusDamage =
    new("bonus_damage", () => 0);

BonusDamage[card] = 3;
```

普通 Mod 设置、进度和功能数据，优先使用 `ModDataStore`。

:::
