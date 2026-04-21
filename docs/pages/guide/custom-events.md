---
title:
  en: Custom Events
  zh-CN: 自定义事件
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

This document explains how to plug custom events into the game's event pipeline using RitsuLib.

It covers three registration shapes:

- shared events: `SharedEvent<TEvent>()`
- act-specific events: `ActEvent<TAct, TEvent>()`
- ancients: `SharedAncient<TAncient>()` / `ActAncient<TAct, TAncient>()`

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

本文说明如何通过 RitsuLib 将自定义事件接入游戏的事件管线。

它覆盖三类注册：

- 共享事件：`SharedEvent<TEvent>()`
- Act 专属事件：`ActEvent<TAct, TEvent>()`
- Ancient：`SharedAncient<TAncient>()` / `ActAncient<TAct, TAncient>()`

---

:::

## Base-game event pipeline{lang="en"}

::: en

> The following is the game's own event runtime flow, to help you see where RitsuLib registration ultimately takes effect.

Event generation and execution in the game involve these stages:

| Stage | Game type | Role |
|---|---|---|
| Candidate generation | `ActModel.GenerateRooms(...)` | Builds the candidate list from the act-local event pool and the `ModelDb.AllSharedEvents` shared pool |
| Filtering | `RoomSet.EnsureNextEventIsValid(...)` | Filters using `IsAllowed(runState)` and visited-state records |
| Entry | `EventRoom.Enter(...)` | Preloads assets, creates the mutable instance, and builds the event UI |
| Assets | `EventModel.GetAssetPaths(...)` | Supplies asset paths that must be ready before the event opens |

---

:::

## 游戏原版事件管线{lang="zh-CN"}

::: zh-CN

> 以下是游戏引擎自身的事件运行时流程，帮助理解 RitsuLib 的注册内容最终在哪里生效。

游戏中事件的生成与执行涉及以下环节：

| 阶段 | 游戏类型 | 职责 |
|---|---|---|
| 候选生成 | `ActModel.GenerateRooms(...)` | 从 Act 本地事件池和 `ModelDb.AllSharedEvents` 共享池构建候选列表 |
| 过滤 | `RoomSet.EnsureNextEventIsValid(...)` | 按 `IsAllowed(runState)` 与已访问记录过滤 |
| 进入 | `EventRoom.Enter(...)` | 预加载资源、创建可变实例、搭建事件界面 |
| 资源 | `EventModel.GetAssetPaths(...)` | 提供进入事件前需要准备的资源路径 |

---

:::

## RitsuLib registration{lang="en"}

::: en

RitsuLib does not replace the flow above. At registration time it adds mod events into the same entry points the base game already uses:

- shared events are appended to `ModelDb.AllSharedEvents`
- act events are appended to the selected act's event list
- ancients are appended to the corresponding shared or act-local ancient lists

For authors, the work boils down to two steps:

1. define a valid `EventModel` or `AncientEventModel` subtype
2. register it before content registration freezes

---

:::

## RitsuLib 的注册机制{lang="zh-CN"}

::: zh-CN

RitsuLib 不替换上述流程，而是在注册阶段把 Mod 事件补充进原版已有的事件入口：

- 共享事件追加到 `ModelDb.AllSharedEvents`
- Act 事件追加到对应 Act 的事件列表
- Ancient 追加到对应的共享或 Act 本地 Ancient 列表

对 Mod 作者来说，实际工作可以概括为两步：

1. 定义一个合法的 `EventModel` 或 `AncientEventModel` 子类
2. 在内容注册冻结之前将其注册

---

:::

## Minimal normal event{lang="en"}

::: en

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

:::

## 最小普通事件{lang="zh-CN"}

::: zh-CN

推荐继承 `ModEventTemplate`，而不是直接继承原版 `EventModel`（原因见下文）。

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

最小可用事件至少应满足：

- 实现 `GenerateInitialOptions()`
- 在选项回调里推进或结束事件
- 本地化键与最终 `ModelId.Entry` 保持一致

---

:::

## Registration{lang="en"}

::: en

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

:::

## 注册方式{lang="zh-CN"}

::: zh-CN

### 共享事件

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SharedEvent<MyFirstEvent>()
    .Apply();
```

### Act 专属事件

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

:::

## Localization keys{lang="en"}

::: en

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

:::

## 本地化键{lang="zh-CN"}

::: zh-CN

通过 RitsuLib 注册后，事件的 `ModelId.Entry` 采用固定格式：

```text
<MODID>_EVENT_<TYPENAME>
```

例如 `MyMod` 与 `MyFirstEvent`：

```text
MY_MOD_EVENT_MY_FIRST_EVENT
```

最小普通事件的本地化块示例：

```json
{
  "MY_MOD_EVENT_MY_FIRST_EVENT.title": "陌生的泉眼",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.INITIAL.description": "你在路边发现了一口发光的泉眼。",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.INITIAL.options.ACCEPT.title": "饮下泉水",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.INITIAL.options.ACCEPT.description": "也许会有好事发生。",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.INITIAL.options.LEAVE.title": "离开",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.INITIAL.options.LEAVE.description": "你决定不冒险。",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.ACCEPT.description": "你感觉精神好了很多。",
  "MY_MOD_EVENT_MY_FIRST_EVENT.pages.LEAVE.description": "你转身离开。"
}
```

关键要求是一致性：事件标题、页面文本和选项键都应基于同一个最终的 `Id.Entry` 生成。

---

:::

## Why use `ModEventTemplate`{lang="en"}

::: en

> The following explains a behavioral detail of the base game's `EventModel`.

In the base game, `EventModel.InitialOptionKey(...)` and internal option-key helpers build key prefixes from `GetType().Name` (via `Slugify`), while titles, page text, and related lookups use `Id.Entry`.

For vanilla events those two usually match. For events registered through RitsuLib, `GetType().Name` and `Id.Entry` differ, so some text lookups use a different key prefix than the rest.

`ModEventTemplate` and `ModAncientEventTemplate` use `protected new` to hide the base `InitialOptionKey` helpers and generate option keys from the final registered `Id.Entry`, removing that mismatch.

---

:::

## 为什么要用 `ModEventTemplate`{lang="zh-CN"}

::: zh-CN

> 以下解释涉及游戏原版 `EventModel` 的一个行为特征。

原版 `EventModel.InitialOptionKey(...)` 及内部 option-key 辅助方法使用 `GetType().Name`（经 `Slugify` 处理）拼接键前缀，而事件标题、页面描述等使用 `Id.Entry`。

对原版事件，这两者通常一致。但对通过 RitsuLib 注册的事件，`GetType().Name` 和 `Id.Entry` 不同，会导致部分文本查找落在不同的键前缀上。

`ModEventTemplate` 和 `ModAncientEventTemplate` 通过 `protected new` 隐藏了基类的 `InitialOptionKey`，统一基于最终注册后的 `Id.Entry` 生成选项键，从而消除这种不一致。

---

:::

## `IsAllowed`{lang="en"}

::: en

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

:::

## `IsAllowed`{lang="zh-CN"}

::: zh-CN

> 以下描述游戏原版的事件过滤机制。

如果事件只应在部分跑局中出现，可以覆写 `IsAllowed(RunState runState)`：

```csharp
public override bool IsAllowed(RunState runState)
{
    return !runState.VisitedEventIds.Contains(Id);
}
```

运行时，游戏会在候选事件池中轮询，直到找到同时满足以下条件的事件：

- `IsAllowed(...)` 返回 `true`
- 当前跑局尚未访问过该事件

`IsAllowed` 表达的是"当前跑局是否允许出现"，不是注册阶段的准备逻辑。

---

:::

## Custom event scene{lang="en"}

::: en

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

:::

## 自定义事件场景{lang="zh-CN"}

::: zh-CN

> 以下描述游戏原版的自定义事件布局机制。

返回自定义布局类型：

```csharp
public override EventLayoutType LayoutType => EventLayoutType.Custom;
```

此时游戏会加载：

```text
res://scenes/events/custom/<event-id-lower>.tscn
```

该场景根节点必须实现 `ICustomEventNode`，至少提供 `Initialize(EventModel)` 和 `CurrentScreenContext`。

---

:::

## Asset preloading{lang="en"}

::: en

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

:::

## 资源预加载{lang="zh-CN"}

::: zh-CN

> 以下描述游戏原版的事件资源预加载规则。

普通事件默认预加载：

- 布局场景
- `res://images/events/<event-id-lower>.png`
- 可选的 `res://scenes/vfx/events/<event-id-lower>_vfx.tscn`

Ancient 默认预加载：

- 布局场景
- `res://scenes/events/background_scenes/<event-id-lower>.tscn`

如需额外资源，可覆写 `GetAssetPaths(IRunState runState)` 追加路径。

---

:::

## Minimal ancient example{lang="en"}

::: en

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

:::

## Ancient 最小示例{lang="zh-CN"}

::: zh-CN

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

选项键、页面键与最终注册后的 `Id.Entry` 保持一致的原则同样适用。

---

:::

## Related docs{lang="en"}

::: en

- [Content Authoring Toolkit](/guide/content-authoring-toolkit)
- [Content Packs & Registries](/guide/content-packs-and-registries)
- [Localization & Keywords](/guide/localization-and-keywords)

:::

## 相关文档{lang="zh-CN"}

::: zh-CN

- [内容注册规则](/guide/content-authoring-toolkit)
- [内容包与注册器](/guide/content-packs-and-registries)
- [本地化与关键词](/guide/localization-and-keywords)

:::
