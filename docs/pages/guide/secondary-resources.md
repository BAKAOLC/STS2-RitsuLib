---
title:
  en: Secondary Resources
  zh-CN: 次级资源
cover: https://wrxinyue.s3.bitiful.net/slay-the-spire-2-wallpaper.webp
---

## Register A Resource{lang="en"}

::: en

Use `RitsuLibFramework.GetSecondaryResourceRegistry(modId)` to declare combat resources such as mod-defined energy, ammunition, stance counters, or other card-payment state.

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

The registry expands the mod-local ID into a stable full resource ID. Use the returned `charge.Id` whenever another API needs that full ID. Registering the same ID again returns the definition registered first.

`baseMaxAmount` is optional. Leave it `null` for resources without a max concept.

By default, each resource uses the following localization layout:

- localization table: `static_hover_tips`
- title key: `{resourceId}.title`
- description key: `{resourceId}.description`

Only pass `locTable`, `titleKey`, or `descriptionKey` when you need to override that layout. Surrounding whitespace in these values and the icon paths is ignored.

:::

## 注册资源{lang="zh-CN"}

::: zh-CN

使用 `RitsuLibFramework.GetSecondaryResourceRegistry(modId)` 声明战斗资源。它适合表示模组自定义的能量、弹药、姿态计数，以及其他参与卡牌支付的状态。

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

注册表会把模组内 ID 扩展成稳定的完整资源 ID。其他 API 需要完整 ID 时，使用返回定义中的 `charge.Id`。重复注册同一 ID 时会返回最先注册的定义。

`baseMaxAmount` 是可选的。没有上限概念的资源保持 `null` 即可。

默认情况下，每种资源使用以下本地化约定：

- 本地化表：`static_hover_tips`
- 标题键：`{resourceId}.title`
- 说明键：`{resourceId}.description`

仅在需要覆盖这套约定时传入 `locTable`、`titleKey` 或 `descriptionKey`。这些值和图标路径的首尾空白会被忽略。

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

Built-in turn-start behavior is selected with `SecondaryResourceTurnStartPolicy`:

| Policy | Effect |
| --- | --- |
| `None` | Keep the current amount |
| `ResetToMax` | Set the current amount to the hook-adjusted maximum |
| `AddMaxToCurrent` | Add the hook-adjusted maximum to the current amount |
| `Clear` | Set current amount to the resource minimum |

Persistence is separate:

| Policy | Saved scope |
| --- | --- |
| `None` | Runtime only |
| `Combat` | Included only in explicitly requested combat snapshots; excluded from normal run-save synchronization |
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

内置的回合开始行为由 `SecondaryResourceTurnStartPolicy` 控制：

| 策略 | 效果 |
| --- | --- |
| `None` | 保持当前数量 |
| `ResetToMax` | 将当前数量设为经钩子修正后的最大值 |
| `AddMaxToCurrent` | 将经钩子修正后的最大值加到当前数量 |
| `Clear` | 将当前数量设为资源下限 |

持久化范围单独由 `SecondaryResourcePersistencePolicy` 控制：

| 策略 | 存储范围 |
| --- | --- |
| `None` | 仅在运行时存在 |
| `Combat` | 仅包含在显式请求的战斗快照中；常规跑局存档同步不会保存 |
| `Run` | 在同一局游戏中跨战斗保留 |

:::

## Attach Card Costs{lang="en"}

::: en

Secondary resources integrate with `CardModel.CanPlay`, `SpendResources`, auto-play bookkeeping, and end-of-turn cleanup. Fixed and X costs can be attached directly to a card:

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
| `Permanent` | Replaced or cleared explicitly |
| `UntilPlayed` | After the card is next played successfully |
| `ThisTurn` | End of turn cleanup runs |
| `ThisCombat` | The card's combat instance is discarded |

When a required secondary-resource cost cannot be paid and its insufficient-payment policy does not allow a shortfall, `CanPlay` fails automatically.

For optional payments that enable an additional effect, use named card-play uses instead of hard costs:

```csharp
card.SecondaryResourceUses()
    .SpendIfAvailable("bonus_charge", charge.Id, 2);
```

Optional payments never block `CanPlay`. If enough resource remains after required payments, RitsuLib spends it during
`SpendResources` and marks the named line as active in the play ledger:

```csharp
var ledger = cardPlay.SecondaryResources();
if (ledger.Activated("bonus_charge"))
{
    // extra effect
}
```

You can also declare required costs through the same use collection when one card needs multiple named entries:

```csharp
card.SecondaryResourceUses()
    .Require("entry_fee", charge.Id, 1)
    .SpendIfAvailable("bonus_charge", charge.Id, 2);
```

Required uses reserve resources before extra and optional payments, so later entries cannot consume resources needed by
hard costs. For compatibility, each `SecondaryCosts()` entry becomes a required use whose ID is the resource ID.

Use IDs must be unique across `SecondaryCosts()`, `SecondaryResourceUses()`, and capability-contributed uses on the same
card. RitsuLib rejects duplicate IDs before committing any payment.

For a repeatable payment that buys as many full stacks as possible, use `SpendExtra(...)`. It is resolved after required
payments and shortfall replacement, but before ordinary optional payments:

```csharp
card.SecondaryResourceUses()
    .Require("seven_stars", stars.Id, 7)
    .SpendExtra(
        "seven_stars_bonus",
        stars.Id,
        perStackAmount: 2,
        maxStacks: null);
```

`perStackAmount` must be positive, and repeatable payments cannot use an X cost. `maxStacks: null` means no explicit cap;
RitsuLib pays for as many full stacks as the remaining resource allows. A remainder that cannot complete one stack is
not spent.

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

Required costs normally block play when the resource is insufficient. To allow a required cost to pass with an unpaid
amount, attach an explicit insufficient-payment policy:

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

`ctx.Shortfall` is the remaining unpaid amount. By default, RitsuLib spends the available resource first; pass
`spendAvailable: false` when the resource itself should remain untouched.

When another payment source can replace the missing resource, add a side-effect-free resolver. The resolver may inspect
state during `CanPlay`, but must not mutate it. Return a commit callback that pays the replacement source later:

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

If replacement payments cover the entire original shortfall, the entry is playable without a remaining-shortfall
callback. The ledger records the original, covered, and remaining amounts:

```csharp
var ledger = cardPlay.SecondaryResources();
var original = ledger.OriginalShortfallByUse("seven_stars");
var covered = ledger.CoveredShortfallByUse("seven_stars");
var remaining = ledger.ShortfallByUse("seven_stars");
```

Models, capabilities, and global listeners can also participate in the same pre-commit planning step by implementing
`ResolveSecondaryResourceShortfall(...)`. The hook receives the current resolution and must return a non-null,
side-effect-free replacement resolution without mutating gameplay state.

When permission to leave an amount unpaid is dynamic, implement
`ModifySecondaryResourceInsufficientPayment(...)`. For example, a relic can keep normal cards blocked by default and
return `SecondaryResourceInsufficientPayment.AllowPlayWithReplacement(...)` only while the relic is active. This hook
also runs during `CanPlay`, so it may only inspect state and must return a non-null policy.

::: 

## 附加卡牌费用{lang="zh-CN"}

::: zh-CN

次级资源已接入 `CardModel.CanPlay`、`SpendResources`、自动打出记录和回合结束清理。固定费用和 X 费用可以直接附加到卡牌：

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
| `Permanent` | 被显式替换或清除时 |
| `UntilPlayed` | 卡牌下一次成功打出后 |
| `ThisTurn` | 回合结束清理时 |
| `ThisCombat` | 卡牌的战斗实例被丢弃时 |

玩家无法支付必需的次级资源费用，且资源不足支付策略不允许留下缺口时，`CanPlay` 会自动失败。

需要“可选支付并启用额外效果”时，使用具名出牌条目，而不是硬性费用：

```csharp
card.SecondaryResourceUses()
    .SpendIfAvailable("bonus_charge", charge.Id, 2);
```

可选支付永远不会阻止 `CanPlay`。如果为必需支付预留后仍有足够资源，RitsuLib 会在 `SpendResources` 阶段支付，
并在本次出牌的支付记录中将对应具名条目标记为已激活：

```csharp
var ledger = cardPlay.SecondaryResources();
if (ledger.Activated("bonus_charge"))
{
    // 额外效果
}
```

如果一张牌需要多个具名条目，也可以通过同一个出牌条目集合声明必需费用：

```csharp
card.SecondaryResourceUses()
    .Require("entry_fee", charge.Id, 1)
    .SpendIfAvailable("bonus_charge", charge.Id, 2);
```

必需条目会先预留资源，再解析额外支付和可选支付，因此后续条目不会占用硬性费用所需的资源。为兼容旧代码，
每个 `SecondaryCosts()` 条目都会转成以资源 ID 作为条目 ID 的必需支付。

同一张牌上的 `SecondaryCosts()`、`SecondaryResourceUses()` 和能力所贡献的条目必须使用互不重复的条目 ID。
RitsuLib 会在扣除任何资源前拒绝重复 ID。

需要“每额外支付一份就获得一层”的可重复支付时，使用 `SpendExtra(...)`。它会在必需支付和缺口替代之后、
普通可选支付之前解析：

```csharp
card.SecondaryResourceUses()
    .Require("seven_stars", stars.Id, 7)
    .SpendExtra(
        "seven_stars_bonus",
        stars.Id,
        perStackAmount: 2,
        maxStacks: null);
```

`perStackAmount` 必须为正数，且可重复支付不能使用 X 费用。`maxStacks: null` 表示不设置显式上限；
RitsuLib 会根据剩余资源尽可能支付完整份数，不足一份的余数不会被支付。

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

必需费用默认会在资源不足时阻止出牌。如果某项必需费用允许留下缺口，可以显式附加资源不足支付策略：

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

`ctx.Shortfall` 是剩余未支付数量。默认会先支付可用资源；如果缺口处理不应改变该资源，传入
`spendAvailable: false`。

如果缺少的资源可以由其他支付来源替代，应使用无副作用的解析器。解析器可以在 `CanPlay` 阶段读取状态，
但不能修改状态；真正的替代支付应放在返回方案的提交回调中：

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

替代支付完全补足原始缺口后，该条目可正常出牌，也不会再调用剩余缺口回调。支付记录会保存原始、
已补足和剩余三种数量：

```csharp
var ledger = cardPlay.SecondaryResources();
var original = ledger.OriginalShortfallByUse("seven_stars");
var covered = ledger.CoveredShortfallByUse("seven_stars");
var remaining = ledger.ShortfallByUse("seven_stars");
```

模型、能力和全局监听器也可以实现 `ResolveSecondaryResourceShortfall(...)`，参与同一个提交前规划步骤。
该钩子会收到当前解析结果，并且必须在不修改游戏状态的前提下返回非空、无副作用的替代支付方案。

如果“是否允许留下未支付数量”本身是动态的，应实现 `ModifySecondaryResourceInsufficientPayment(...)`。
例如，某件遗物可以让普通卡默认仍因资源不足而无法打出，只在遗物生效时返回
`SecondaryResourceInsufficientPayment.AllowPlayWithReplacement(...)`。该钩子同样会在 `CanPlay` 阶段运行，
因此只能读取状态，并且必须返回非空策略。

:::

## Hooks, UI, And Text{lang="en"}

::: en

Implement `ISecondaryResourceHookListener` on a model or capability when secondary-resource behavior must react to gameplay:

- Modify gains, maximum amounts, costs, or secondary X values captured for a card play
- Dynamically decide whether an unpaid required amount blocks play, is allowed, or can be replaced
- Use `ModifySecondaryResourceCostLate(...)` when a cost modifier should run after normal secondary cost modifiers,
  mirroring the game's late energy-cost pass
- Prevent gains, payments, or calls to `SecondaryResourceCmd.Reset(...)`; this includes the `ResetToMax` turn-start policy,
  but not `AddMaxToCurrent` or `Clear`
- `ShouldSpendSecondaryResource(...)` blocks `CanPlay` for required card costs; optional spend lines simply become
  inactive when vetoed
- React after an amount change, payment, remaining-shortfall payment, or reset

For process-wide behavior, register a global listener through `SecondaryResourceHook.RegisterGlobalListener(...)`.

For the game's energy and Stars resources, use `IPlayerResourceHookListener` on a model or capability, or register a global listener
through `PlayerResourceHook.RegisterGlobalListener(...)`. `AfterPlayerEnergyGained(...)` and
`AfterPlayerStarsGained(...)` run after successful `PlayerCmd.GainEnergy(...)` / `PlayerCmd.GainStars(...)` calls and
receive the actual gained amount plus old and new totals.

For combat presentation:

- `AlwaysShowInCombatUi(...)` and `AlwaysShowInCombatUiForCharacter(...)` keep a resource visible before it is gained
- `RegisterCombatUi(...)`, `RegisterCardUi(...)`, and `RegisterMultiplayerPlayerStateUi(...)` attach custom Godot nodes through the node-attachment system
- custom `RegisterCombatUi(...)` updates should use `ctx.VisibleDefinitions` or `definition.IsVisibleInCombatUi(ctx.Player)` to decide whether their nodes are visible
- the `RegisterCombatUi(...)` overload with `SecondaryResourceCombatUiChangedHandler` receives formal UI change
  notifications after the combat UI refreshes. Use `ctx.Definition`, `ctx.OldAmount`, `ctx.NewAmount`, `ctx.Delta`,
  `ctx.Reason`, and `ctx.Source` to play presentation-only feedback such as particles or pulses
- registered UI callbacks are isolated: a callback failure is logged once and does not abort the rest of the UI refresh
- `NSecondaryResourceCardCostUi` is a single-entry card-cost node for `RegisterCardUi(...)`; bind one resource or one
  named use per node and place each node explicitly
- When a card-cost node is deliberately placed in the game's Stars-cost slot, set `SecondaryResourceCardCostUiStyle.ReserveVanillaStarCostSlot = true` so enchanted cards retain the game's Stars-cost enchantment-tab layout. For custom grouped cost UI, call `SecondaryResourceCardUiLayout.ReserveVanillaStarCostSlot(ctx.Parent)` from the card UI updater when the visible group occupies that slot.
- Built-in `NSecondaryResourceIcon` / `NSecondaryResourceCounter` hover tips always use the resource title and description. Pass a `SecondaryResourceIconStyle` with `HoverTip = SecondaryResourceHoverTipStyle.Default with { ResolveGlobalPosition = ... }` when you need custom placement. Hover-tip title and description receive `Amount`, `HasMaxAmount`, and `MaxAmount` `LocString` variables so localization can decide how to show dynamic amounts.

```csharp
resources.RegisterCombatUi(
    "charge_feedback",
    parent => new MyChargeFeedbackNode(),
    update: ctx => ctx.Node.Refresh(ctx.Player, ctx.VisibleDefinitions),
    changed: ctx =>
    {
        if (ctx.Definition.Id == charge.Id && ctx.Delta > 0)
            ctx.Node.PlayGainFeedback(ctx.Delta, ctx.Source);
    });
```

For text:

- `SecondaryResourceText.GetIconTag(...)` returns a rich-text `[img]...[/img]` icon tag
- `SecondaryResourceVars.For(...)` and `SecondaryResourceVars.ForLocal(...)` create SmartFormat-friendly dynamic variables
- `{secondaryResource:secondaryResourceIcons(charge,1)}` renders a fixed amount from a registered resource ID or an
  unambiguous mod-local ID
- `{Cost:secondaryResourceIcons(charge)}` renders a dynamic variable using a registered resource ID or an unambiguous
  mod-local ID
- titles and descriptions come from the resource's localization table and keys

:::

## Hook、UI 与文本{lang="zh-CN"}

::: zh-CN

如果次级资源行为需要响应游戏逻辑，可以在模型或能力上实现 `ISecondaryResourceHookListener`：

- 修正获得量、最大数量、费用或一次出牌所记录的次级 X 值
- 动态决定必需支付的未支付数量会阻止出牌、允许保留，还是可由替代支付补足
- 某项费用修正需要在常规次级费用修正之后执行时，使用 `ModifySecondaryResourceCostLate(...)`，对应游戏的后置能量费用修正阶段
- 阻止资源增加、支付或对 `SecondaryResourceCmd.Reset(...)` 的调用；这包括回合开始的 `ResetToMax`，
  但不包括 `AddMaxToCurrent` 或 `Clear`
- `ShouldSpendSecondaryResource(...)` 会使必需卡牌费用无法通过 `CanPlay`；可选支付被阻止时只会变为未激活
- 在数量变化、支付、仍有缺口的支付或重置之后执行附加逻辑

进程级行为可通过 `SecondaryResourceHook.RegisterGlobalListener(...)` 注册全局监听器。

游戏内置的能量和辉星使用模型或能力上的 `IPlayerResourceHookListener`，也可以通过
`PlayerResourceHook.RegisterGlobalListener(...)` 注册全局监听器。`AfterPlayerEnergyGained(...)` 和
`AfterPlayerStarsGained(...)` 会在成功调用 `PlayerCmd.GainEnergy(...)` / `PlayerCmd.GainStars(...)` 后运行，
上下文包含实际获得量以及变化前后的总量。

对于战斗表现层：

- `AlwaysShowInCombatUi(...)` 和 `AlwaysShowInCombatUiForCharacter(...)` 可以让资源在尚未获得前也显示出来
- `RegisterCombatUi(...)`、`RegisterCardUi(...)`、`RegisterMultiplayerPlayerStateUi(...)` 可通过节点挂载机制附加自定义 Godot 节点
- 自定义 `RegisterCombatUi(...)` 更新逻辑应使用 `ctx.VisibleDefinitions` 或 `definition.IsVisibleInCombatUi(ctx.Player)` 判断节点是否可见
- 带 `SecondaryResourceCombatUiChangedHandler` 的 `RegisterCombatUi(...)` 重载会在战斗 UI 刷新后收到正式的
  UI 变更通知。用 `ctx.Definition`、`ctx.OldAmount`、`ctx.NewAmount`、`ctx.Delta`、`ctx.Reason` 和
  `ctx.Source` 判断是否播放粒子、脉冲等纯表现反馈
- 已注册的界面回调彼此隔离；某个回调失败时只记录一次警告，不会中止其余界面刷新
- `NSecondaryResourceCardCostUi` 是用于 `RegisterCardUi(...)` 的单条目卡牌费用节点；每个节点可绑定一种资源或一个具名条目，并由注册方明确指定位置
- 卡牌费用节点明确放在游戏的辉星费用槽时，应设置 `SecondaryResourceCardCostUiStyle.ReserveVanillaStarCostSlot = true`，让带附魔的卡牌沿用辉星费用卡牌的附魔标签布局。自定义聚合费用界面占用该槽位时，可在卡牌界面更新器中调用 `SecondaryResourceCardUiLayout.ReserveVanillaStarCostSlot(ctx.Parent)`
- 内置 `NSecondaryResourceIcon` / `NSecondaryResourceCounter` 的悬浮提示始终使用资源标题和说明。需要自定义位置时，传入带有 `HoverTip = SecondaryResourceHoverTipStyle.Default with { ResolveGlobalPosition = ... }` 的 `SecondaryResourceIconStyle`。悬浮提示的标题和说明会收到 `Amount`、`HasMaxAmount` 和 `MaxAmount` 这些 `LocString` 变量，由本地化文本决定如何显示动态数量

```csharp
resources.RegisterCombatUi(
    "charge_feedback",
    parent => new MyChargeFeedbackNode(),
    update: ctx => ctx.Node.Refresh(ctx.Player, ctx.VisibleDefinitions),
    changed: ctx =>
    {
        if (ctx.Definition.Id == charge.Id && ctx.Delta > 0)
            ctx.Node.PlayGainFeedback(ctx.Delta, ctx.Source);
    });
```

对于文本表现：

- `SecondaryResourceText.GetIconTag(...)` 返回富文本 `[img]...[/img]` 图标标签
- `SecondaryResourceVars.For(...)` 和 `SecondaryResourceVars.ForLocal(...)` 用于创建 SmartFormat 动态变量
- `{secondaryResource:secondaryResourceIcons(charge,1)}` 使用已注册资源 ID 或无歧义的模组内 ID 渲染固定数量
- `{Cost:secondaryResourceIcons(charge)}` 使用已注册资源 ID 或无歧义的模组内 ID 渲染动态变量
- 标题和说明来自资源定义中的本地化表与键

:::
