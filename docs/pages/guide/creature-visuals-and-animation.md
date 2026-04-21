---
title:
  en: Creature Visuals & Animation
  zh-CN: 生物体视觉与动画
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

This document covers the runtime-Godot factory interfaces that let mod creatures
replace vanilla `CreateVisuals` / `GenerateAnimator`, and the backend-agnostic
animation state machine (`ModAnimStateMachine`) that drives non-Spine combat
visuals (`AnimatedSprite2D`, Godot `AnimationPlayer`, or cue frame sequences)
through the same trigger protocol Spine creatures use.

For content pack registration, see [Content Packs & Registries](content-packs-and-registries.md).
For character assembly, see [Character & Unlock Templates](character-and-unlock-scaffolding.md).
For Harmony patch wiring in general, see [Patching Guide](patching-guide.md).

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

本文介绍一组 mod 可以接入的运行时 Godot 工厂接口（替换原版
`CreateVisuals` / `GenerateAnimator`），以及后端无关的动画状态机
`ModAnimStateMachine`。后者让非 Spine 的战斗视觉（`AnimatedSprite2D`、Godot
`AnimationPlayer`、cue 帧序列）通过和 Spine 生物**相同的**触发协议驱动动画。

内容包注册见 [内容包与注册器](content-packs-and-registries.md)。
角色装配见 [角色与解锁模板](character-and-unlock-scaffolding.md)。
Harmony 补丁机制见 [补丁系统](patching-guide.md)。

---

:::

## Overview{lang="en"}

::: en

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

:::

## 概览{lang="zh-CN"}

::: zh-CN

原版将 `MonsterModel` / `CharacterModel` 绑定到战斗视觉的三个入口：

- `Model.CreateVisuals()` — 返回一个 `NCreatureVisuals`（战斗生物节点下的视觉
  根场景）。
- `Model.GenerateAnimator(MegaSprite controller)` — 返回一个 `CreatureAnimator`，
  内部封装 Spine 骨骼及 idle / hit / attack / cast / die / relaxed 状态图。
- `NCreature.SetAnimationTrigger(trigger)` — 在运行时把触发器（`Idle`、
  `Attack`、`Cast`、`Hit`、`Dead`、`Revive` 等）派发给这个 animator。

Mod 常见的需求至少包括以下一种：

- 用代码供给 `NCreatureVisuals`（而不是只用路径）；
- 用 mod 自己写的状态图替换 Spine 状态图；
- 给 **没有** Spine 骨骼的生物做动画（精灵表、帧序列、Godot `AnimationPlayer`）。

RitsuLib 为这三种需求暴露了三个**彼此正交**的工厂接口，以及一个针对非 Spine
场景的状态机抽象。四个接口都对生物类型无感（玩家角色与怪物通用），也不要求
继承任何模板。

| 接口 | 用途 | 对应原版入口 |
|---|---|---|
| `IModCreatureVisualsFactory` | 从代码构造 `NCreatureVisuals` | `CharacterModel.CreateVisuals`、`MonsterModel.CreateVisuals` |
| `IModCreatureAnimatorFactory` | 从代码构造 Spine `CreatureAnimator` | `CharacterModel.GenerateAnimator`、`MonsterModel.GenerateAnimator` |
| `IModNonSpineAnimationStateMachineFactory` | 为非 Spine 视觉构造 `ModAnimStateMachine` | `NCreature.SetAnimationTrigger`（路由补丁） |
| `IModCharacterMerchantAnimationStateMachineFactory` | 为商人 / 休息站中的角色视觉构造 `ModAnimStateMachine` | 商人场景初始化流程 |

商人工厂专属玩家角色，因为怪物从不会出现在商人 / 休息站场景中；其余三个接口对
任意 `MegaCrit.Sts2.Core.Models.AbstractModel` 都适用。

---

:::

## Creature Visuals Factory{lang="en"}

::: en

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

:::

## 生物视觉工厂{lang="zh-CN"}

::: zh-CN

`IModCreatureVisualsFactory` 在返回非 null 的 `NCreatureVisuals` 时，会替换
`(Character|Monster)Model.CreateVisuals` 的原有行为；返回 `null` 则退回到
`CustomVisualsPath` 等原版解析链路。

```csharp
public class MyCharacter : ModCharacterTemplate<...>
{
    // 模板已经实现了 IModCreatureVisualsFactory，并把调用转发到这个
    // protected virtual；重写它即可：
    protected override NCreatureVisuals? TryCreateCreatureVisuals()
    {
        var scene = GD.Load<PackedScene>(
            "res://MyMod/scenes/my_character/my_character_visuals.tscn");
        return scene.Instantiate<NCreatureVisuals>();
    }
}
```

如果不使用 `ModCharacterTemplate` / `ModMonsterTemplate`，直接在自己的
`CharacterModel` / `MonsterModel` 上实现接口即可：

```csharp
public class MyRawCharacter : CharacterModel, IModCreatureVisualsFactory
{
    public NCreatureVisuals? TryCreateCreatureVisuals() => ...;
}
```

路由补丁（`CharacterCreatureVisualsRuntimeFactoryPatch`、
`MonsterCreatureVisualsRuntimeFactoryPatch`）以 Harmony `Priority.First` 运行，
在原版基于路径的加载逻辑之前生效。

---

:::

## Creature Animator Factory (Spine){lang="en"}

::: en

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

:::

## Spine Animator 工厂{lang="zh-CN"}

::: zh-CN

`IModCreatureAnimatorFactory` 用来替换 `GenerateAnimator`，适用于 Spine 视觉。
推荐使用 `ModAnimStateMachines.Standard` 以复用原版状态图的形状：

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

`ModAnimStateMachines.Standard` 返回一个已经布好 `Idle` / `Dead` / `Hit` /
`Attack` / `Cast` / `Relaxed` any-state 触发器的 `CreatureAnimator`。终态
（`Dead`）不设置 `NextState`，所以播放完不会自动回到 idle。

路由补丁（`CharacterCreatureAnimatorRuntimeFactoryPatch`、
`MonsterCreatureAnimatorRuntimeFactoryPatch`）接受非 null 的工厂返回值；返回
`null` 则退回到原版 `GenerateAnimator`。

---

:::

## Non-Spine State Machine{lang="en"}

::: en

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

:::

## 非 Spine 状态机{lang="zh-CN"}

::: zh-CN

如果生物的战斗视觉**不是** Spine（没有 `MegaSprite` 控制器），实现
`IModNonSpineAnimationStateMachineFactory` 并返回绑定到视觉根节点的
`ModAnimStateMachine`。`ModCreatureNonSpineAnimationPlaybackPatch` 把
`NCreature.SetAnimationTrigger(trigger)` 路由到 `ModAnimStateMachine.SetTrigger`，
因此非 Spine 生物会接收到与 Spine 生物**完全相同**的触发流。

### 接入方式

```csharp
public class MyWolf : ModMonsterTemplate
{
    // 模板已经实现了 IModNonSpineAnimationStateMachineFactory，并把调用转发
    // 到这个 protected virtual；重写它即可：
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
            .AddState("die").Done()                     // 终态：不设置 NextState
            .AddAnyState("Idle",   "idle")
            .AddAnyState("Attack", "attack")
            .AddAnyState("Hit",    "hurt")
            .AddAnyState("Dead",   "die")
            .Build(backend);
    }
}
```

不使用模板时同理：

```csharp
public class MyRawMonster : MonsterModel, IModNonSpineAnimationStateMachineFactory
{
    public ModAnimStateMachine? TryCreateNonSpineAnimationStateMachine(Node visualsRoot)
        => /* 同上的 builder 代码 */;
}
```

### 路由行为

`ModCreatureNonSpineAnimationPlaybackPatch` 是 `NCreature.SetAnimationTrigger`
上的 Prefix 补丁，流程如下：

1. 如果生物已有 Spine animator，直接跳过（原版链路继续执行）。
2. 定位生物对应的模型（`Entity.Player?.Character` 或 `Entity.Monster`）。
3. 如果任一侧实现了 `IModNonSpineAnimationStateMachineFactory` 且返回非 null 的
   状态机，调用 `ModAnimStateMachine.SetTrigger` 派发触发器，然后返回。
4. 否则退回到单次 cue 播放
   （`ModCreatureVisualPlayback.TryPlayFromCreatureAnimatorTrigger`）。

状态机按视觉根节点缓存在 `ConditionalWeakTable<Node, StateMachineSlot>` 中，
因此工厂在一次战斗生命周期内最多执行一次，节点释放时会自动回收。

### 快捷方式：`ModAnimStateMachines.StandardCue`

如果视觉遵循 vanilla 的 idle / dead / hit / attack / cast / relaxed 结构，
直接使用 `ModAnimStateMachines.StandardCue` 即可由库内构建状态图。它会通过
`CompositeBackendFactory` 为每个状态挑选最合适的后端（优先 cue 帧序列，其次
Godot `AnimationPlayer` 或 `AnimatedSprite2D`），返回一个可直接使用的
`ModAnimStateMachine`。

---

:::

## Animation Backends{lang="en"}

::: en

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

:::

## 动画后端{lang="zh-CN"}

::: zh-CN

`IAnimationBackend` 是 `ModAnimStateMachine` 消费的统一驱动层。每个后端包装
Godot 的一个动画子系统，并在对应时机发出 `Started` / `Completed` /
`Interrupted` 事件。

| 后端 | 驱动对象 | 适用场景 |
|---|---|---|
| `AnimatedSprite2DBackend` | `AnimatedSprite2D` | 基于帧的 sprite 动画 |
| `GodotAnimationPlayerBackend` | `AnimationPlayer` | Godot `.tres` 动画库 |
| `CueAnimationBackend` | `VisualCueSet`（cue 帧序列 / cue 贴图） | 单帧贴图或帧序列播放 |
| `SpineAnimationBackend` | `MegaSprite` | Spine 骨骼动画 |
| `CompositeAnimationBackend` | 任意组合 | 多后端派发（同一状态机内部分状态走 sprite，另一部分走 animation player 等） |

### 事件契约

| 事件 | 触发时机 |
|---|---|
| `Started(id)` | `id` 对应的播放已开始 |
| `Completed(id)` | 单次播放结束，或一个循环周期结束 |
| `Interrupted(id)` | 播放被新动画抢占，尚未自然结束 |

`ModAnimState.NextState` 在 `Completed` 时推进，因此对非循环状态（`attack ->
idle` 等），后端**必须**准确发出 `Completed`。

### 队列语义

`Queue(id, loop)` 的语义是「当前动画播完后再播这一个」。各后端实现略有差异：

| 后端 | `Queue` 行为 |
|---|---|
| `SpineAnimationBackend` | Spine 原生队列（在 track 上 `AddAnimation`） |
| `AnimatedSprite2DBackend` | 记录待播 id，在下一次 `animation_finished` 信号时播放 |
| `GodotAnimationPlayerBackend` | 使用 `AnimationPlayer.Queue` |
| `CueAnimationBackend` | 记录待播 id，在当前序列结束时播放 |

任何后端上调用 `Play` 都会清空已排队的动画。

### `Stop()` 与跨后端切换

`IAnimationBackend.Stop()`（默认接口方法）会**静默**停止后端——既不发
`Completed` 也不发 `Interrupted`，并清掉排队动画。它的主要使用方是
`CompositeAnimationBackend`，在不同子后端之间切换时：

1. 新状态使用的后端与当前活动的后端不同。
2. 为即将离开的动画发出 `Interrupted`。
3. 调用离开后端的 `Stop()` 清理其内部状态。
4. 调用新进入后端的 `Play`。

如果不调用 `Stop()`，离开的后端可能继续发出已失效状态 id 的 `Completed` /
`Interrupted` 事件，干扰上层状态机。

---

:::

## Lifecycle Trigger Patches{lang="en"}

::: en

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

:::

## 生命周期触发补丁{lang="zh-CN"}

::: zh-CN

原版 `NCreature.StartDeathAnim` 和 `NCreature.StartReviveAnim` 只在
`_spineAnimator != null` 时派发 `Dead` / `Revive` 触发器。非 Spine 生物因此
收不到这两个触发器，自定义状态机在「弃置当前游戏」或玩家死亡时永远看不到
死亡动画。

RitsuLib 通过两个 Postfix 补丁修正这一缺陷：

- `NCreatureNonSpineDeathAnimationTriggerPatch` — 在 `StartDeathAnim` 之后派发
  `Dead`。
- `NCreatureNonSpineReviveAnimationTriggerPatch` — 在 `StartReviveAnim` 之后
  派发 `Revive`。

### 作用域收敛

这两个补丁是**opt-in**：只有当生物没有 Spine animator 且模型**显式**接入了
RitsuLib 视觉链路时才会触发。具体而言，`NonSpineAnimationTriggerScope.AppliesTo(NCreature)`
在以下任一条件成立时返回 `true`：

| 模型槽位 | 接口 | 备注 |
|---|---|---|
| `Entity.Player?.Character` | `IModNonSpineAnimationStateMachineFactory` | 状态机路径 |
| `Entity.Monster` | `IModNonSpineAnimationStateMachineFactory` | 状态机路径 |
| `Entity.Player?.Character` | `IModCharacterAssetOverrides` | cue 播放回退（仅玩家） |

原版生物、以及未接入 RitsuLib 视觉的其他 mod 都不会被影响。`Dead` 与 `Revive`
两个补丁使用完全相同的 gate。

---

:::

## Migration & Deprecation{lang="en"}

::: en

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

:::

## 迁移与废弃{lang="zh-CN"}

::: zh-CN

两个工厂接口最初按生物种类分别命名，现在已统一，旧名称被标记为 `[Obsolete]`：

| 新名称（推荐） | 已废弃的别名 |
|---|---|
| `IModCreatureVisualsFactory` | `IModMonsterCreatureVisualsFactory`、`IModCharacterCreatureVisualsFactory` |
| `IModCreatureAnimatorFactory` | `IModCharacterCreatureAnimatorFactory` |

### 兼容性保证

- 路由补丁在每次调用时**同时**检查新接口和废弃接口，所以只实现旧接口的 mod
  无需任何代码改动即可继续工作。
- `ModCharacterTemplate` / `ModMonsterTemplate` **同时**实现新接口和废弃别名，
  并把调用转发到同一批 protected virtual 钩子；外部对模板子类做
  `is IModCharacterCreatureVisualsFactory` 之类的类型检查仍然成立。
- 实现废弃接口会触发编译警告 **CS0618** 引导迁移。运行时行为不变、没有运行时
  警告。

### 迁移步骤

1. 在 `: Interfaces` 列表和显式接口实现中把旧名替换为新名：
   - `IModMonsterCreatureVisualsFactory` → `IModCreatureVisualsFactory`
   - `IModCharacterCreatureVisualsFactory` → `IModCreatureVisualsFactory`
   - `IModCharacterCreatureAnimatorFactory` → `IModCreatureAnimatorFactory`
2. 方法签名（`TryCreateCreatureVisuals()`、
   `TryCreateCreatureAnimator(MegaSprite)`）保持不变，变化只在声明接口的名字上。
3. 重新编译，CS0618 警告消失。

如果只是继承模板并重写 protected virtual 钩子
（`TryCreateCreatureVisuals`、`SetupCustomCreatureAnimator`），无需任何迁移；
这些钩子未变。

---

:::

## Summary Cheat-sheet{lang="en"}

::: en

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

:::

## 速查表{lang="zh-CN"}

::: zh-CN

```text
目标                                        实现的接口
---------------------------------------------------------------------------
替换 CreateVisuals（玩家或怪物）             IModCreatureVisualsFactory
替换 Spine GenerateAnimator                  IModCreatureAnimatorFactory
驱动非 Spine 状态机                          IModNonSpineAnimationStateMachineFactory
驱动商人 / 休息站状态机                       IModCharacterMerchantAnimationStateMachineFactory
```

无论你是继承 `ModCharacterTemplate` / `ModMonsterTemplate`，还是直接在
`CharacterModel` / `MonsterModel` 上实现这些接口，路由补丁都会生效：它们以
Harmony `Priority.First` 运行，且在工厂返回 `null` 时退回到原版行为。

:::
