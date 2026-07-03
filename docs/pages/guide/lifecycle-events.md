---
title:
  en: Lifecycle Events
  zh-CN: 生命周期事件
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Subscribe{lang="en"}

::: en

Use lifecycle events when a mod needs game timing but does not need to own a Harmony patch.

```csharp
var subscription = RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
{
    Logger.Info($"Game ready: {evt.Game.Name}");
});
```

Dispose the returned subscription when the handler is temporary.

```csharp
RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>((evt, sub) =>
{
    PrepareForCombat(evt.RunState);
    sub.Dispose();
});
```

Replayable events are delivered immediately to late subscribers by default. Pass `replayCurrentState: false` when you only want future events.

:::

## 订阅{lang="zh-CN"}

::: zh-CN

Mod 需要游戏时机但不需要自己拥有 Harmony patch 时，使用生命周期事件。

```csharp
var subscription = RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
{
    Logger.Info($"Game ready: {evt.Game.Name}");
});
```

临时 handler 用完后 dispose 返回的 subscription。

```csharp
RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>((evt, sub) =>
{
    PrepareForCombat(evt.RunState);
    sub.Dispose();
});
```

可重放事件默认会立即补发给后订阅者。只想接收未来事件时，传 `replayCurrentState: false`。

:::

## Common Events{lang="en"}

::: en

| Timing | Events |
| --- | --- |
| Framework boot | `FrameworkInitializingEvent`, `FrameworkInitializedEvent` |
| Model setup | `ContentRegistrationClosedEvent`, `ModelRegistryInitializedEvent`, `ModelIdsInitializedEvent`, `ModelPreloadingCompletedEvent` |
| Game node | `GameTreeEnteredEvent`, `GameReadyEvent` |
| Profiles and saves | `ProfileIdInitializedEvent`, `ProfileSwitchingEvent`, `ProfileSwitchedEvent`, `RunSavingEvent`, `RunSavedEvent`, `ProgressSavingEvent`, `ProgressSavedEvent`, `ProfileDeletingEvent`, `ProfileDeletedEvent` |
| Run flow | `RunStartedEvent`, `RunLoadedEvent`, `RunEndedEvent`, `RoomEnteringEvent`, `RoomEnteredEvent`, `RoomExitedEvent`, `ActEnteringEvent`, `ActEnteredEvent` |
| Combat | `CombatStartingEvent`, `CombatEndedEvent`, `CombatVictoryEvent`, `SideTurnStartingEvent`, `SideTurnStartedEvent`, `CardPlayingEvent`, `CardPlayedEvent` |
| Combat resources | `EnergyGainedEvent`, `EnergyResetEvent`, `EnergySpentEvent`, `StarsGainedEvent`, `StarsSpentEvent` |
| Cards | `CardMovedBetweenPilesEvent`, `CardDrawnEvent`, `CardDiscardedEvent`, `CardExhaustedEvent`, `BeforeFlushEvent`, `CardsFlushedEvent` |
| Rewards and inventory | `GoldGainedEvent`, `GoldLostEvent`, `PotionProcuredEvent`, `PotionDiscardedEvent`, `RelicObtainedEvent`, `RelicRemovedEvent`, `RewardTakenEvent` |
| Unlocks | `EpochObtainedEvent`, `EpochRevealedEvent`, `UnlockIncrementedEvent` |

:::

## 常用事件{lang="zh-CN"}

::: zh-CN

| 时机 | 事件 |
| --- | --- |
| 框架启动 | `FrameworkInitializingEvent`、`FrameworkInitializedEvent` |
| 模型设置 | `ContentRegistrationClosedEvent`、`ModelRegistryInitializedEvent`、`ModelIdsInitializedEvent`、`ModelPreloadingCompletedEvent` |
| 游戏节点 | `GameTreeEnteredEvent`、`GameReadyEvent` |
| 档位与存档 | `ProfileIdInitializedEvent`、`ProfileSwitchingEvent`、`ProfileSwitchedEvent`、`RunSavingEvent`、`RunSavedEvent`、`ProgressSavingEvent`、`ProgressSavedEvent`、`ProfileDeletingEvent`、`ProfileDeletedEvent` |
| Run 流程 | `RunStartedEvent`、`RunLoadedEvent`、`RunEndedEvent`、`RoomEnteringEvent`、`RoomEnteredEvent`、`RoomExitedEvent`、`ActEnteringEvent`、`ActEnteredEvent` |
| 战斗 | `CombatStartingEvent`、`CombatEndedEvent`、`CombatVictoryEvent`、`SideTurnStartingEvent`、`SideTurnStartedEvent`、`CardPlayingEvent`、`CardPlayedEvent` |
| 战斗资源 | `EnergyGainedEvent`、`EnergyResetEvent`、`EnergySpentEvent`、`StarsGainedEvent`、`StarsSpentEvent` |
| 卡牌 | `CardMovedBetweenPilesEvent`、`CardDrawnEvent`、`CardDiscardedEvent`、`CardExhaustedEvent`、`BeforeFlushEvent`、`CardsFlushedEvent` |
| 奖励与物品栏 | `GoldGainedEvent`、`GoldLostEvent`、`PotionProcuredEvent`、`PotionDiscardedEvent`、`RelicObtainedEvent`、`RelicRemovedEvent`、`RewardTakenEvent` |
| 解锁 | `EpochObtainedEvent`、`EpochRevealedEvent`、`UnlockIncrementedEvent` |

:::

## Version Notes{lang="en"}

::: en

`CardRetainedEvent` is obsolete on newer host APIs. Use `CardsFlushedEvent` when you need retained and flushed cards together.

For game API differences that affect event availability, check [Diagnostics and compatibility](/guide/diagnostics-and-compatibility).

:::

## 版本注意点{lang="zh-CN"}

::: zh-CN

`CardRetainedEvent` 在较新 host API 上已过时。需要同时观察 retained 和 flushed cards 时，使用 `CardsFlushedEvent`。

影响事件可用性的游戏 API 差异见 [诊断与兼容](/guide/diagnostics-and-compatibility)。

:::
