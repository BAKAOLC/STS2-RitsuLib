# Custom Events

This document explains how to plug custom events into the game's event pipeline using RitsuLib.

It covers three registration shapes:

- shared events: `SharedEvent<TEvent>()`
- act-specific events: `ActEvent<TAct, TEvent>()`
- ancients: `SharedAncient<TAncient>()` / `ActAncient<TAct, TAncient>()`

---

## Base-game event pipeline

> The following is the game's own event runtime flow, to help you see where RitsuLib registration ultimately takes effect.

Event generation and execution in the game involve these stages:

| Stage | Game type | Role |
|---|---|---|
| Candidate generation | `ActModel.GenerateRooms(...)` | Builds the candidate list from the act-local event pool and the `ModelDb.AllSharedEvents` shared pool |
| Filtering | `RoomSet.EnsureNextEventIsValid(...)` | Filters using `IsAllowed(runState)` and visited-state records |
| Entry | `EventRoom.Enter(...)` | Preloads assets, creates the mutable instance, and builds the event UI |
| Assets | `EventModel.GetAssetPaths(...)` | Supplies asset paths that must be ready before the event opens |

---

## RitsuLib registration

RitsuLib does not replace the flow above. At registration time it adds mod events into the same entry points the base game already uses:

- shared events are appended to `ModelDb.AllSharedEvents`
- act events are appended to the selected act's event list
- ancients are appended to the corresponding shared or act-local ancient lists

For authors, the work boils down to two steps:

1. define a valid `EventModel` or `AncientEventModel` subtype
2. register it before content registration freezes

---

## Minimal normal event

Prefer inheriting `ModEventTemplate` rather than subclassing the base `EventModel` directly (see below).

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

A minimal usable event should:

- implement `GenerateInitialOptions()`
- advance or finish the event inside option callbacks
- keep localization keys aligned with the final `ModelId.Entry`

---

## Registration

### Shared event

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SharedEvent<MyFirstEvent>()
    .Apply();
```

### Act-specific event

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .ActEvent<MyAct, MyFirstEvent>()
    .Apply();
```

### Ancient

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SharedAncient<MyAncient>()
    .Apply();
```

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .ActAncient<MyAct, MyAncient>()
    .Apply();
```

---

## Localization keys

After registration through RitsuLib, the event's `ModelId.Entry` follows a fixed format:

```text
<MODID>_EVENT_<TYPENAME>
```

For `MyMod` and `MyFirstEvent`:

```text
MY_MOD_EVENT_MY_FIRST_EVENT
```

Example localization block for a minimal normal event:

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

The key requirement is consistency: titles, page text, and option keys should all be derived from the same final `Id.Entry`.

---

## Why use `ModEventTemplate`

> The following explains a behavioral detail of the base game's `EventModel`.

In the base game, `EventModel.InitialOptionKey(...)` and internal option-key helpers build key prefixes from `GetType().Name` (via `Slugify`), while titles, page text, and related lookups use `Id.Entry`.

For vanilla events those two usually match. For events registered through RitsuLib, `GetType().Name` and `Id.Entry` differ, so some text lookups use a different key prefix than the rest.

`ModEventTemplate` and `ModAncientEventTemplate` use `protected new` to hide the base `InitialOptionKey` helpers and generate option keys from the final registered `Id.Entry`, removing that mismatch.

---

## `IsAllowed`

> The following describes the base game's event filtering mechanism.

Override `IsAllowed(RunState runState)` when the event should only appear in some runs:

```csharp
public override bool IsAllowed(RunState runState)
{
    return !runState.VisitedEventIds.Contains(Id);
}
```

At runtime, the game walks the candidate pool until it finds an event that satisfies both:

- `IsAllowed(...)` returns `true`
- the event has not been visited in the current run yet

`IsAllowed` expresses whether the event may appear in the current run, not registration-time setup.

---

## Custom event scene

> The following describes the base game's custom event layout mechanism.

Return a custom layout type:

```csharp
public override EventLayoutType LayoutType => EventLayoutType.Custom;
```

The game then loads:

```text
res://scenes/events/custom/<event-id-lower>.tscn
```

The scene root must implement `ICustomEventNode` and provide at least `Initialize(EventModel)` and `CurrentScreenContext`.

---

## Asset preloading

> The following describes the base game's rules for event asset preloading.

Normal events preload by default:

- the layout scene
- `res://images/events/<event-id-lower>.png`
- optional `res://scenes/vfx/events/<event-id-lower>_vfx.tscn`

Ancients preload by default:

- the layout scene
- `res://scenes/events/background_scenes/<event-id-lower>.tscn`

Override `GetAssetPaths(IRunState runState)` to append paths when you need extra assets.

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
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.ACCEPT.description"));
        return Task.CompletedTask;
    }
}
```

The same principle applies: keep option keys, page keys, and the final registered `Id.Entry` aligned.

---

## Add conditional options to a target ancient

If you want to append extra options to an **existing ancient** (including vanilla ancients) without modifying the ancient class itself, register `ModAncientOptionRule`.

```csharp
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models.Events;
using STS2RitsuLib.Scaffolding.Ancients.Options;

RitsuLibFramework.CreateContentPack("MyMod")
    .AncientOption<Neow>(
        new ModAncientOptionRule(ancient =>
            [
                new EventOption(
                    ancient,
                    () =>
                    {
                        ancient.SetEventFinished(ancient.L10NLookup("NEOW.pages.DONE.description"));
                        return Task.CompletedTask;
                    },
                    "NEOW.pages.INITIAL.options.MYMOD_BONUS")
            ])
        {
            Condition = ancient => ancient.Owner?.Character is MyCharacter,
            Priority = 100,
        })
    .Apply();
```

Rule fields:

- `Condition`: optional gate; option injection runs only when this returns `true`
- `Priority`: execution order (higher runs first)
- `SkipDuplicateTextKeys`: default `true`; skips duplicate `TextKey` options

You can also register directly via framework API:

```csharp
RitsuLibFramework.RegisterAncientOption<Neow>(
    "MyMod",
    new ModAncientOptionRule(...)
);
```

---

## Related docs

- [Content Authoring Toolkit](ContentAuthoringToolkit.md)
- [Content Packs & Registries](ContentPacksAndRegistries.md)
- [Localization & Keywords](LocalizationAndKeywords.md)
