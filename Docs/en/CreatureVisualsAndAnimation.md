# Creature Visuals & Animation

This document covers the runtime-Godot factory interfaces that let mod creatures
replace vanilla `CreateVisuals` / `GenerateAnimator`, and the backend-agnostic
animation state machine (`ModAnimStateMachine`) that drives non-Spine combat
visuals (`AnimatedSprite2D`, Godot `AnimationPlayer`, or cue frame sequences)
through the same trigger protocol Spine creatures use.

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

RitsuLib exposes three orthogonal factory interfaces for those hooks and one
state machine abstraction for the non-Spine case. All four interfaces are
creature-agnostic (players **and** monsters) and do not require subclassing any
template.

| Interface | Purpose | Vanilla entry point |
|---|---|---|
| `IModCreatureVisualsFactory` | Build `NCreatureVisuals` from code | `CharacterModel.CreateVisuals`, `MonsterModel.CreateVisuals` |
| `IModCreatureAnimatorFactory` | Build Spine `CreatureAnimator` from code | `CharacterModel.GenerateAnimator`, `MonsterModel.GenerateAnimator` |
| `IModNonSpineAnimationStateMachineFactory` | Build `ModAnimStateMachine` for non-Spine visuals | `NCreature.SetAnimationTrigger` (routing patch) |
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

## Non-Spine State Machine

For creatures whose combat visuals are **not** Spine (no `MegaSprite` controller),
implement `IModNonSpineAnimationStateMachineFactory` and return a
`ModAnimStateMachine` bound to the visuals root. The
`ModCreatureNonSpineAnimationPlaybackPatch` routes
`NCreature.SetAnimationTrigger(trigger)` into `ModAnimStateMachine.SetTrigger`,
so the non-Spine path receives the **same trigger stream** as Spine creatures.

### Opting in

```csharp
public class MyWolf : ModMonsterTemplate
{
    // IModNonSpineAnimationStateMachineFactory is already implemented by the
    // template, forwarding to this protected virtual:
    protected override ModAnimStateMachine? SetupCustomNonSpineAnimationStateMachine(
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
public class MyRawMonster : MonsterModel, IModNonSpineAnimationStateMachineFactory
{
    public ModAnimStateMachine? TryCreateNonSpineAnimationStateMachine(Node visualsRoot)
        => /* same builder code */;
}
```

### Routing behaviour

`ModCreatureNonSpineAnimationPlaybackPatch` is a prefix on
`NCreature.SetAnimationTrigger`:

1. If the creature has a Spine animator, skip (vanilla path runs).
2. Look up the creature's model (`Entity.Player?.Character` or `Entity.Monster`).
3. If either implements `IModNonSpineAnimationStateMachineFactory` and
   returns a non-null state machine, dispatch the trigger via
   `ModAnimStateMachine.SetTrigger` and return.
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

The patches are **opt-in**: they only fire when the creature has no Spine
animator and the model opts into the RitsuLib visuals pipeline. Specifically,
`NonSpineAnimationTriggerScope.AppliesTo(NCreature)` returns `true` only when
**one** of the following holds for the creature's model:

| Model slot | Interface | Notes |
|---|---|---|
| `Entity.Player?.Character` | `IModNonSpineAnimationStateMachineFactory` | State machine path |
| `Entity.Monster` | `IModNonSpineAnimationStateMachineFactory` | State machine path |
| `Entity.Player?.Character` | `IModCharacterAssetOverrides` | Cue-playback fallback (player-only) |

Vanilla creatures and mods that do not opt into RitsuLib visuals are never
affected. The gate is identical for the `Dead` and `Revive` patches.

---

## Migration & Deprecation

Two factory interfaces were originally named after the creature kind. They are
now unified and the old names marked `[Obsolete]`:

| New (preferred) | Obsolete aliases |
|---|---|
| `IModCreatureVisualsFactory` | `IModMonsterCreatureVisualsFactory`, `IModCharacterCreatureVisualsFactory` |
| `IModCreatureAnimatorFactory` | `IModCharacterCreatureAnimatorFactory` |

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
2. The method signatures (`TryCreateCreatureVisuals()`,
   `TryCreateCreatureAnimator(MegaSprite)`) are unchanged; only the declaring
   interface name differs.
3. Rebuild. CS0618 warnings disappear.

No migration is required if you only subclass the templates and override the
protected virtual hooks (`TryCreateCreatureVisuals`,
`SetupCustomCreatureAnimator`); those hooks are unchanged.

---

## Summary Cheat-sheet

```text
Goal                                          Interface to implement
---------------------------------------------------------------------------
Replace CreateVisuals (players or monsters)   IModCreatureVisualsFactory
Replace Spine GenerateAnimator                IModCreatureAnimatorFactory
Drive a non-Spine state machine               IModNonSpineAnimationStateMachineFactory
Drive merchant / rest-site state machine      IModCharacterMerchantAnimationStateMachineFactory
```

All four interfaces are honoured whether you inherit `ModCharacterTemplate` /
`ModMonsterTemplate` or implement them directly on your `CharacterModel` /
`MonsterModel`. The routing patches always run at Harmony `Priority.First` and
defer to vanilla when the factory returns `null`.
