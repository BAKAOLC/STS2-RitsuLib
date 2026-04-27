# Creature Visuals & Animation

This document covers the runtime-Godot factory interfaces that let mod creatures
replace vanilla `CreateVisuals` / `GenerateAnimator`, and the backend-agnostic
animation state machine (`ModAnimStateMachine`) that can drive combat visuals with
the same trigger protocol vanilla uses for Spine `CreatureAnimator` — including
non-Spine backends (`AnimatedSprite2D`, Godot `AnimationPlayer`, cue frame sequences)
and optional Spine via `MegaSprite` (`BuildSpine`).

For content pack registration, see [Content Packs & Registries](ContentPacksAndRegistries.md).
For character assembly, see [Character & Unlock Templates](CharacterAndUnlockScaffolding.md).
For Harmony patch wiring in general, see [Patching Guide](PatchingGuide.md).

---

## Overview

Vanilla binds a `MonsterModel` or `CharacterModel` to combat visuals through:

- `Model.CreateVisuals()` — returns an `NCreatureVisuals` (the scene root under
  the combat creature node).
- `Model.GenerateAnimator(MegaSprite controller)` — returns a `CreatureAnimator`
  wrapping a Spine skeleton with an idle / hit / attack / cast / die / relaxed
  state graph.
- `NCreature.SetAnimationTrigger(trigger)` — dispatches triggers
  (`Idle`, `Attack`, `Cast`, `Hit`, `Dead`, `Revive`, ...) into that animator at
  runtime.

Mods commonly need one or more of:

- supplying `NCreatureVisuals` from code (not a path);
- replacing the Spine state graph with a mod-authored one;
- animating creatures **without** a Spine skeleton (sprite sheets, frame
  sequences, Godot `AnimationPlayer`).

RitsuLib exposes three orthogonal factory interfaces for those hooks plus a
combat `ModAnimStateMachine` factory that works with Spine or non-Spine backends.
All four interfaces are creature-agnostic (players **and** monsters) and do not
require subclassing any template.

| Interface | Purpose | Vanilla entry point |
|---|---|---|
| `IModCreatureVisualsFactory` | Build `NCreatureVisuals` from code | `CharacterModel.CreateVisuals`, `MonsterModel.CreateVisuals` |
| `IModCreatureAnimatorFactory` | Build Spine `CreatureAnimator` from code | `CharacterModel.GenerateAnimator`, `MonsterModel.GenerateAnimator` |
| `IModCreatureCombatAnimationStateMachineFactory` | Build `ModAnimStateMachine` for combat (any backend, including Spine) | `NCreature.SetAnimationTrigger` (`ModCreatureCombatAnimationPlaybackPatch`) |
| `IModCharacterMerchantAnimationStateMachineFactory` | Build `ModAnimStateMachine` for merchant / rest-site character visuals | Merchant scene setup |

The merchant factory is character-specific because monsters never appear in
merchant / rest-site scenes; the other three apply to any
`MegaCrit.Sts2.Core.Models.AbstractModel`.

---

## Creature Visuals Factory

`IModCreatureVisualsFactory` replaces the path-based
`(Character|Monster)Model.CreateVisuals` when it returns a non-null
`NCreatureVisuals`. `null` defers to `CustomVisualsPath` / vanilla resolution.

```csharp
public class MyCharacter : ModCharacterTemplate<...>
{
    // IModCreatureVisualsFactory is already implemented by the template,
    // forwarding to this protected virtual:
    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        var scene = GD.Load<PackedScene>(
            "res://MyMod/scenes/my_character/my_character_visuals.tscn");
        return scene.Instantiate<NCreatureVisuals>();
    }
}
```

For mods that do not use `ModCharacterTemplate` / `ModMonsterTemplate`, implement
the interface directly on your `CharacterModel` / `MonsterModel`:

```csharp
public class MyRawCharacter : CharacterModel, IModCreatureVisualsFactory
{
    public NCreatureVisuals? TryCreateCreatureVisuals() => ...;
}
```

The routing patches (`CharacterCreatureVisualsRuntimeFactoryPatch`,
`MonsterCreatureVisualsRuntimeFactoryPatch`) run at Harmony `Priority.First`, so
they take effect before the vanilla path-based loader.

---

## Creature Animator Factory (Spine)

`IModCreatureAnimatorFactory` replaces `GenerateAnimator` for Spine visuals.
Prefer `ModAnimStateMachines.Standard` to match the vanilla state shape:

```csharp
public class MySpineCharacter : ModCharacterTemplate<...>
{
    protected override CreatureAnimator? SetupCustomCreatureAnimator(MegaSprite controller) =>
        ModAnimStateMachines.Standard(
            controller,
            idleName: "idle_loop",
            deadName: "die",
            hitName: "hit",
            attackName: "attack",
            castName: "cast",
            relaxedName: "relaxed");
}
```

`ModAnimStateMachines.Standard` returns a `CreatureAnimator` wired with any-state
triggers for `Idle`, `Dead`, `Hit`, `Attack`, `Cast`, `Relaxed`. Terminal states
(`Dead`) leave `NextState` unset so playback does not loop back to idle.

The routing patches (`CharacterCreatureAnimatorRuntimeFactoryPatch`,
`MonsterCreatureAnimatorRuntimeFactoryPatch`) honour non-null factory output;
`null` defers to vanilla `GenerateAnimator`.

---

## Combat animation state machine

When a model implements `IModCreatureCombatAnimationStateMachineFactory` and
`TryCreateCombatAnimationStateMachine` returns a non-null `ModAnimStateMachine`,
`ModCreatureCombatAnimationPlaybackPatch` routes `NCreature.SetAnimationTrigger`
**first** into `ModAnimStateMachine.SetTrigger`, including for Spine-backed creatures.
Return `null` to defer to vanilla `CreatureAnimator` when Spine is present, or to
single-shot cue playback when there is no Spine animator.

For Spine + `ModAnimStateMachine`, you can wire `BuildSpine(MegaSprite)` in the
factory; keep supplying a `CreatureAnimator` from `GenerateAnimator` for bounds
subscriptions and align `Revive` triggers with vanilla `StartReviveAnim` gating when
possible (see interface XML remarks).

### Opting in

```csharp
public class MyWolf : ModMonsterTemplate
{
    // IModCreatureCombatAnimationStateMachineFactory is implemented by the
    // template, forwarding to this protected virtual:
    protected override ModAnimStateMachine? SetupCustomCombatAnimationStateMachine(
        Node visualsRoot, MonsterModel monster)
    {
        if (visualsRoot is not MyWolfVisuals wolfVisuals)
            return null;

        var backend = new AnimatedSprite2DBackend(wolfVisuals.GetAnimatedSprite());

        return ModAnimStateMachineBuilder.Create()
            .AddState("idle", loop: true).AsInitial().Done()
            .AddState("attack").WithNext("idle").Done()
            .AddState("hurt").WithNext("idle").Done()
            .AddState("die").Done()                     // terminal: no NextState
            .AddAnyState("Idle",   "idle")
            .AddAnyState("Attack", "attack")
            .AddAnyState("Hit",    "hurt")
            .AddAnyState("Dead",   "die")
            .Build(backend);
    }
}
```

Equivalent if you do not use a template:

```csharp
public class MyRawMonster : MonsterModel, IModCreatureCombatAnimationStateMachineFactory
{
    public ModAnimStateMachine? TryCreateCombatAnimationStateMachine(Node visualsRoot)
        => /* same builder code */;
}
```

### Routing behaviour

`ModCreatureCombatAnimationPlaybackPatch` is a prefix on
`NCreature.SetAnimationTrigger`:

1. Resolve the creature model (`Entity.Player?.Character` or `Entity.Monster`),
   build/cache a state machine from `TryCreateCombatAnimationStateMachine` (and still
   honour the obsolete `IModNonSpineAnimationStateMachineFactory` name).
2. If a non-null machine exists, dispatch via `ModAnimStateMachine.SetTrigger` and
   skip the vanilla `_spineAnimator` path (including when Spine visuals are present).
3. Otherwise, if the creature has Spine visuals, run vanilla `CreatureAnimator`.
4. Otherwise, fall back to single-shot cue playback
   (`ModCreatureVisualPlayback.TryPlayFromCreatureAnimatorTrigger`).

State machines are **cached per visuals root** with a
`ConditionalWeakTable<Node, StateMachineSlot>`, so the factory runs at most once
per combat lifetime and is automatically released when the visuals node is
freed.

### Shorthand: `ModAnimStateMachines.StandardCue`

For visuals that follow the vanilla idle / dead / hit / attack / cast / relaxed
shape, `ModAnimStateMachines.StandardCue` builds the state graph for you. It
uses `CompositeBackendFactory` to pick the best backend per state (cue frame
sequences first, Godot `AnimationPlayer` or `AnimatedSprite2D` if they resolve
the animation id) and returns a ready-to-use `ModAnimStateMachine`.

### Recommended pattern: stable root + child-form switching

For in-combat "model/form switching", prefer keeping one persistent
`NCreatureVisuals` root and mounting multiple child forms (`FormA`, `FormB`, ...).
Switch forms by changing the active animation backend instead of rebuilding the root.

RitsuLib provides `FormSwitchingAnimationBackend` for this: wrap one
`IAnimationBackend` per form, then call `SwitchForm(formId)` at runtime.
With `replayCurrent: true`, it attempts to replay the current logical animation id
on the new form.

```csharp
var forms = new Dictionary<string, IAnimationBackend>(StringComparer.Ordinal)
{
    ["base"] = new AnimatedSprite2DBackend(baseSprite),
    ["alt"] = new AnimatedSprite2DBackend(altSprite),
};

var formBackend = new FormSwitchingAnimationBackend(forms, initialFormId: "base");

var machine = ModAnimStateMachineBuilder.Create()
    .AddState("idle", loop: true).AsInitial().Done()
    .AddState("attack").WithNext("idle").Done()
    .AddAnyState("Idle", "idle")
    .AddAnyState("Attack", "attack")
    .Build(formBackend);

// Switch form on gameplay event without rebuilding visuals root
formBackend.SwitchForm("alt", replayCurrent: true);
```

Benefits:

- no `NCreatureVisuals` rebuild, so lifecycle wiring remains stable;
- trigger flow stays unified (`SetAnimationTrigger` -> `ModAnimStateMachine`);
- easy to standardise across mods: form is just `formId -> backend`.

---

## Animation Backends

`IAnimationBackend` is the uniform driver surface consumed by
`ModAnimStateMachine`. Each backend wraps a Godot animation subsystem and
reports `Started` / `Completed` / `Interrupted` events.

| Backend | Drives | Used for |
|---|---|---|
| `AnimatedSprite2DBackend` | `AnimatedSprite2D` | Frame-based sprite animation |
| `GodotAnimationPlayerBackend` | `AnimationPlayer` | Godot `.tres` animation library |
| `CueAnimationBackend` | `VisualCueSet` (cue frame sequences, cue textures) | Per-cue static textures / sequence playback |
| `SpineAnimationBackend` | `MegaSprite` | Spine skeletal animation |
| `CompositeAnimationBackend` | Any mix | Multi-backend dispatch (one state plays via sprite, another via animation player, etc.) |
| `FormSwitchingAnimationBackend` | Multiple child backends (selected by form id) | In-combat form switching under one visuals root |

### Event contract

| Event | When it fires |
|---|---|
| `Started(id)` | Playback for `id` has started |
| `Completed(id)` | One-shot finished, or a loop cycle ended |
| `Interrupted(id)` | Playback was replaced before completion |

`ModAnimState.NextState` advances on `Completed`, so backends must emit it
accurately for non-looping states (`attack -> idle` etc.).

### Queue semantics

`Queue(id, loop)` is semantically "play this after the currently active
animation finishes". Backends implement it differently:

| Backend | `Queue` behaviour |
|---|---|
| `SpineAnimationBackend` | True native Spine queue (`AddAnimation` on the track) |
| `AnimatedSprite2DBackend` | Stores pending id, plays on next `animation_finished` signal |
| `GodotAnimationPlayerBackend` | Uses `AnimationPlayer.Queue` |
| `CueAnimationBackend` | Stores pending id, plays on sequence completion |

In all cases, calling `Play` clears any pending queued animation.

### `Stop()` and cross-backend transitions

`IAnimationBackend.Stop()` (default interface method) halts the backend
**silently** — it neither fires `Completed` nor `Interrupted`, and clears any
queued animation. The primary consumer is `CompositeAnimationBackend` when
transitioning from one child backend to another:

1. The new state's backend differs from the active one.
2. `Interrupted` is fired for the outgoing animation.
3. The outgoing backend's `Stop()` is called to clear its internal state.
4. The incoming backend's `Play` runs.

Without `Stop()`, the outgoing backend could keep emitting `Completed` /
`Interrupted` events bound to its old state id and confuse the state machine.

---

## Lifecycle Trigger Patches

Vanilla `NCreature.StartDeathAnim` and `NCreature.StartReviveAnim` dispatch the
`Dead` / `Revive` triggers only when `_spineAnimator != null`. Non-Spine
creatures therefore never receive those triggers, so a custom state machine
never sees the death animation play when the run is abandoned or the player
dies.

RitsuLib fixes this with two Postfix patches:

- `NCreatureNonSpineDeathAnimationTriggerPatch` — dispatches `Dead` after
  `StartDeathAnim`.
- `NCreatureNonSpineReviveAnimationTriggerPatch` — dispatches `Revive` after
  `StartReviveAnim`.

### Scope gate

The `Dead` postfix only runs for **non-Spine** creatures whose model opts into the
RitsuLib combat animation pipeline (`IModCreatureCombatAnimationStateMachineFactory`,
the legacy `IModNonSpineAnimationStateMachineFactory`, or player
`IModCharacterAssetOverrides`), filling the gap where vanilla never calls
`SetAnimationTrigger("Dead")` when `_spineAnimator` is null.

The `Revive` postfix uses the same non-Spine opt-in, **and** additionally covers
Spine-backed creatures when a cached combat state machine declares `Revive` but the
vanilla `CreatureAnimator` does not — so `Revive` can still be dispatched without
duplicating when the animator already exposes `Revive`.

---

## Migration & Deprecation

Two factory interfaces were originally named after the creature kind. They are
now unified and the old names marked `[Obsolete]`:

| New (preferred) | Obsolete aliases |
|---|---|
| `IModCreatureVisualsFactory` | `IModMonsterCreatureVisualsFactory`, `IModCharacterCreatureVisualsFactory` |
| `IModCreatureAnimatorFactory` | `IModCharacterCreatureAnimatorFactory` |
| `IModCreatureCombatAnimationStateMachineFactory` | `IModNonSpineAnimationStateMachineFactory` (method `TryCreateNonSpineAnimationStateMachine`) |

Template subclasses: rename `SetupCustomNonSpineAnimationStateMachine` to
`SetupCustomCombatAnimationStateMachine`; the old hook remains as an `[Obsolete]`
forwarder.

### Compatibility guarantees

- The routing patches check both the new and the obsolete interfaces on each
  call, so mods that implement only the old interface continue to work without
  any code change.
- `ModCharacterTemplate` / `ModMonsterTemplate` implement **both** the new and
  the obsolete aliases and forward to the same protected virtual hooks, so
  external `is IModCharacterCreatureVisualsFactory` checks against a template
  subclass still succeed.
- Implementing an obsolete interface emits compiler warning **CS0618** to guide
  migration. No runtime warning or behavioural change.

### Migration steps

1. Replace the old interface name in the `: Interfaces` list and in explicit
   interface implementations:
   - `IModMonsterCreatureVisualsFactory` → `IModCreatureVisualsFactory`
   - `IModCharacterCreatureVisualsFactory` → `IModCreatureVisualsFactory`
   - `IModCharacterCreatureAnimatorFactory` → `IModCreatureAnimatorFactory`
   - `IModNonSpineAnimationStateMachineFactory` → `IModCreatureCombatAnimationStateMachineFactory`
2. The method signatures (`TryCreateCreatureVisuals()`,
   `TryCreateCreatureAnimator(MegaSprite)`) are unchanged; only the declaring
   interface name differs.
3. Combat state machine: rename `TryCreateNonSpineAnimationStateMachine` to
   `TryCreateCombatAnimationStateMachine`; template subclasses rename
   `SetupCustomNonSpineAnimationStateMachine` to `SetupCustomCombatAnimationStateMachine`.
4. Rebuild. CS0618 warnings disappear.

No migration is required if you only subclass the templates and override the
protected virtual hooks (`TryCreateCreatureVisuals`,
`SetupCustomCreatureAnimator`) without using the combat state machine hook.

---

## Summary Cheat-sheet

```text
Goal                                          Interface to implement
---------------------------------------------------------------------------
Replace CreateVisuals (players or monsters)   IModCreatureVisualsFactory
Replace Spine GenerateAnimator                IModCreatureAnimatorFactory
Drive combat state machine (Spine or not)     IModCreatureCombatAnimationStateMachineFactory
Drive merchant / rest-site state machine      IModCharacterMerchantAnimationStateMachineFactory
```

All four interfaces are honoured whether you inherit `ModCharacterTemplate` /
`ModMonsterTemplate` or implement them directly on your `CharacterModel` /
`MonsterModel`. The routing patches always run at Harmony `Priority.First` and
defer to vanilla when the factory returns `null`.
