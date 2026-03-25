# Custom Events

This document explains how custom events fit into the base game's event pipeline, and how RitsuLib exposes that pipeline through its registration APIs.

It covers three content shapes:

- shared events via `SharedEvent<TEvent>()`
- act-specific events via `ActEvent<TAct, TEvent>()`
- ancients via `SharedAncient<TAncient>()` / `ActAncient<TAct, TAncient>()`

---

## Runtime Model

RitsuLib does not replace the game's event flow.
It extends the existing flow by registering additional event and ancient models at the points where the base game already consumes them.

The relevant runtime responsibilities in `sts-2-source` are:

- `ActModel.GenerateRooms(...)` builds event pools from shared and act-local sources
- `RoomSet.EnsureNextEventIsValid(...)` filters by `IsAllowed(runState)` and visited-state rules
- `EventRoom.Enter(...)` preloads assets, creates the mutable event instance, and builds the event UI
- `EventModel.GetAssetPaths(...)` determines which assets must be ready before the room opens

Within that model, RitsuLib appends registered content to the existing lists used by the game:

- shared events are added to the shared event collections
- act events are added to the selected act's event list
- ancients are added to the corresponding shared or act-local ancient lists

For authors, the practical consequence is simple:

1. define a valid `EventModel` or `AncientEventModel` subtype
2. register it before content registration freezes

---

## Minimal Normal Event

For most mod events, prefer `ModEventTemplate` over a raw `EventModel` subclass.

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

A minimal event model should do three things correctly:

- implement `GenerateInitialOptions()`
- advance the event state, or finish the event, inside option callbacks
- keep localization keys aligned with the final `ModelId.Entry`

---

## Registration

### Shared Event

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SharedEvent<MyFirstEvent>()
    .Apply();
```

This makes the event part of the shared event pool.

### Act Event

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .ActEvent<MyAct, MyFirstEvent>()
    .Apply();
```

This makes the event available only in the selected act.

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

## Localization Keys

After registration, a RitsuLib event receives a fixed public `ModelId.Entry` in the form:

```text
<MODID>_EVENT_<TYPENAME>
```

For `MyMod` and `MyFirstEvent`, that becomes:

```text
MY_MOD_EVENT_MY_FIRST_EVENT
```

A minimal normal-event localization block typically looks like this:

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

What matters here is consistency:

- event title and page text are resolved through `Id.Entry`
- option keys should also be generated from that same final identifier

---

## Why `ModEventTemplate` And `ModAncientEventTemplate` Exist

The base game contains an implementation mismatch that is usually harmless for vanilla content, but becomes important for registered mod events.

The mismatch is:

- vanilla `EventModel.InitialOptionKey(...)` and the internal option-key helpers use `GetType().Name`
- event title lookup, page descriptions, and `GameInfoOptions` use `Id.Entry`

For base-game events those values often happen to match.
For RitsuLib-registered events they usually do not.

That can lead to one part of the event resolving text under something like:

```text
MY_FIRST_EVENT...
```

while the actual event title and pages live under:

```text
MY_MOD_EVENT_MY_FIRST_EVENT...
```

To keep those lookups aligned, RitsuLib provides:

- `ModEventTemplate`
- `ModAncientEventTemplate`

Their helper methods generate option keys from the final registered `Id.Entry` rather than the raw CLR type name.

---

## `IsAllowed`

Override `IsAllowed(RunState runState)` when the event should only appear in some runs:

```csharp
public override bool IsAllowed(RunState runState)
{
    return !runState.VisitedEventIds.Contains(Id);
}
```

At runtime, the game rotates through the available event pool until it finds an event that:

- returns `true` from `IsAllowed(...)`
- has not already been visited in the current run

So `IsAllowed` should describe run-time availability conditions, not registration-time setup.

---

## Custom Event Scenes

If the default event layout is not appropriate, return a custom layout type:

```csharp
public override EventLayoutType LayoutType => EventLayoutType.Custom;
```

The game will then load:

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

This is required because `EventModel.SetNode(...)` hard-casts custom layouts to `ICustomEventNode`.

---

## Asset Preloading

By default, normal events preload:

- the layout scene
- `res://images/events/<event-id-lower>.png`
- optional `res://scenes/vfx/events/<event-id-lower>_vfx.tscn`

By default, ancients preload:

- the layout scene
- `res://scenes/events/background_scenes/<event-id-lower>.tscn`

If an event needs additional assets, override `GetAssetPaths(IRunState runState)` and append those paths.

---

## Minimal Ancient Example

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
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.ACCEPT.description"));
        return Task.CompletedTask;
    }
}
```

The same identity and localization guidance applies here: keep option keys, page keys, and final registered `Id.Entry` aligned.

---

## Related Docs

- [Content Authoring Toolkit](ContentAuthoringToolkit.md)
- [Content Packs & Registries](ContentPacksAndRegistries.md)
- [Localization & Keywords](LocalizationAndKeywords.md)
