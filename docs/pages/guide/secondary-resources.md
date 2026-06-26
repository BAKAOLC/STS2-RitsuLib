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
    smallIconPath: "res://MyMod/assets/ui/charge_small.png",
    largeIconPath: "res://MyMod/assets/ui/charge_large.png"));
```

The registry expands the local id into a stable compound id. Use that returned `charge.Id` when you need a concrete resource id later.

`baseMaxAmount` is optional. Leave it `null` for resources without a max concept.

With the default layout, one resource stays on one predictable path:

- loc table: `static_hover_tips`
- title key: `{resourceId}.title`
- description key: `{resourceId}.description`

Only pass `locTable`, `titleKey`, or `descriptionKey` when you are intentionally overriding that layout.

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
    smallIconPath: "res://MyMod/assets/ui/charge_small.png",
    largeIconPath: "res://MyMod/assets/ui/charge_large.png"));
```

注册器会把本地 id 扩展成稳定的 compound id。后续需要具体资源 id 时，直接使用返回定义上的 `charge.Id`。

`baseMaxAmount` 是可选的。没有上限概念的资源保持 `null` 即可。

按默认约定，一个资源会稳定落在同一套路径上：

- loc table：`static_hover_tips`
- title key：`{resourceId}.title`
- description key：`{resourceId}.description`

只有在你明确要覆盖这套路径时，再传 `locTable`、`titleKey`、`descriptionKey`。

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
| `Combat` | Currently mostly equivalent to `None`; normal run saves do not restore in-progress combat state |
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
| `Combat` | 目前基本等同于 `None`；普通跑局存档不会恢复进行中的战斗状态 |
| `Run` | 在同一游戏中跨战斗保留 |

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

For optional "kicker" style payments, use card play uses instead of hard costs:

```csharp
card.SecondaryResourceUses()
    .SpendIfAvailable("bonus_charge", charge.Id, 2);
```

Optional spends never block `CanPlay`. If enough resource remains after required costs, RitsuLib spends it during
`SpendResources` and records the line as activated on the play ledger:

```csharp
var ledger = cardPlay.SecondaryResources();
if (ledger.Activated("bonus_charge"))
{
    // extra effect
}
```

You can also declare required costs through the same use set when one card needs multiple named lines:

```csharp
card.SecondaryResourceUses()
    .Require("entry_fee", charge.Id, 1)
    .SpendIfAvailable("bonus_charge", charge.Id, 2);
```

Required uses reserve resource before optional spends, so optional lines cannot consume resource needed by hard costs.
Existing `SecondaryCosts()` entries are treated as unnamed required uses keyed by resource id for compatibility.

For repeatable "kicker stack" payments, use `SpendExtra(...)`. It runs after required payments and shortfall replacement,
but before ordinary optional spends:

```csharp
card.SecondaryResourceUses()
    .Require("seven_stars", stars.Id, 7)
    .SpendExtra(
        "seven_stars_bonus",
        stars.Id,
        perStackAmount: 2,
        maxStacks: null);
```

`perStackAmount` must be positive. `maxStacks: null` means no explicit cap; RitsuLib spends as many full stacks as the
remaining resource can pay. A remainder that cannot complete one stack is not spent.

```csharp
var ledger = cardPlay.SecondaryResources();
var extraSpent = ledger.ExtraSpentByUse("seven_stars_bonus");
var stacks = ledger.ExtraStacksByUse("seven_stars_bonus");
var totalStarsSpent = ledger.Spent(stars.Id);

if (stacks > 0)
{
    // one effect per extra stack
}

if (totalStarsSpent >= 20)
{
    // effect for spending at least 20 stars in total
}
```

Required costs normally block play when the resource is short. To allow a required cost to pass with a shortfall, attach
an explicit insufficient-payment policy:

```csharp
card.SecondaryResourceUses()
    .RequireAllowingShortfall(
        "seven_stars",
        stars.Id,
        7,
        onShortfall: async ctx =>
        {
            // Runs once during SpendResources, after available stars are spent.
            await ApplyShortfallPenalty(ctx.Card, ctx.Shortfall);
        });
```

`ctx.Shortfall` is the remaining unpaid amount. By default, RitsuLib spends the available amount first; pass
`spendAvailable: false` if the shortfall payment should leave the resource untouched.

When another payment source can replace the missing resource, add a side-effect-free resolver. The resolver is allowed
to inspect state during `CanPlay`, but must not mutate state. Return a commit callback to spend the replacement source
later:

```csharp
card.SecondaryResourceUses()
    .RequireAllowingShortfall(
        "seven_stars",
        stars.Id,
        7,
        resolveShortfall: ctx =>
        {
            var backup = SecondaryResourceCmd.Get(ctx.Player, backupStars.Id);
            if (backup < ctx.Shortfall)
                return SecondaryResourceShortfallResolution.None;

            return SecondaryResourceShortfallResolution.Cover(
                ctx.Shortfall,
                async commit =>
                {
                    await SecondaryResourceCmd.Spend(
                        commit.Player,
                        backupStars.Id,
                        commit.CoveredShortfall,
                        commit.Card,
                        commit.Source);
                });
        },
        onShortfall: async ctx =>
        {
            // Runs only for any remaining amount not covered by the replacement payment.
            await ApplyShortfallPenalty(ctx.Card, ctx.Shortfall);
        });
```

If replacement payments cover the full original shortfall, the line is considered playable without a remaining
shortfall penalty. The ledger records all three amounts:

```csharp
var ledger = cardPlay.SecondaryResources();
var original = ledger.OriginalShortfallByUse("seven_stars");
var covered = ledger.CoveredShortfallByUse("seven_stars");
var remaining = ledger.ShortfallByUse("seven_stars");
```

Model, capability, and global hook listeners can also participate in the same pre-commit planning step by implementing
`ResolveSecondaryResourceShortfall(...)`. This hook receives the current resolution and should return a new pure
resolution without mutating gameplay state.

When the permission to use shortfall is itself dynamic, implement
`ModifySecondaryResourceInsufficientPayment(...)`. For example, a relic can leave normal cards blocked by default, then
return `SecondaryResourceInsufficientPayment.AllowPlayWithReplacement(...)` only while that relic is active. This hook
also runs during `CanPlay`, so it must only inspect state and return a policy.

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

对于类似 kicker 的“可选支付并触发额外效果”，使用出牌条款，而不是硬费用：

```csharp
card.SecondaryResourceUses()
    .SpendIfAvailable("bonus_charge", charge.Id, 2);
```

可选支付永远不会阻止 `CanPlay`。如果在必需费用预留后仍有足够资源，RitsuLib 会在 `SpendResources` 阶段消耗它，
并在本次出牌的 ledger 上把该条款标记为已激活：

```csharp
var ledger = cardPlay.SecondaryResources();
if (ledger.Activated("bonus_charge"))
{
    // 额外效果
}
```

如果一张牌需要多个具名行，也可以通过同一个 use set 声明必需费用：

```csharp
card.SecondaryResourceUses()
    .Require("entry_fee", charge.Id, 1)
    .SpendIfAvailable("bonus_charge", charge.Id, 2);
```

必需条款会先预留资源，然后再判断可选支付，所以可选行不会抢走硬费用所需的资源。为了兼容旧代码，已有
`SecondaryCosts()` 条目会被视为以 resource id 为 key 的未具名必需条款。

对于类似“每额外支付一份就叠一层”的 kicker 支付，使用 `SpendExtra(...)`。它会在必需支付和短缺替代之后、
普通可选支付之前运行：

```csharp
card.SecondaryResourceUses()
    .Require("seven_stars", stars.Id, 7)
    .SpendExtra(
        "seven_stars_bonus",
        stars.Id,
        perStackAmount: 2,
        maxStacks: null);
```

`perStackAmount` 必须为正数。`maxStacks: null` 表示不设置显式上限；RitsuLib 会按剩余资源能支付的完整份数尽可能消耗。
不足一整份的余数不会被消耗。

```csharp
var ledger = cardPlay.SecondaryResources();
var extraSpent = ledger.ExtraSpentByUse("seven_stars_bonus");
var stacks = ledger.ExtraStacksByUse("seven_stars_bonus");
var totalStarsSpent = ledger.Spent(stars.Id);

if (stacks > 0)
{
    // 每层额外支付触发一次
}

if (totalStarsSpent >= 20)
{
    // 本次总共消耗至少 20 个辉星时的效果
}
```

必需费用默认会在资源不足时阻止出牌。如果某个必需费用允许短缺通过，可以显式附加短缺支付策略：

```csharp
card.SecondaryResourceUses()
    .RequireAllowingShortfall(
        "seven_stars",
        stars.Id,
        7,
        onShortfall: async ctx =>
        {
            // 在 SpendResources 阶段运行一次；可用辉星已经先被消耗。
            await ApplyShortfallPenalty(ctx.Card, ctx.Shortfall);
        });
```

`ctx.Shortfall` 是剩余未支付数量。默认会先消耗可用资源；如果短缺支付不应改变该资源，传入
`spendAvailable: false`。

如果缺少的资源可以由其他支付来源替代，使用无副作用的 resolver。resolver 可以在 `CanPlay` 阶段读取状态，
但不能修改状态；真正的替代支付应放在返回结果的 commit 回调中：

```csharp
card.SecondaryResourceUses()
    .RequireAllowingShortfall(
        "seven_stars",
        stars.Id,
        7,
        resolveShortfall: ctx =>
        {
            var backup = SecondaryResourceCmd.Get(ctx.Player, backupStars.Id);
            if (backup < ctx.Shortfall)
                return SecondaryResourceShortfallResolution.None;

            return SecondaryResourceShortfallResolution.Cover(
                ctx.Shortfall,
                async commit =>
                {
                    await SecondaryResourceCmd.Spend(
                        commit.Player,
                        backupStars.Id,
                        commit.CoveredShortfall,
                        commit.Card,
                        commit.Source);
                });
        },
        onShortfall: async ctx =>
        {
            // 只处理没有被替代支付覆盖的剩余短缺。
            await ApplyShortfallPenalty(ctx.Card, ctx.Shortfall);
        });
```

当替代支付完全覆盖原始短缺时，该行会被视为没有剩余短缺的可打出支付，不再触发短缺惩罚。ledger 会记录三段数量：

```csharp
var ledger = cardPlay.SecondaryResources();
var original = ledger.OriginalShortfallByUse("seven_stars");
var covered = ledger.CoveredShortfallByUse("seven_stars");
var remaining = ledger.ShortfallByUse("seven_stars");
```

模型、capability 和全局 hook listener 也可以实现 `ResolveSecondaryResourceShortfall(...)` 参与同一个提交前规划步骤。
这个 hook 会收到当前解析结果，并应返回一个新的纯解析结果；不要在这里修改游戏状态。

如果“是否允许短缺”本身也是动态的，实现 `ModifySecondaryResourceInsufficientPayment(...)`。例如某个遗物可以让普通卡
默认仍然被不足资源阻止，但在遗物生效时才返回 `SecondaryResourceInsufficientPayment.AllowPlayWithReplacement(...)`。
这个 hook 同样会在 `CanPlay` 阶段运行，因此只能读取状态并返回策略。

:::

## Hooks, UI, And Text{lang="en"}

::: en

Implement `ISecondaryResourceHookListener` on models or capabilities when the resource should react to gameplay:

- Modify gain, max amount, costs, or captured secondary X values
- Dynamically modify whether a required shortfall blocks play, is allowed, or can be replaced
- Use `ModifySecondaryResourceCostLate(...)` when a cost modifier should run after normal secondary cost modifiers,
  mirroring the game's late energy-cost pass
- Veto gain, spend, or built-in reset steps
- `ShouldSpendSecondaryResource(...)` blocks `CanPlay` for required card costs; optional spend lines simply become
  inactive when vetoed
- React after change, spend, or reset

For process-wide behavior, register a global listener through `SecondaryResourceHook.RegisterGlobalListener(...)`.

For combat presentation:

- `AlwaysShowInCombatUi(...)` and `AlwaysShowInCombatUiForCharacter(...)` keep a resource visible before it is gained
- `RegisterCombatUi(...)`, `RegisterCardUi(...)`, and `RegisterMultiplayerPlayerStateUi(...)` attach custom Godot nodes through the node-attachment runtime
- custom `RegisterCombatUi(...)` updates should use `ctx.VisibleDefinitions` or `definition.IsVisibleInCombatUi(ctx.Player)` when deciding whether their nodes are visible
- `NSecondaryResourceCardCostUi` is a simple single-resource card-cost wrapper node for `RegisterCardUi(...)`; bind one resource id per node and place each node yourself
- Built-in `NSecondaryResourceIcon` / `NSecondaryResourceCounter` hover tips always use the resource title and description. Pass a `SecondaryResourceIconStyle` with `HoverTip = SecondaryResourceHoverTipStyle.Default with { ResolveGlobalPosition = ... }` when you need custom placement. Hover-tip title and description receive `Amount`, `HasMaxAmount`, and `MaxAmount` LocString variables so localization can decide how to show dynamic amounts.

For text:

- `SecondaryResourceText.GetIconTag(...)` returns a rich-text `[img]...[/img]` icon tag
- `SecondaryResourceVars.For(...)` and `SecondaryResourceVars.ForLocal(...)` create SmartFormat-friendly dynamic vars
- `{secondaryResource:secondaryResourceIcons(charge,1)}` renders a fixed amount from a registered resource id or
  unique local id
- `{Cost:secondaryResourceIcons(charge)}` renders a dynamic var amount with a fixed registered resource id or unique
  local id
- Titles and descriptions come from the resource loc table and keys

:::

## Hook、UI 与文本{lang="zh-CN"}

::: zh-CN

如果资源需要响应游戏逻辑，可以在模型或 capability 上实现 `ISecondaryResourceHookListener`：

- 修正 gain、max、cost 或捕获到的次级 X 值
- 动态修正必需费用短缺是阻止出牌、允许短缺，还是可被替代支付覆盖
- 如果某个费用修正应在普通次级费用修正之后执行，使用 `ModifySecondaryResourceCostLate(...)`，对应游戏的 late
  energy-cost pass
- 阻止 gain、spend 或内建 reset
- `ShouldSpendSecondaryResource(...)` 会让必需卡牌费用在 `CanPlay` 阶段被阻止；可选支付行被阻止时只会变为未激活
- 在 change、spend、reset 之后执行附加逻辑

进程级行为可通过 `SecondaryResourceHook.RegisterGlobalListener(...)` 注册全局监听器。

对于战斗表现层：

- `AlwaysShowInCombatUi(...)` 和 `AlwaysShowInCombatUiForCharacter(...)` 可以让资源在尚未获得前也显示出来
- `RegisterCombatUi(...)`、`RegisterCardUi(...)`、`RegisterMultiplayerPlayerStateUi(...)` 可以借助 node attachment 体系挂接自定义 Godot 节点
- 自定义 `RegisterCombatUi(...)` 更新逻辑应使用 `ctx.VisibleDefinitions` 或 `definition.IsVisibleInCombatUi(ctx.Player)` 判断节点是否可见
- `NSecondaryResourceCardCostUi` 是用于 `RegisterCardUi(...)` 的单资源简易卡牌费用包装节点；每个节点绑定一个 resource id，并由注册方分别指定位置
- 内建 `NSecondaryResourceIcon` / `NSecondaryResourceCounter` 的 hover tip 始终使用资源的 title 和 description。需要自定义位置时，传入带 `HoverTip = SecondaryResourceHoverTipStyle.Default with { ResolveGlobalPosition = ... }` 的 `SecondaryResourceIconStyle`。hover-tip title 和 description 会收到 `Amount`、`HasMaxAmount` 和 `MaxAmount` 这些 LocString 变量，由本地化文本决定如何显示动态数量。

对于文本表现：

- `SecondaryResourceText.GetIconTag(...)` 返回富文本 `[img]...[/img]` 图标标签
- `SecondaryResourceVars.For(...)` 和 `SecondaryResourceVars.ForLocal(...)` 用于 SmartFormat 动态变量
- `{secondaryResource:secondaryResourceIcons(charge,1)}` 用已注册资源 id 或唯一 local id 渲染固定数量
- `{Cost:secondaryResourceIcons(charge)}` 用固定的已注册资源 id 或唯一 local id 渲染 dynamic var 数量
- 标题和描述来自资源定义上的本地化表与 key

:::
