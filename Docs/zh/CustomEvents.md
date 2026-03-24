# 自定义事件

本文结合 `sts-2-source` 中的事件运行机制与 RitsuLib 提供的注册接口，说明自定义事件的实现方式与接入流程。

它覆盖三类内容：

- 普通共享事件（`SharedEvent<TEvent>()`）
- Act 专属事件（`ActEvent<TAct, TEvent>()`）
- Ancient（`SharedAncient<TAncient>()` / `ActAncient<TAct, TAncient>()`）

---

## 先理解游戏源码里的事件入口

从 `sts-2-source` 的实现看，事件进入游戏主要经过以下几个入口：

- `ActModel.GenerateRooms(...)`：将 `AllEvents` 与 `ModelDb.AllSharedEvents` 合并到当前章节的事件池
- `RoomSet.EnsureNextEventIsValid(...)`：在进入未知房时跳过 `IsAllowed(runState) == false` 或已经访问过的事件
- `EventRoom.Enter(...)`：预加载事件资源，创建 mutable 实例，再构建事件 UI
- `EventModel.GetAssetPaths(...)`：决定事件房间需要预加载哪些资源

RitsuLib 并不重写这套流程，而是将已注册的类型补充到这些访问点中：

- 共享事件追加到 `ModelDb.AllSharedEvents` / `ModelDb.AllEvents`
- Act 事件通过动态补丁追加到每个 `ActModel.AllEvents`
- Ancient 同理追加到 `AllSharedAncients`、`AllAncients` 与各 Act 的 `AllAncients`

因此，对 Mod 作者而言，关键步骤主要有两项：

1. 写一个符合原版 `EventModel` / `AncientEventModel` 约定的模型
2. 在框架冻结前把它注册进 RitsuLib

---

## 最小普通事件

推荐继承 `ModEventTemplate`，而不是直接继承原版 `EventModel`。

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

最小事件模型通常应满足以下条件：

- 实现 `GenerateInitialOptions()`
- 选项回调最后把事件推进到新状态，或直接 `SetEventFinished(...)`
- 本地化键与最终 `ModelId.Entry` 对齐

---

## 注册方式

### 共享事件

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SharedEvent<MyFirstEvent>()
    .Apply();
```

共享事件会进入所有章节的共享事件池。

### Act 专属事件

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .ActEvent<MyAct, MyFirstEvent>()
    .Apply();
```

它只会追加到指定 `ActModel.AllEvents` 中。

### Ancient

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SharedAncient<MyAncient>()
    .Apply();
```

或：

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .ActAncient<MyAct, MyAncient>()
    .Apply();
```

---

## 本地化键怎么写

通过 RitsuLib 注册后，事件的 `ModelId.Entry` 会固定成：

```text
<MODID>_EVENT_<TYPENAME>
```

例如 `MyMod` + `MyFirstEvent`：

```text
MY_MOD_EVENT_MY_FIRST_EVENT
```

一个最小可用的普通事件本地化通常至少包含：

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

这里需要特别注意两点：

- 事件标题与正文本来就按 `Id.Entry` 查找
- RitsuLib 提供的 `ModEventTemplate.InitialOptionKey(...)` 也会按 `Id.Entry` 生成选项 key

---

## 为什么推荐 `ModEventTemplate`

这里存在一个需要明确说明的实现差异：

- 原版 `EventModel.InitialOptionKey(...)` / 内部 `OptionKey(...)` 用的是 `GetType().Name`
- 但事件标题、页面描述、`GameInfoOptions` 等逻辑用的是 `Id.Entry`
- 对原版事件，这两者通常恰好相同
- 对通过 RitsuLib 注册的 Mod 事件，这两者通常不同

因此，如果直接继承原版 `EventModel` 并继续使用其 `InitialOptionKey(...)`，生成出的选项 key 可能落在 `MY_FIRST_EVENT...`，而正文与标题却位于 `MY_MOD_EVENT_MY_FIRST_EVENT...` 下，最终会导致本地化与游戏信息页引用不一致。

为消除这一差异，RitsuLib 提供了两个模板：

- `ModEventTemplate`
- `ModAncientEventTemplate`

它们提供的 `InitialOptionKey(...)` / `ModOptionKey(...)` 会统一基于最终 `Id.Entry`。

---

## `IsAllowed` 的作用

如果事件不是每局都可出现，覆写 `IsAllowed(RunState runState)`：

```csharp
public override bool IsAllowed(RunState runState)
{
    return !runState.VisitedEventIds.Contains(Id);
}
```

游戏会在 `RoomSet.EnsureNextEventIsValid(...)` 中轮换事件池，直到找到：

- `IsAllowed(...) == true`
- 当前跑局还没访问过该 `Id`

因此，`IsAllowed` 应只表达当前跑局中的可出现条件，而不应承担注册期逻辑。

---

## 自定义事件场景

如果不使用默认事件布局，可以返回自定义布局：

```csharp
public override EventLayoutType LayoutType => EventLayoutType.Custom;
```

这时游戏会去加载：

```text
res://scenes/events/custom/<event-id-lower>.tscn
```

例如：

```text
res://scenes/events/custom/my_mod_event_my_first_event.tscn
```

并且该场景根节点需要实现 `ICustomEventNode`，至少提供：

- `Initialize(EventModel eventModel)`
- `CurrentScreenContext`

源码中的 `EventModel.SetNode(...)` 在 `LayoutType == Custom` 时会将节点强制转换为 `ICustomEventNode`，因此这里不仅需要匹配场景结构，也必须实现对应接口。

---

## 资源预加载规则

普通事件默认预加载：

- 布局场景
- `res://images/events/<event-id-lower>.png`
- 可选的 `res://scenes/vfx/events/<event-id-lower>_vfx.tscn`

Ancient 默认预加载：

- 布局场景
- `res://scenes/events/background_scenes/<event-id-lower>.tscn`

如果事件需要额外资源，可覆写 `GetAssetPaths(IRunState runState)` 并追加相应路径。

---

## Ancient 最小示例

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

与普通事件相比，Ancient 还需要额外注意以下几点：

- `LocTable` 是 `ancients`
- 必须实现 `DefineDialogues()`
- 完成时通常调用 `Done()`，这样 Ancient 历史记录也会一起写入

如果目标只是为自定义角色补充原版 Ancient 对话，而不是新增 Ancient 模型，可直接使用现有的 `AncientDialogueLocalization` 支持，详见 [本地化与关键词](LocalizationAndKeywords.md)。

---

## 和解锁系统一起用

事件同样可以附加 Epoch 门槛：

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SharedEvent<MyFirstEvent>()
    .RequireEpoch<MyFirstEvent, MyEpoch>()
    .Apply();
```

RitsuLib 会在章节生成完成后的事件池上执行过滤。

这一部分同时补充了一个框架层面的稳定性修正：

- 先前如果解锁过滤将某个章节的事件池清空，后续选择事件时可能触发运行时错误
- 现在 RitsuLib 会保留过滤前的原始事件池并输出警告，避免将章节置于无可用事件的状态

不过从内容设计角度，仍建议：

- 不要把某个 Act 里“所有可能出现的事件”都锁掉
- 至少保留一个始终可出现的事件

---

## 推荐实践

- 普通事件继承 `ModEventTemplate`
- Ancient 继承 `ModAncientEventTemplate`
- 选项 key 统一走 `InitialOptionKey(...)` / `ModOptionKey(...)`
- 自定义布局时确保场景根节点实现 `ICustomEventNode`
- 有 Epoch 门槛时，至少保证每个事件池还能留下一个可用事件

---

## 相关文档

- [内容包与注册器](ContentPacksAndRegistries.md)
- [时间线与解锁](TimelineAndUnlocks.md)
- [本地化与关键词](LocalizationAndKeywords.md)
- [Godot 场景编写说明](GodotSceneAuthoring.md)
