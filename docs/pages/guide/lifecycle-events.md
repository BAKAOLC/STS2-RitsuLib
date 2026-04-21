---
title:
  en: Lifecycle Events
  zh-CN: 生命周期事件参考
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

This document lists all lifecycle events provided by RitsuLib, explains subscription patterns, and details replayable event behavior.

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

本文列出 RitsuLib 提供的全部生命周期事件，介绍订阅方式及可重放事件的行为。

---

:::

## Subscription Patterns{lang="en"}

::: en

### Subscribe by Event Type (Recommended)

```csharp
var sub = RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
{
    Logger.Info($"Game ready: {evt.Game}");
});

// Unsubscribe
sub.Dispose();
```

### Subscribe via `ILifecycleObserver`

```csharp
public class MyObserver : ILifecycleObserver
{
    public void OnEvent(IFrameworkLifecycleEvent evt)
    {
        if (evt is CombatStartingEvent combat)
            HandleCombatStart(combat);
        else if (evt is RunEndedEvent run)
            HandleRunEnd(run);
    }
}

RitsuLibFramework.SubscribeLifecycle(new MyObserver());
```

> **Replayable events** (`IReplayableFrameworkLifecycleEvent`): if you subscribe after the event has already fired, the framework immediately calls your handler with the stored event instance — no timing concerns.

---

:::

## 订阅方式{lang="zh-CN"}

::: zh-CN

### 按类型订阅（推荐）

```csharp
var sub = RitsuLibFramework.SubscribeLifecycle<GameReadyEvent>(evt =>
{
    Logger.Info($"游戏已就绪：{evt.Game}");
});

// 取消订阅
sub.Dispose();
```

### 通过 `ILifecycleObserver` 订阅多种事件

```csharp
public class MyObserver : ILifecycleObserver
{
    public void OnEvent(IFrameworkLifecycleEvent evt)
    {
        if (evt is CombatStartingEvent combat)
            HandleCombatStart(combat);
        else if (evt is RunEndedEvent run)
            HandleRunEnd(run);
    }
}

RitsuLibFramework.SubscribeLifecycle(new MyObserver());
```

> **可重放事件（`IReplayableFrameworkLifecycleEvent`）：** 若在事件已发生后才订阅，框架会立即以已存储的事件实例回调，无需关心订阅时机。

---

:::

## Framework Events{lang="en"}

::: en

Fired during framework initialization and profile service setup.

| Event | Replayable | Payload |
|---|---|---|
| `FrameworkInitializingEvent` | — | `FrameworkModId`, `FrameworkVersion` |
| `FrameworkInitializedEvent` | ✓ | `FrameworkModId`, `IsActive` |
| `ProfileServicesInitializingEvent` | — | — |
| `ProfileServicesInitializedEvent` | ✓ | `ProfileId` |

---

:::

## 框架事件{lang="zh-CN"}

::: zh-CN

框架初始化与 Profile 服务初始化阶段触发。

| 事件 | 可重放 | 携带数据 |
|---|---|---|
| `FrameworkInitializingEvent` | — | `FrameworkModId`、`FrameworkVersion` |
| `FrameworkInitializedEvent` | ✓ | `FrameworkModId`、`IsActive` |
| `ProfileServicesInitializingEvent` | — | — |
| `ProfileServicesInitializedEvent` | ✓ | `ProfileId` |

---

:::

## Game Bootstrap Events{lang="en"}

::: en

Fired in sequence during game startup, from model registration through to game ready.

| Event | Replayable | Payload |
|---|---|---|
| `EssentialInitializationStartingEvent` | — | — |
| `EssentialInitializationCompletedEvent` | ✓ | — |
| `DeferredInitializationStartingEvent` | — | — |
| `DeferredInitializationCompletedEvent` | ✓ | — |
| `ContentRegistrationClosedEvent` | ✓ | `Reason` |
| `ModelRegistryInitializingEvent` | — | — |
| `ModelRegistryInitializedEvent` | ✓ | `RegisteredModelTypeCount` |
| `ModelIdsInitializingEvent` | — | — |
| `ModelIdsInitializedEvent` | ✓ | — |
| `ModelPreloadingStartingEvent` | — | — |
| `ModelPreloadingCompletedEvent` | ✓ | — |
| `GameTreeEnteredEvent` | ✓ | `Game` |
| `GameReadyEvent` | ✓ | `Game` |

```csharp
RitsuLibFramework.SubscribeLifecycle<ModelIdsInitializedEvent>(_ =>
{
    var id = ModelDb.GetId<MyCard>();
});
```

---

:::

## 游戏引导事件{lang="zh-CN"}

::: zh-CN

游戏启动流程中依次触发，覆盖 Model 注册到游戏就绪全程。

| 事件 | 可重放 | 携带数据 |
|---|---|---|
| `EssentialInitializationStartingEvent` | — | — |
| `EssentialInitializationCompletedEvent` | ✓ | — |
| `DeferredInitializationStartingEvent` | — | — |
| `DeferredInitializationCompletedEvent` | ✓ | — |
| `ContentRegistrationClosedEvent` | ✓ | `Reason` |
| `ModelRegistryInitializingEvent` | — | — |
| `ModelRegistryInitializedEvent` | ✓ | `RegisteredModelTypeCount` |
| `ModelIdsInitializingEvent` | — | — |
| `ModelIdsInitializedEvent` | ✓ | — |
| `ModelPreloadingStartingEvent` | — | — |
| `ModelPreloadingCompletedEvent` | ✓ | — |
| `GameTreeEnteredEvent` | ✓ | `Game` |
| `GameReadyEvent` | ✓ | `Game` |

```csharp
RitsuLibFramework.SubscribeLifecycle<ModelIdsInitializedEvent>(_ =>
{
    var id = ModelDb.GetId<MyCard>();
});
```

---

:::

## Run Events{lang="en"}

::: en

| Event | Replayable | Payload |
|---|---|---|
| `RunStartedEvent` | — | `RunState`, `IsMultiplayer`, `IsDaily` |
| `RunLoadedEvent` | — | `RunState`, `IsMultiplayer`, `IsDaily` |
| `RunEndedEvent` | — | `Run`, `IsVictory`, `IsAbandoned` |

---

:::

## 跑局事件{lang="zh-CN"}

::: zh-CN

| 事件 | 可重放 | 携带数据 |
|---|---|---|
| `RunStartedEvent` | — | `RunState`、`IsMultiplayer`、`IsDaily` |
| `RunLoadedEvent` | — | `RunState`、`IsMultiplayer`、`IsDaily` |
| `RunEndedEvent` | — | `Run`、`IsVictory`、`IsAbandoned` |

---

:::

## Room & Act Events{lang="en"}

::: en

| Event | Payload |
|---|---|
| `RoomEnteringEvent` | `RunState`, `Room` |
| `RoomEnteredEvent` | `RunState`, `Room` |
| `RoomExitedEvent` | `RunManager`, `Room` |
| `ActEnteringEvent` | `RunManager`, `TargetActIndex`, `DoTransition` |
| `ActEnteredEvent` | `RunState`, `CurrentActIndex` |
| `RewardsScreenContinuingEvent` | `RunManager` |

---

:::

## 房间与章节事件{lang="zh-CN"}

::: zh-CN

| 事件 | 携带数据 |
|---|---|
| `RoomEnteringEvent` | `RunState`、`Room` |
| `RoomEnteredEvent` | `RunState`、`Room` |
| `RoomExitedEvent` | `RunManager`、`Room` |
| `ActEnteringEvent` | `RunManager`、`TargetActIndex`、`DoTransition` |
| `ActEnteredEvent` | `RunState`、`CurrentActIndex` |
| `RewardsScreenContinuingEvent` | `RunManager` |

---

:::

## Combat Events{lang="en"}

::: en

| Event | Payload |
|---|---|
| `CombatStartingEvent` | `RunState`, `CombatState?` |
| `CombatEndedEvent` | `RunState`, `CombatState?`, `Room` |
| `CombatVictoryEvent` | `RunState`, `CombatState?`, `Room` |
| `SideTurnStartingEvent` | `CombatState`, `Side` |
| `SideTurnStartedEvent` | `CombatState`, `Side` |
| `CardPlayingEvent` | `CombatState`, `CardPlay` |
| `CardPlayedEvent` | `CombatState`, `CardPlay` |
| `CardDrawnEvent` | `CombatState`, `Card`, `FromHandDraw` |
| `CardDiscardedEvent` | `CombatState`, `Card` |
| `CardExhaustedEvent` | `CombatState`, `Card`, `CausedByEthereal` |
| `CardRetainedEvent` | `CombatState`, `Card` |
| `CardMovedBetweenPilesEvent` | `RunState`, `CombatState?`, `Card`, `PreviousPile`, `Source` |

### Creature Events

| Event | Payload |
|---|---|
| `CreatureDyingEvent` | `CombatState`, `Creature` |
| `CreatureDiedEvent` | `CombatState`, `Creature` |

```csharp
RitsuLibFramework.SubscribeLifecycle<CardDrawnEvent>(evt =>
{
    if (evt.Card is MyCard myCard)
        myCard.OnDrawn(evt.CombatState);
});
```

---

:::

## 战斗事件{lang="zh-CN"}

::: zh-CN

| 事件 | 携带数据 |
|---|---|
| `CombatStartingEvent` | `RunState`、`CombatState?` |
| `CombatEndedEvent` | `RunState`、`CombatState?`、`Room` |
| `CombatVictoryEvent` | `RunState`、`CombatState?`、`Room` |
| `SideTurnStartingEvent` | `CombatState`、`Side` |
| `SideTurnStartedEvent` | `CombatState`、`Side` |
| `CardPlayingEvent` | `CombatState`、`CardPlay` |
| `CardPlayedEvent` | `CombatState`、`CardPlay` |
| `CardDrawnEvent` | `CombatState`、`Card`、`FromHandDraw` |
| `CardDiscardedEvent` | `CombatState`、`Card` |
| `CardExhaustedEvent` | `CombatState`、`Card`、`CausedByEthereal` |
| `CardRetainedEvent` | `CombatState`、`Card` |
| `CardMovedBetweenPilesEvent` | `RunState`、`CombatState?`、`Card`、`PreviousPile`、`Source` |

### 生物事件

| 事件 | 携带数据 |
|---|---|
| `CreatureDyingEvent` | `CombatState`、`Creature` |
| `CreatureDiedEvent` | `CombatState`、`Creature` |

```csharp
RitsuLibFramework.SubscribeLifecycle<CardDrawnEvent>(evt =>
{
    if (evt.Card is MyCard myCard)
        myCard.OnDrawn(evt.CombatState);
});
```

---

:::

## Reward Events{lang="en"}

::: en

| Event | Payload |
|---|---|
| `GoldGainedEvent` | `Amount` |
| `GoldLostEvent` | `Amount` |
| `PotionProcuredEvent` | `Potion` |
| `PotionDiscardedEvent` | `Potion` |
| `RelicObtainedEvent` | `Relic` |
| `RelicRemovedEvent` | `Relic` |
| `RewardTakenEvent` | `Reward` |

---

:::

## 奖励事件{lang="zh-CN"}

::: zh-CN

| 事件 | 携带数据 |
|---|---|
| `GoldGainedEvent` | `Amount` |
| `GoldLostEvent` | `Amount` |
| `PotionProcuredEvent` | `Potion` |
| `PotionDiscardedEvent` | `Potion` |
| `RelicObtainedEvent` | `Relic` |
| `RelicRemovedEvent` | `Relic` |
| `RewardTakenEvent` | `Reward` |

---

:::

## Unlock Events{lang="en"}

::: en

| Event | Payload |
|---|---|
| `EpochObtainedEvent` | `Epoch` |
| `EpochRevealedEvent` | `Epoch` |
| `UnlockIncrementedEvent` | `UnlockState` |

---

:::

## 解锁事件{lang="zh-CN"}

::: zh-CN

| 事件 | 携带数据 |
|---|---|
| `EpochObtainedEvent` | `Epoch` |
| `EpochRevealedEvent` | `Epoch` |
| `UnlockIncrementedEvent` | `UnlockState` |

---

:::

## Save & Persistence Events{lang="en"}

::: en

### Profile Lifecycle

| Event | Payload |
|---|---|
| `ProfileIdInitializedEvent` | `ProfileId` |
| `ProfileSwitchingEvent` | `OldProfileId`, `NewProfileId` |
| `ProfileSwitchedEvent` | `ProfileId` |
| `ProfileDeletingEvent` | `ProfileId` |
| `ProfileDeletedEvent` | `ProfileId` |

### Save Writing

| Event | Payload |
|---|---|
| `RunSavingEvent` | `RunState` |
| `RunSavedEvent` | `RunState` |
| `ProgressSavingEvent` | — |
| `ProgressSavedEvent` | — |

### ModDataStore Data Events

Used internally by `ModDataStore`, also available for mods to react to save state changes.

| Event | Description |
|---|---|
| `ProfileDataReadyEvent` | Save data loaded — safe to read/write |
| `ProfileDataChangedEvent` | Save data changed |
| `ProfileDataInvalidatedEvent` | Save data invalidated (e.g. profile switch) |

---

:::

## 存档与持久化事件{lang="zh-CN"}

::: zh-CN

### Profile 生命周期

| 事件 | 携带数据 |
|---|---|
| `ProfileIdInitializedEvent` | `ProfileId` |
| `ProfileSwitchingEvent` | `OldProfileId`、`NewProfileId` |
| `ProfileSwitchedEvent` | `ProfileId` |
| `ProfileDeletingEvent` | `ProfileId` |
| `ProfileDeletedEvent` | `ProfileId` |

### 存档写入

| 事件 | 携带数据 |
|---|---|
| `RunSavingEvent` | `RunState` |
| `RunSavedEvent` | `RunState` |
| `ProgressSavingEvent` | — |
| `ProgressSavedEvent` | — |

### ModDataStore 数据事件

由 `ModDataStore` 内部使用，也可供 Mod 监听存档状态变化。

| 事件 | 说明 |
|---|---|
| `ProfileDataReadyEvent` | 存档数据加载完毕，可安全读写 |
| `ProfileDataChangedEvent` | 存档数据发生变更 |
| `ProfileDataInvalidatedEvent` | 存档数据失效（如切换档案） |

---

:::

## Game Over Events{lang="en"}

::: en

| Event | Payload |
|---|---|
| `GameOverScreenCreatedEvent` | `Screen` |

---

:::

## 游戏结算事件{lang="zh-CN"}

::: zh-CN

| 事件 | 携带数据 |
|---|---|
| `GameOverScreenCreatedEvent` | `Screen` |

---

:::

## Related Documents{lang="en"}

::: en

- [Getting Started](/guide/getting-started)
- [Content Authoring Toolkit](/guide/content-authoring-toolkit)
- [Persistence Guide](/guide/persistence-guide)
- [Timeline & Unlocks](/guide/timeline-and-unlocks)

:::

## 相关文档{lang="zh-CN"}

::: zh-CN

- [快速入门](/guide/getting-started)
- [内容注册规则](/guide/content-authoring-toolkit)
- [持久化设计](/guide/persistence-guide)
- [时间线与解锁](/guide/timeline-and-unlocks)

:::
