---
title:
  en: Timeline & Unlocks
  zh-CN: 时间线与解锁
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Introduction{lang="en"}

::: en

This is the reference for timeline registration and unlock semantics.

RitsuLib splits timeline registration and unlock rules into two systems that are meant to work together. This document covers:

- How `Story` and `Epoch` are registered
- What the template types are responsible for
- How unlock rules are evaluated
- Limitations of vanilla progression for mod characters and RitsuLib’s compatibility bridges

---

:::

## 简介{lang="zh-CN"}

::: zh-CN

本文是时间线注册与解锁语义的参考文档。

RitsuLib 将时间线注册和解锁规则拆成两个系统，配合使用。本文说明：

- `Story` / `Epoch` 的注册方式
- 模板类型的职责
- 解锁规则的判定机制
- 原版进度逻辑对 Mod 角色的局限性与 RitsuLib 的兼容桥接

---

:::

## The Two Registries{lang="en"}

::: en

| Registry | Role |
|---|---|
| `ModTimelineRegistry` | Registers `StoryModel` and `EpochModel` |
| `ModUnlockRegistry` | Defines unlock conditions for content or epochs |

In the fluent builder, these correspond to:

- `.Story<TStory>()`, `.Epoch<TEpoch>()`
- `.RequireEpoch<TModel, TEpoch>()`, `.UnlockEpochAfter...()`

Core distinction:

- **Timeline registration** answers “does this thing exist?”
- **Unlock registration** answers “when does it become available?”

---

:::

## 两个注册器{lang="zh-CN"}

::: zh-CN

| 注册器 | 职责 |
|---|---|
| `ModTimelineRegistry` | 注册 `StoryModel` 和 `EpochModel` |
| `ModUnlockRegistry` | 定义内容或纪元的解锁条件 |

在链式构建器里，对应：

- `.Story<TStory>()`、`.Epoch<TEpoch>()`
- `.RequireEpoch<TModel, TEpoch>()`、`.UnlockEpochAfter...()`

核心区别：

- **时间线注册**回答"这个东西是否存在"
- **解锁注册**回答"它什么时候可用"

---

:::

## Story Registration{lang="en"}

::: en

Use `ModStoryTemplate` for the story **type** (slug id from `StoryKey` only). Epoch **order** is not a hard-coded list on the story class; register each epoch against the story in manifest order:

```csharp
public class MyStory : ModStoryTemplate
{
    protected override string StoryKey => "my-story";
}

// Fluent (order = column order):
// .StoryEpoch<MyStory, MyCharacterEpoch>()
// .StoryEpoch<MyStory, MyCardEpoch>()
// .Story<MyStory>()

// Or IModContentPackEntry list (same idea as card manifest entries):
// new TimelineColumnPackEntry<MyStory>(c => c.Epoch<MyCharacterEpoch>()...),
// new StoryPackEntry<MyStory>(),
```

`ModStoryTemplate` is responsible for:

- Deriving a normalized story identity from `StoryKey`
- Building `Epochs` from `ModStoryEpochBindings` (filled by `ModTimelineRegistry.RegisterStoryEpoch<TStory, TEpoch>()`)

`RegisterStoryEpoch` registers the epoch with vanilla discovery **and** appends it to that story’s column. Use `.Epoch<TEpoch>()` only for epochs that are **not** part of a mod story column.

---

:::

## `Story` 注册{lang="zh-CN"}

::: zh-CN

故事类型仍用 `ModStoryTemplate`，只实现 `StoryKey`。栏内 **Epoch 顺序**不要在故事类里写死；按注册顺序把每个 Epoch 绑到该故事：

```csharp
public class MyStory : ModStoryTemplate
{
    protected override string StoryKey => "my-story";
}

// 流式: .StoryEpoch<MyStory, MyCharacterEpoch>() … .Story<MyStory>()
// 或 IModContentPackEntry: TimelineColumnPackEntry / StoryPackEntry
```

`ModStoryTemplate` 的职责：

- 通过 `StoryKey` 自动生成规范化的故事标识
- 通过 `ModStoryEpochBindings`（`RegisterStoryEpoch` 写入）组装 `Epochs`

`RegisterStoryEpoch` 会注册 Epoch 并追加到该故事栏。不属于 mod 故事栏的 Epoch 可继续只用 `.Epoch<TEpoch>()`。

---

:::

## Epoch Registration{lang="en"}

::: en

You can write plain `EpochModel` subclasses, or use RitsuLib template types:

| Template | Description |
|---|---|
| `CharacterUnlockEpochTemplate<TCharacter>` | Epoch that unlocks the character |
| `CardUnlockEpochTemplate` | Epoch that unlocks extra cards |
| `RelicUnlockEpochTemplate` | Epoch that unlocks extra relics |
| `PotionUnlockEpochTemplate` | Epoch that unlocks extra potions |

These templates mainly handle:

- Enqueue logic for the timeline unlock UI
- Follow-up epochs via `ExpansionEpochTypes`

### Character unlock epoch template

Built-in behavior of `CharacterUnlockEpochTemplate<TCharacter>`:

- Queues a character unlock in `NTimelineScreen`
- Writes the pending character unlock to save progress
- If `ExpansionEpochTypes` is set, queues further epochs into the timeline expansion

### Card / relic / potion epoch templates

`CardUnlockEpochTemplate`, `RelicUnlockEpochTemplate`, and `PotionUnlockEpochTemplate` work similarly:

- You declare the model types to unlock
- The template resolves types through `ModelDb`
- `UnlockText` is generated automatically
- `QueueUnlocks()` pushes into the timeline UI

---

:::

## `Epoch` 注册{lang="zh-CN"}

::: zh-CN

可以直接写原生 `EpochModel` 子类，也可以使用 RitsuLib 提供的模板类型：

| 模板 | 说明 |
|---|---|
| `CharacterUnlockEpochTemplate<TCharacter>` | 解锁角色本身的纪元 |
| `CardUnlockEpochTemplate` | 解锁额外卡牌的纪元 |
| `RelicUnlockEpochTemplate` | 解锁额外遗物的纪元 |
| `PotionUnlockEpochTemplate` | 解锁额外药水的纪元 |

这些模板主要负责：

- 生成时间线界面的解锁入队逻辑
- 通过 `ExpansionEpochTypes` 支持后续纪元展开

### 角色解锁纪元模板

`CharacterUnlockEpochTemplate<TCharacter>` 的内置行为：

- 向 `NTimelineScreen` 队列一个角色解锁
- 把待解锁角色写入进度存档
- 若配置了 `ExpansionEpochTypes`，继续把后续纪元加入时间线展开

### 卡牌/遗物/药水纪元模板

`CardUnlockEpochTemplate`、`RelicUnlockEpochTemplate`、`PotionUnlockEpochTemplate` 的工作方式相似：

- 声明要解锁的模型类型
- 模板通过 `ModelDb` 解析类型
- `UnlockText` 自动生成
- `QueueUnlocks()` 自动推入时间线界面

---

:::

## Expansion Epochs{lang="en"}

::: en

All unlock epoch templates support:

```csharp
protected virtual IEnumerable<Type> ExpansionEpochTypes => [];
```

When the current epoch completes, these epochs are added automatically as timeline expansions, which helps chain unlocks:

1. Unlock the character first
2. Then reveal card unlocks
3. Then reveal relic unlocks

---

:::

## Expansion Epochs{lang="zh-CN"}

::: zh-CN

所有解锁纪元模板都支持：

```csharp
protected virtual IEnumerable<Type> ExpansionEpochTypes => [];
```

当前纪元完成时会自动把这些纪元作为时间线扩展加入，用于组织解锁链：

1. 先解锁角色
2. 再展开卡牌解锁
3. 再展开遗物解锁

---

:::

## Registration Timing and Freeze{lang="en"}

::: en

Both the timeline and unlock registries freeze after early initialization because:

- Story and epoch identities must stay stable
- Unlock filtering and compatibility patches need a finalized rule set

Register `Story`, `Epoch`, and unlock rules from your initializer — not later at runtime.

---

:::

## 注册时机与冻结{lang="zh-CN"}

::: zh-CN

时间线和解锁两个注册器都会在早期初始化后冻结。原因是：

- 故事/纪元标识必须稳定
- 解锁过滤与兼容补丁需要面对最终确定的规则表

`Story`、`Epoch` 和解锁规则都应在初始化入口中注册，不要拖到运行期。

---

:::

## Requiring an Epoch for Content{lang="en"}

::: en

When a model is registered but should only appear after an epoch is obtained, use `RequireEpoch<TModel, TEpoch>()`.

Typical uses:

- Late-game cards stay out of the pool until progress is met
- Relics open only after a specific story branch
- Shared ancients / events need a timeline milestone

RitsuLib applies the gate across multiple entry points:

- `UnlockState.Characters`
- Unlocked card / relic / potion pool queries
- Shared ancient lists
- Events generated for acts

This is not UI-only filtering; it changes what the game can actually offer.

### Epoch progress vs. timeline reveal

Vanilla `UnlockState` built from save progress mainly reflects epochs that have reached **`EpochState.Revealed`** (visible on the timeline) in **`UnlockedEpochs`**. **`SaveManager.ObtainEpoch`** can set **`Obtained`** / **`ObtainedNoSlot`** *before* the timeline slot is revealed.

**`ModUnlockRegistry.IsUnlocked`** (used when applying **`RequireEpoch`** gating) treats the requirement as satisfied if **either**:

- the epoch id is in **`unlockState.UnlockedEpochs`**, or  
- **`SaveManager.Instance.Progress.IsEpochObtained(epochId)`** is true.

So pool / character / event gating lines up with mod rules that call **`ObtainEpoch`**, not only with vanilla timeline reveal timing.

---

:::

## 为内容设置 Epoch 门槛{lang="zh-CN"}

::: zh-CN

当模型已注册，但应在某个纪元解锁后才出现时，使用 `RequireEpoch<TModel, TEpoch>()`。

常见用途：

- 后期卡牌在进度达成前不进入牌池
- 遗物只在特定故事分支后开放
- 共享 Ancient / 事件需要时间线进度门槛

RitsuLib 将门槛应用到多个访问入口：

- `UnlockState.Characters`
- 卡牌/遗物/药水的已解锁池查询
- 共享 Ancient 列表
- Act 生成出来的事件列表

这不是单纯 UI 过滤，而是真正影响游戏可提供内容的规则。

### 纪元进度与时间线「揭示」

从存档生成的原版 **`UnlockState`** 里，**`UnlockedEpochs`** 主要反映已进入 **`EpochState.Revealed`**（时间线栏位已显示）的纪元。而 **`SaveManager.ObtainEpoch`** 可能先把纪元标成 **`Obtained`** / **`ObtainedNoSlot`**，时间线槽位尚未揭示。

应用 **`RequireEpoch`** 门槛时，**`ModUnlockRegistry.IsUnlocked`** 在以下**任一**成立时即视为已满足：

- 该纪元 id 出现在 **`unlockState.UnlockedEpochs`** 中，或  
- **`SaveManager.Instance.Progress.IsEpochObtained(epochId)`** 为真。

这样，牌池 / 角色 / 事件等门槛会与通过 Mod 规则调用 **`ObtainEpoch`** 的进度一致，而不必等到原版时间线 UI 完全跟上。

---

:::

## Post-Run Epoch Rules{lang="en"}

::: en

Common convenience APIs on `ModUnlockRegistry`:

| Method | Description |
|---|---|
| `UnlockEpochAfterRunAs<TCharacter, TEpoch>()` | Unlock after completing a run with the given character |
| `UnlockEpochAfterWinAs<TCharacter, TEpoch>()` | Unlock after a win with that character |
| `UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(level)` | Unlock after a win at the given ascension |
| `UnlockEpochAfterRunCount<TEpoch>(requiredRuns, requireVictory)` | Unlock after enough runs |

These all compile to `PostRunEpochUnlockRule`.

You can also register a custom rule:

```csharp
unlocks.RegisterPostRunRule(
    PostRunEpochUnlockRule.Create(
        epochId: new MyEpoch().Id,
        description: "Unlock after any abandoned ascension-5 run",
        shouldUnlock: ctx => ctx.IsAbandoned && ctx.AscensionLevel >= 5));
```

---

:::

## 局后 Epoch 规则{lang="zh-CN"}

::: zh-CN

`ModUnlockRegistry` 提供的常用便捷 API：

| 方法 | 说明 |
|---|---|
| `UnlockEpochAfterRunAs<TCharacter, TEpoch>()` | 使用指定角色完成一局后解锁 |
| `UnlockEpochAfterWinAs<TCharacter, TEpoch>()` | 使用指定角色胜利后解锁 |
| `UnlockEpochAfterAscensionWin<TCharacter, TEpoch>(level)` | 指定进阶等级胜利后解锁 |
| `UnlockEpochAfterRunCount<TEpoch>(requiredRuns, requireVictory)` | 累计跑局次数后解锁 |

这些最终都转成 `PostRunEpochUnlockRule`。

也可以直接注册自定义规则：

```csharp
unlocks.RegisterPostRunRule(
    PostRunEpochUnlockRule.Create(
        epochId: new MyEpoch().Id,
        description: "在任意一次被放弃的 5 层进阶局后解锁",
        shouldUnlock: ctx => ctx.IsAbandoned && ctx.AscensionLevel >= 5));
```

---

:::

## Counted Progression Rules{lang="en"}

::: en

| Method | Description |
|---|---|
| `UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(count)` | Elite kill count |
| `UnlockEpochAfterBossVictories<TCharacter, TEpoch>(count)` | Boss kill count |
| `UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>()` | Ascension 1 win |
| `RevealAscensionAfterEpoch<TCharacter, TEpoch>()` | Show ascension after the epoch |
| `UnlockCharacterAfterRunAs<TCharacter, TEpoch>()` | Unlock character after using that character |

---

:::

## 累计进度型规则{lang="zh-CN"}

::: zh-CN

| 方法 | 说明 |
|---|---|
| `UnlockEpochAfterEliteVictories<TCharacter, TEpoch>(count)` | 精英击杀数 |
| `UnlockEpochAfterBossVictories<TCharacter, TEpoch>(count)` | Boss 击杀数 |
| `UnlockEpochAfterAscensionOneWin<TCharacter, TEpoch>()` | 进阶 1 胜利 |
| `RevealAscensionAfterEpoch<TCharacter, TEpoch>()` | 纪元后显示进阶 |
| `UnlockCharacterAfterRunAs<TCharacter, TEpoch>()` | 使用角色后解锁角色 |

---

:::

## Compatibility Patches{lang="en"}

::: en

> This section explains how vanilla progression limits mod characters and how RitsuLib bridges those gaps.

Several vanilla progression checks assume vanilla characters and do not naturally include mod characters. RitsuLib applies narrow bridge patches so registered unlock rules still apply at those checkpoints:

- Elite kill count → epoch checks
- Boss kill count → epoch checks
- Ascension 1 → epoch checks
- Post-run character-unlock epochs
- Ascension reveal unlock checks

These patches do not replace vanilla progression; they only add a bridge where vanilla would skip mod characters. That is why the unlock registry stores rules explicitly by `ModelId` instead of inferring all progression from the timeline graph alone.

---

:::

## 兼容补丁{lang="zh-CN"}

::: zh-CN

> 以下解释原版进度系统对 Mod 角色的局限性，以及 RitsuLib 的桥接策略。

原版的若干进度检查是按原版角色设计的，不会自然支持 Mod 角色。RitsuLib 通过以下桥接补丁，让注册的解锁规则在这些检查点上生效：

- 精英击杀计数的纪元判定桥接
- Boss 击杀计数的纪元判定桥接
- 进阶 1 的纪元判定桥接
- 局后角色解锁纪元桥接
- 进阶显示解锁判定桥接

这些补丁并不重写原版进度系统，只是在原版会跳过 Mod 角色的节点上补一层桥。这也是为什么解锁注册器会显式按 `ModelId` 保存规则，而不是试图仅从时间线图推断全部进度逻辑。

---

:::

## Recommended Pattern{lang="en"}

::: en

For a story-driven character mod:

1. Register character, pools, epochs, and story in one content pack
2. Use `CharacterUnlockEpochTemplate<TCharacter>` for the character unlock epoch
3. Use card / relic / potion epoch templates for follow-up content
4. Use `RequireEpoch<TModel, TEpoch>()` for late-game gates
5. Prefer a small set of clear progression rules over many overlapping ones

---

:::

## 推荐模式{lang="zh-CN"}

::: zh-CN

对故事驱动型角色 Mod：

1. 在一个内容包里注册角色、池、纪元和故事
2. 用 `CharacterUnlockEpochTemplate<TCharacter>` 作为角色解锁纪元
3. 用卡牌/遗物/药水纪元模板做后续内容展开
4. 用 `RequireEpoch<TModel, TEpoch>()` 给后期内容加门槛
5. 使用少量清晰的进度规则，而不是堆叠重叠规则

---

:::

## Builder Example{lang="en"}

::: en

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>()
    .Card<MyCardPool, MyLateCard>()
    .Relic<MyRelicPool, MyLateRelic>()
    .Epoch<MyCharacterEpoch>()
    .Epoch<MyLateContentEpoch>()
    .Story<MyStory>()
    .RequireEpoch<MyLateCard, MyLateContentEpoch>()
    .RequireEpoch<MyLateRelic, MyLateContentEpoch>()
    .UnlockEpochAfterWinAs<MyCharacter, MyCharacterEpoch>()
    .UnlockEpochAfterAscensionWin<MyCharacter, MyLateContentEpoch>(10)
    .Apply();
```

---

:::

## 构建器示例{lang="zh-CN"}

::: zh-CN

```csharp
RitsuLibFramework.CreateContentPack("MyMod")
    .Character<MyCharacter>()
    .Card<MyCardPool, MyLateCard>()
    .Relic<MyRelicPool, MyLateRelic>()
    .Epoch<MyCharacterEpoch>()
    .Epoch<MyLateContentEpoch>()
    .Story<MyStory>()
    .RequireEpoch<MyLateCard, MyLateContentEpoch>()
    .RequireEpoch<MyLateRelic, MyLateContentEpoch>()
    .UnlockEpochAfterWinAs<MyCharacter, MyCharacterEpoch>()
    .UnlockEpochAfterAscensionWin<MyCharacter, MyLateContentEpoch>(10)
    .Apply();
```

---

:::

## Common Mistakes{lang="en"}

::: en

- Registering epochs but forgetting the story that lists those epochs
- Registering story/epochs after the timeline has frozen
- Using `RequireEpoch` without any rule that can actually unlock that epoch
- Stacking many overlapping rules for the same epoch without a clear design
- Assuming vanilla counted progression works for mod characters without registering RitsuLib unlock rules
- Leaving **`UnlocksAfterRunAsType`** at the default on a mod character while **`unlockText`** uses **`{Prerequisite}`** — the character-select hover then shows the generic locked title (often **`???`**). Set **`UnlocksAfterRunAsType`** to the same prerequisite character type as in **`UnlockEpochAfterWinAs<TCharacter, TEpoch>`** / **`UnlockEpochAfterRunAs<…>`** (see [Character & Unlock Templates](/guide/character-and-unlock-scaffolding))

---

:::

## 常见错误{lang="zh-CN"}

::: zh-CN

- 注册了纪元，却忘了注册包含这些纪元的故事
- 在时间线冻结之后才注册故事/纪元
- 给内容设置了 `RequireEpoch`，却没有任何规则能真正解锁该纪元
- 对同一个纪元叠很多重叠解锁规则，却没有明确设计理由
- 误以为原版累计进度逻辑会自动兼容 Mod 角色，而没有注册 RitsuLib 解锁规则
- Mod 角色的 **`unlockText`** 里用了 **`{Prerequisite}`**，却未覆盖 **`UnlocksAfterRunAsType`**（默认为 `null`）——选人界面悬停说明里的前置名会变成通用锁定标题（常显示为 **`???`**）。应将其设为与 **`UnlockEpochAfterWinAs<TCharacter, TEpoch>`** / **`UnlockEpochAfterRunAs<…>`** 中 **`TCharacter`** 一致的前置角色类型（详见 [角色与解锁模板](/guide/character-and-unlock-scaffolding)）

---

:::

## Related Documents{lang="en"}

::: en

- [Character & Unlock Templates](/guide/character-and-unlock-scaffolding)
- [Content Packs & Registries](/guide/content-packs-and-registries)
- [Diagnostics & Compatibility](/guide/diagnostics-and-compatibility)
- [Framework Design](/guide/framework-design)

:::

## 相关文档{lang="zh-CN"}

::: zh-CN

- [角色与解锁模板](/guide/character-and-unlock-scaffolding)
- [内容包与注册器](/guide/content-packs-and-registries)
- [诊断与兼容层](/guide/diagnostics-and-compatibility)
- [框架设计](/guide/framework-design)

:::
