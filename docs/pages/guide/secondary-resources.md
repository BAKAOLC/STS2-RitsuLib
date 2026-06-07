---
title:
  en: Secondary Resources
  zh-CN: 次级资源
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Register A Resource{lang="en"}

::: en

Use `RitsuLibFramework.GetSecondaryResourceRegistry(modId)` to declare combat resources that behave like mod-owned energy, ammo, stance counters, or other card-payment state.

```csharp
var resources = RitsuLibFramework.GetSecondaryResourceRegistry("MyMod");

var charge = resources.Register("charge", new SecondaryResourceDefinition(
    defaultAmount: 0,
    baseMaxAmount: 3,
    turnStartPolicy: SecondaryResourceTurnStartPolicy.ResetToMax,
    persistencePolicy: SecondaryResourcePersistencePolicy.Combat,
    locTable: "my_mod_resources",
    titleKey: "my_mod.charge.title",
    descriptionKey: "my_mod.charge.description",
    smallIconPath: "res://MyMod/assets/ui/charge_small.png",
    largeIconPath: "res://MyMod/assets/ui/charge_large.png"));
```

The registry expands the local id into a stable compound id. Use that returned `charge.Id` when you need a concrete resource id later.

`baseMaxAmount` is optional. Leave it `null` for resources without a max concept.

:::

## 注册资源{lang="zh-CN"}

::: zh-CN

用 `RitsuLibFramework.GetSecondaryResourceRegistry(modId)` 声明战斗资源。它适合表达模组自定义的能量、弹药、姿态计数，或其他需要参与卡牌支付的状态。

```csharp
var resources = RitsuLibFramework.GetSecondaryResourceRegistry("MyMod");

var charge = resources.Register("charge", new SecondaryResourceDefinition(
    defaultAmount: 0,
    baseMaxAmount: 3,
    turnStartPolicy: SecondaryResourceTurnStartPolicy.ResetToMax,
    persistencePolicy: SecondaryResourcePersistencePolicy.Combat,
    locTable: "my_mod_resources",
    titleKey: "my_mod.charge.title",
    descriptionKey: "my_mod.charge.description",
    smallIconPath: "res://MyMod/assets/ui/charge_small.png",
    largeIconPath: "res://MyMod/assets/ui/charge_large.png"));
```

注册器会把本地 id 扩展成稳定的 compound id。后续需要具体资源 id 时，直接使用返回定义上的 `charge.Id`。

`baseMaxAmount` 是可选的。没有上限概念的资源保持 `null` 即可。

:::

## Mutate Runtime State{lang="en"}

::: en

Use `SecondaryResourceCmd` to read and change values during combat:

```csharp
var current = SecondaryResourceCmd.Get(player, charge.Id);
var max = SecondaryResourceCmd.GetMax(player, charge.Id);

await SecondaryResourceCmd.Gain(player, charge.Id, 1, source: card);
await SecondaryResourceCmd.Lose(player, charge.Id, 1, source: relic);
await SecondaryResourceCmd.Set(player, charge.Id, 2, source: power);

var spent = await SecondaryResourceCmd.Spend(player, charge.Id, 2, card, source: card);
await SecondaryResourceCmd.Reset(player, charge.Id, toMax: true);
```

Built-in turn-start handling comes from `SecondaryResourceTurnStartPolicy`:

| Policy | Effect |
| --- | --- |
| `None` | Keep the current amount |
| `ResetToMax` | Set current amount to the hook-adjusted max |
| `AddMaxToCurrent` | Add the hook-adjusted max to the current amount |
| `Clear` | Set current amount to the resource minimum |

Persistence is separate:

| Policy | Saved scope |
| --- | --- |
| `None` | Runtime only |
| `Combat` | Restore with the current combat |
| `Run` | Persist across combats in the same run |

:::

## 修改运行时状态{lang="zh-CN"}

::: zh-CN

战斗中读取和修改数值时，使用 `SecondaryResourceCmd`：

```csharp
var current = SecondaryResourceCmd.Get(player, charge.Id);
var max = SecondaryResourceCmd.GetMax(player, charge.Id);

await SecondaryResourceCmd.Gain(player, charge.Id, 1, source: card);
await SecondaryResourceCmd.Lose(player, charge.Id, 1, source: relic);
await SecondaryResourceCmd.Set(player, charge.Id, 2, source: power);

var spent = await SecondaryResourceCmd.Spend(player, charge.Id, 2, card, source: card);
await SecondaryResourceCmd.Reset(player, charge.Id, toMax: true);
```

内建的回合开始行为由 `SecondaryResourceTurnStartPolicy` 控制：

| 策略 | 效果 |
| --- | --- |
| `None` | 保持当前数量 |
| `ResetToMax` | 把当前数量设为经过 hook 修正后的最大值 |
| `AddMaxToCurrent` | 将经过 hook 修正后的最大值加到当前数量 |
| `Clear` | 把当前数量设为资源最小值 |

持久化范围单独由 `SecondaryResourcePersistencePolicy` 控制：

| 策略 | 存储范围 |
| --- | --- |
| `None` | 仅运行时存在 |
| `Combat` | 随当前战斗恢复 |
| `Run` | 在同一跑局中跨战斗保留 |

:::

## Attach Card Costs{lang="en"}

::: en

Secondary resources integrate into `CardModel.CanPlay`, `SpendResources`, auto-play bookkeeping, and end-of-turn cleanup. Attach costs directly to cards:

```csharp
card.SecondaryCosts()
    .Set(charge.Id, 1)
    .Set(
        charge.Id,
        SecondaryResourceCost.X(),
        SecondaryResourceCostDuration.UntilPlayed);
```

Use `SecondaryResourceCostDuration` to scope temporary modifiers:

| Duration | Cleared when |
| --- | --- |
| `Permanent` | You replace or clear it manually |
| `UntilPlayed` | The next successful play finishes |
| `ThisTurn` | End of turn cleanup runs |
| `ThisCombat` | The card object leaves combat |

When the player cannot pay all material secondary costs, `CanPlay` fails automatically.

:::

## 附加卡牌费用{lang="zh-CN"}

::: zh-CN

次级资源已经接入 `CardModel.CanPlay`、`SpendResources`、自动打出流程和回合结束清理。把费用直接挂到卡牌对象上即可：

```csharp
card.SecondaryCosts()
    .Set(charge.Id, 1)
    .Set(
        charge.Id,
        SecondaryResourceCost.X(),
        SecondaryResourceCostDuration.UntilPlayed);
```

用 `SecondaryResourceCostDuration` 控制临时费用的生命周期：

| 持续时间 | 清除时机 |
| --- | --- |
| `Permanent` | 手动覆盖或清除 |
| `UntilPlayed` | 下一次成功打出结束后 |
| `ThisTurn` | 回合结束清理时 |
| `ThisCombat` | 卡牌对象离开战斗时 |

玩家无法支付全部有效次级费用时，`CanPlay` 会自动失败。

:::

## Hooks, UI, And Text{lang="en"}

::: en

Implement `ISecondaryResourceHookListener` on models or capabilities when the resource should react to gameplay:

- Modify gain, max amount, costs, or captured secondary X values
- Veto gain, spend, or built-in reset steps
- React after change, spend, or reset

For process-wide behavior, register a global listener through `SecondaryResourceHook.RegisterGlobalListener(...)`.

For combat presentation:

- `AlwaysShowInCombatUi(...)` and `AlwaysShowInCombatUiForCharacter(...)` keep a resource visible before it is gained
- `RegisterCombatUi(...)`, `RegisterCardUi(...)`, and `RegisterMultiplayerPlayerStateUi(...)` attach custom Godot nodes through the node-attachment runtime

For text:

- `SecondaryResourceText.GetIconTag(...)` returns a rich-text `[img]...[/img]` icon tag
- `SecondaryResourceVars.For(...)` and `SecondaryResourceVars.ForLocal(...)` create SmartFormat-friendly dynamic vars
- Titles and descriptions come from the resource loc table and keys

:::

## Hook、UI 与文本{lang="zh-CN"}

::: zh-CN

如果资源需要响应游戏逻辑，可以在模型或 capability 上实现 `ISecondaryResourceHookListener`：

- 修正 gain、max、cost 或捕获到的次级 X 值
- 阻止 gain、spend 或内建 reset
- 在 change、spend、reset 之后执行附加逻辑

进程级行为可通过 `SecondaryResourceHook.RegisterGlobalListener(...)` 注册全局监听器。

对于战斗表现层：

- `AlwaysShowInCombatUi(...)` 和 `AlwaysShowInCombatUiForCharacter(...)` 可以让资源在尚未获得前也显示出来
- `RegisterCombatUi(...)`、`RegisterCardUi(...)`、`RegisterMultiplayerPlayerStateUi(...)` 可以借助 node attachment 体系挂接自定义 Godot 节点

对于文本表现：

- `SecondaryResourceText.GetIconTag(...)` 返回富文本 `[img]...[/img]` 图标标签
- `SecondaryResourceVars.For(...)` 和 `SecondaryResourceVars.ForLocal(...)` 用于 SmartFormat 动态变量
- 标题和描述来自资源定义上的本地化表与 key

:::
