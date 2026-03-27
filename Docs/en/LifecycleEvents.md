# Lifecycle Events

This document lists all lifecycle events provided by RitsuLib, explains subscription patterns, and details replayable event behavior.

---

## Subscription Patterns

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

## Framework Events

Fired during framework initialization and profile service setup.

| Event | Replayable | Payload |
|---|---|---|
| `FrameworkInitializingEvent` | — | `FrameworkModId`, `FrameworkVersion` |
| `FrameworkInitializedEvent` | ✓ | `FrameworkModId`, `IsActive` |
| `ProfileServicesInitializingEvent` | — | — |
| `ProfileServicesInitializedEvent` | ✓ | `ProfileId` |

---

## Game Bootstrap Events

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

## Run Events

| Event | Replayable | Payload |
|---|---|---|
| `RunStartedEvent` | — | `RunState`, `IsMultiplayer`, `IsDaily` |
| `RunLoadedEvent` | — | `RunState`, `IsMultiplayer`, `IsDaily` |
| `RunEndedEvent` | — | `Run`, `IsVictory`, `IsAbandoned` |

---

## Room & Act Events

| Event | Payload |
|---|---|
| `RoomEnteringEvent` | `RunState`, `Room` |
| `RoomEnteredEvent` | `RunState`, `Room` |
| `RoomExitedEvent` | `RunManager`, `Room` |
| `ActEnteringEvent` | `RunManager`, `TargetActIndex`, `DoTransition` |
| `ActEnteredEvent` | `RunState`, `CurrentActIndex` |
| `RewardsScreenContinuingEvent` | `RunManager` |

---

## Combat Events

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

## Reward Events

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

## Unlock Events

| Event | Payload |
|---|---|
| `EpochObtainedEvent` | `Epoch` |
| `EpochRevealedEvent` | `Epoch` |
| `UnlockIncrementedEvent` | `UnlockState` |

---

## Save & Persistence Events

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

## Game Over Events

| Event | Payload |
|---|---|
| `GameOverScreenCreatedEvent` | `Screen` |

---

## Related Documents

- [Getting Started](GettingStarted.md)
- [Content Authoring Toolkit](ContentAuthoringToolkit.md)
- [Persistence Guide](PersistenceGuide.md)
- [Timeline & Unlocks](TimelineAndUnlocks.md)
