# Custom Events

This guide explains custom event authoring by relating the event runtime flow in `sts-2-source` to the registration APIs provided by RitsuLib.

It covers three cases:

- normal shared events via `SharedEvent<TEvent>()`
- act-specific events via `ActEvent<TAct, TEvent>()`
- ancients via `SharedAncient<TAncient>()` / `ActAncient<TAct, TAncient>()`

---

## How the game loads events

In `sts-2-source`, events enter the game through several key access points:

- `ActModel.GenerateRooms(...)` merges `AllEvents` with `ModelDb.AllSharedEvents`
- `RoomSet.EnsureNextEventIsValid(...)` skips events that fail `IsAllowed(runState)` or were already visited
- `EventRoom.Enter(...)` preloads assets, creates a mutable event instance, and builds the event UI
- `EventModel.GetAssetPaths(...)` decides which room assets must be preloaded

RitsuLib does not replace that pipeline. Instead, it appends registered content to those existing access points:

- shared events are appended to `ModelDb.AllSharedEvents` and `ModelDb.AllEvents`
- act events are appended to each act's `AllEvents` via dynamic patches
- ancients are appended similarly to shared and act ancient lists

In practice, the essential steps are:

1. write a valid `EventModel` / `AncientEventModel` subclass
2. register it before content registration freezes

---

## Minimal normal event

Prefer inheriting from `ModEventTemplate` rather than directly from `EventModel`.

```csharp
using MegaCrit.Sts2.Core.Events;
using STS2RitsuLib.Scaffolding.Content;

public sealed class MyFirstEvent : ModEventTemplate
{
    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return
        [
            new EventOption(this, Accept, InitialOptionKey("ACCEPT")),
            new EventOption(this, Leave, InitialOptionKey("LEAVE")),
        ];
    }

    private Task Accept()
    {
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.ACCEPT.description"));
        return Task.CompletedTask;
    }

    private Task Leave()
    {
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.LEAVE.description"));
        return Task.CompletedTask;
    }
}
```

A minimal event model should satisfy the following requirements:

- implement `GenerateInitialOptions()`
- advance the event state or call `SetEventFinished(...)` from option callbacks
- keep localization keys aligned with the final `ModelId.Entry`

---

## Registration

### Shared event

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SharedEvent<MyFirstEvent>()
    .Apply();
```

This appends the event to the shared pool used across acts.

### Act event

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .ActEvent<MyAct, MyFirstEvent>()
    .Apply();
```

This appends the event only to the chosen act.

### Ancient

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SharedAncient<MyAncient>()
    .Apply();
```

Or:

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .ActAncient<MyAct, MyAncient>()
    .Apply();
```

---

## Localization keys

After RitsuLib registration, an event gets a fixed public entry in the form:

```text
<MODID>_EVENT_<TYPENAME>
```

For `MyMod` + `MyFirstEvent`, that becomes:

```text
MY_MOD_EVENT_MY_FIRST_EVENT
```

A minimal normal-event localization block usually looks like this:

```json
{
  "MY_MOD_EVENT_MY_FIRST_EVENT.title": "A Strange Spring",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.INITIAL.description": "A glowing spring waits by the roadside.",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.INITIAL.options.ACCEPT.title": "Drink",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.INITIAL.options.ACCEPT.description": "This might go well.",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.INITIAL.options.LEAVE.title": "Leave",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.INITIAL.options.LEAVE.description": "Do not risk it.",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.ACCEPT.description": "You feel renewed.",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.LEAVE.description": "You walk away."
}
```

Two details are especially important here:

- event title and body text are looked up by `Id.Entry`
- `ModEventTemplate.InitialOptionKey(...)` also generates option keys from `Id.Entry`

---

## Why `ModEventTemplate` exists

There is an implementation mismatch in the base-game helper methods:

- vanilla `EventModel.InitialOptionKey(...)` / internal `OptionKey(...)` use `GetType().Name`
- event title lookup, page descriptions, and `GameInfoOptions` use `Id.Entry`
- for vanilla content, those values usually match
- for RitsuLib-registered mod events, they usually do not

As a result, a raw `EventModel` subclass can easily generate option keys under something like `MY_FIRST_EVENT...` while the event body and title live under `MY_MOD_EVENT_MY_FIRST_EVENT...`, producing inconsistent localization lookups.

To resolve that mismatch, RitsuLib now provides:

- `ModEventTemplate`
- `ModAncientEventTemplate`

Their `InitialOptionKey(...)` / `ModOptionKey(...)` helpers stay aligned with the final public `Id.Entry`.

---

## `IsAllowed`

If an event should only appear in some runs, override `IsAllowed(RunState runState)`:

```csharp
public override bool IsAllowed(RunState runState)
{
    return !runState.VisitedEventIds.Contains(Id);
}
```

The game rotates the pool in `RoomSet.EnsureNextEventIsValid(...)` until it finds an event that:

- returns `true` from `IsAllowed(...)`
- has not already been visited in the current run

Accordingly, `IsAllowed` should describe run-time availability rather than registration-time setup.

---

## Custom event scenes

If the default event layout is not appropriate, return a custom layout type:

```csharp
public override EventLayoutType LayoutType => EventLayoutType.Custom;
```

The game then loads:

```text
res://scenes/events/custom/<event-id-lower>.tscn
```

For example:

```text
res://scenes/events/custom/my_mod_event_my_first_event.tscn
```

The scene root must implement `ICustomEventNode` and provide at least:

- `Initialize(EventModel eventModel)`
- `CurrentScreenContext`

`EventModel.SetNode(...)` hard-casts custom layouts to `ICustomEventNode`, so implementing that interface is mandatory.

---

## Asset preloading

Normal events preload, by default:

- the layout scene
- `res://images/events/<event-id-lower>.png`
- optional `res://scenes/vfx/events/<event-id-lower>_vfx.tscn`

Ancients preload, by default:

- the layout scene
- `res://scenes/events/background_scenes/<event-id-lower>.tscn`

If the event requires additional assets, override `GetAssetPaths(IRunState runState)` and append those paths.

---

## Minimal ancient example

```csharp
using MegaCrit.Sts2.Core.Entities.Ancients;
using MegaCrit.Sts2.Core.Events;
using STS2RitsuLib.Scaffolding.Content;

public sealed class MyAncient : ModAncientEventTemplate
{
    protected override AncientDialogueSet DefineDialogues()
    {
        return new AncientDialogueSet();
    }

    public override IEnumerable<EventOption> AllPossibleOptions =>
    [
        new EventOption(this, Accept, InitialOptionKey("ACCEPT")),
    ];

    protected override IReadOnlyList<EventOption> GenerateInitialOptions()
    {
        return AllPossibleOptions.ToArray();
    }

    private Task Accept()
    {
        Done();
        return Task.CompletedTask;
    }
}
```

Compared with a normal event, ancients add a few requirements:

- `LocTable` is `ancients`
- you must implement `DefineDialogues()`
- finishing should usually go through `Done()` so ancient history is recorded correctly

If you only want to add dialogue for a custom character to an existing ancient, use `AncientDialogueLocalization` instead of creating a new ancient model. See `LocalizationAndKeywords.md`.

---

## Using unlock rules with events

Events can also be gated behind epochs:

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SharedEvent<MyFirstEvent>()
    .RequireEpoch<MyFirstEvent, MyEpoch>()
    .Apply();
```

RitsuLib filters generated act event pools after room generation.

This area also included a framework-level stability gap:

- previously, if unlock filtering removed every generated event for an act, later room selection could fail at run time
- RitsuLib now preserves the original generated pool and logs a warning instead of leaving the act with no available events

Even with that safeguard, the preferred content design is still:

- do not lock every possible event in an act
- keep at least one event available at all times

---

## Recommended Practices

- inherit normal events from `ModEventTemplate`
- inherit ancients from `ModAncientEventTemplate`
- generate option keys through `InitialOptionKey(...)` / `ModOptionKey(...)`
- for custom layouts, make the scene root implement `ICustomEventNode`
- if you add epoch gating, leave at least one event available in each pool

---

## Related docs

- [Content Packs & Registries](ContentPacksAndRegistries.md)
- [Timeline & Unlocks](TimelineAndUnlocks.md)
- [Localization & Keywords](LocalizationAndKeywords.md)
- [Godot Scene Authoring](GodotSceneAuthoring.md)
