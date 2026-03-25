# 自定义事件

本文说明自定义事件如何接入游戏原本的事件管线，以及 RitsuLib 如何把这条管线整理成可注册、可预测的作者接口。

它覆盖三类内容：

- 共享事件：`SharedEvent<TEvent>()`
- Act 专属事件：`ActEvent<TAct, TEvent>()`
- Ancient：`SharedAncient<TAncient>()` / `ActAncient<TAct, TAncient>()`

---

## 运行时模型

RitsuLib 不会替换游戏原本的事件流程。
它做的是把已注册的事件和 Ancient 模型补充进原版已经在使用的事件入口。

在 `sts-2-source` 中，相关运行时职责主要包括：

- `ActModel.GenerateRooms(...)`：从共享池和 Act 本地池构建事件候选
- `RoomSet.EnsureNextEventIsValid(...)`：按 `IsAllowed(runState)` 与访问记录过滤事件
- `EventRoom.Enter(...)`：预加载资源、创建 mutable 实例、搭建事件界面
- `EventModel.GetAssetPaths(...)`：给出进入事件前需要准备的资源路径

在这套模型上，RitsuLib 追加注册内容：

- 共享事件追加到共享事件集合
- Act 事件追加到对应 Act 的事件列表
- Ancient 追加到对应的共享或 Act 本地 Ancient 列表

对作者来说，实际工作可以概括为两步：

1. 定义一个合法的 `EventModel` 或 `AncientEventModel` 子类
2. 在内容注册冻结之前把它注册进去

---

## 最小普通事件

对大多数 Mod 事件，推荐继承 `ModEventTemplate`，而不是直接继承原版 `EventModel`。

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

一个最小可用的事件模型至少应满足：

- 实现 `GenerateInitialOptions()`
- 在选项回调里推进事件状态，或结束事件
- 让本地化 key 与最终 `ModelId.Entry` 保持一致

---

## 注册方式

### 共享事件

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SharedEvent<MyFirstEvent>()
    .Apply();
```

这样事件会进入共享事件池。

### Act 专属事件

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .ActEvent<MyAct, MyFirstEvent>()
    .Apply();
```

这样事件只会进入指定 Act 的事件列表。

### Ancient

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .SharedAncient<MyAncient>()
    .Apply();
```

或者：

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .ActAncient<MyAct, MyAncient>()
    .Apply();
```

---

## 本地化键

通过 RitsuLib 注册后，事件的公开 `ModelId.Entry` 采用固定格式：

```text
<MODID>_EVENT_<TYPENAME>
```

例如 `MyMod` 与 `MyFirstEvent`：

```text
MY_MOD_EVENT_MY_FIRST_EVENT
```

一个最小普通事件的本地化块通常可以写成：

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

这里最重要的是一致性：

- 事件标题和页面文本通过 `Id.Entry` 查找
- 选项 key 也应该基于同一个最终标识生成

---

## 为什么有 `ModEventTemplate` 与 `ModAncientEventTemplate`

原版事件实现里有一个对 vanilla 内容通常无害、但对注册式 Mod 事件很重要的差异。

这个差异是：

- 原版 `EventModel.InitialOptionKey(...)` 以及内部 option-key helper 使用 `GetType().Name`
- 但事件标题、页面描述、`GameInfoOptions` 使用的是 `Id.Entry`

对原版事件，这两者经常正好相同。
但对通过 RitsuLib 注册的事件，它们通常不同。

结果就是，事件有可能一部分文本落在：

```text
MY_FIRST_EVENT...
```

另一部分文本却落在：

```text
MY_MOD_EVENT_MY_FIRST_EVENT...
```

为了让这些查找统一起来，RitsuLib 提供了：

- `ModEventTemplate`
- `ModAncientEventTemplate`

它们的 helper 会统一基于最终注册后的 `Id.Entry` 来生成选项 key，而不是直接使用 CLR 类型名。

---

## `IsAllowed`

如果事件只应在部分跑局中出现，可以覆写 `IsAllowed(RunState runState)`：

```csharp
public override bool IsAllowed(RunState runState)
{
    return !runState.VisitedEventIds.Contains(Id);
}
```

运行时，游戏会在可用事件池中轮换，直到找到同时满足以下条件的事件：

- `IsAllowed(...)` 返回 `true`
- 当前跑局尚未访问过该事件

因此，`IsAllowed` 应表达的是“当前跑局是否允许出现”，而不是注册阶段的准备逻辑。

---

## 自定义事件场景

如果默认事件布局不适合，可以返回自定义布局类型：

```csharp
public override EventLayoutType LayoutType => EventLayoutType.Custom;
```

此时游戏会加载：

```text
res://scenes/events/custom/<event-id-lower>.tscn
```

例如：

```text
res://scenes/events/custom/my_mod_event_my_first_event.tscn
```

该场景根节点必须实现 `ICustomEventNode`，并至少提供：

- `Initialize(EventModel eventModel)`
- `CurrentScreenContext`

这是因为 `EventModel.SetNode(...)` 在处理自定义布局时会将节点强制转换为 `ICustomEventNode`。

---

## 资源预加载

普通事件默认会预加载：

- 布局场景
- `res://images/events/<event-id-lower>.png`
- 可选的 `res://scenes/vfx/events/<event-id-lower>_vfx.tscn`

Ancient 默认会预加载：

- 布局场景
- `res://scenes/events/background_scenes/<event-id-lower>.tscn`

如果事件还需要额外资源，可以覆写 `GetAssetPaths(IRunState runState)` 并追加路径。

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
        SetEventFinished(L10NLookup($"{Id.Entry}.pages.ACCEPT.description"));
        return Task.CompletedTask;
    }
}
```

这里同样遵循相同原则：选项 key、页面 key 与最终注册后的 `Id.Entry` 应当保持一致。

---

## 相关文档

- [内容注册规则](ContentAuthoringToolkit.md)
- [内容包与注册器](ContentPacksAndRegistries.md)
- [本地化与关键词](LocalizationAndKeywords.md)
