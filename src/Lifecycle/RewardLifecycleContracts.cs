#if !STS2_AT_LEAST_0_104_0
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.CombatState;
#else
using CombatStateCompat = MegaCrit.Sts2.Core.Combat.ICombatState;
#endif
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rewards;
using MegaCrit.Sts2.Core.Runs;

namespace STS2RitsuLib
{
    /// <summary>
    ///     Player gold increased.
    ///     玩家金币增加。
    /// </summary>
    /// <param name="RunState">
    ///     Current run state.
    ///     当前跑局状态。
    /// </param>
    /// <param name="Player">
    ///     Player that gained gold.
    ///     获得金币的玩家。
    /// </param>
    /// <param name="GoldTotal">
    ///     New gold total after the change.
    ///     变更后的新金币总数。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct GoldGainedEvent(
        IRunState RunState,
        Player Player,
        int GoldTotal,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Player gold decreased.
    ///     玩家金币减少。
    /// </summary>
    /// <param name="Player">
    ///     Player that lost gold.
    ///     失去金币的玩家。
    /// </param>
    /// <param name="Amount">
    ///     Amount lost.
    ///     失去的数量。
    /// </param>
    /// <param name="LossType">
    ///     Reason category.
    ///     原因类别。
    /// </param>
    /// <param name="GoldTotal">
    ///     New gold total after the change.
    ///     变更后的新金币总数。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct GoldLostEvent(
        Player Player,
        decimal Amount,
        GoldLossType LossType,
        int GoldTotal,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Potion added to inventory.
    ///     药水已加入库存。
    /// </summary>
    /// <param name="RunState">
    ///     Current run state.
    ///     当前跑局状态。
    /// </param>
    /// <param name="CombatState">
    ///     Combat state when in combat.
    ///     处于战斗中时的战斗状态。
    /// </param>
    /// <param name="Potion">
    ///     Potion model.
    ///     药水模型。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct PotionProcuredEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        PotionModel Potion,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Potion removed from inventory.
    ///     药水已从库存移除。
    /// </summary>
    /// <param name="RunState">
    ///     Current run state.
    ///     当前跑局状态。
    /// </param>
    /// <param name="CombatState">
    ///     Combat state when in combat.
    ///     处于战斗中时的战斗状态。
    /// </param>
    /// <param name="Potion">
    ///     Potion model.
    ///     药水模型。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct PotionDiscardedEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        PotionModel Potion,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Relic added to the player.
    ///     遗物已添加到玩家。
    /// </summary>
    /// <param name="Player">
    ///     Receiving player.
    ///     接收遗物的玩家。
    /// </param>
    /// <param name="Relic">
    ///     Relic model.
    ///     RelicModel。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct RelicObtainedEvent(
        Player Player,
        RelicModel Relic,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Relic removed from the player.
    ///     遗物已从玩家移除。
    /// </summary>
    /// <param name="Player">
    ///     Affected player.
    ///     受影响的玩家。
    /// </param>
    /// <param name="Relic">
    ///     Relic model.
    ///     RelicModel。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct RelicRemovedEvent(
        Player Player,
        RelicModel Relic,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A reward option was taken.
    ///     一个奖励选项已被领取。
    /// </summary>
    /// <param name="RunState">
    ///     Current run state.
    ///     当前跑局状态。
    /// </param>
    /// <param name="Player">
    ///     Player taking the reward.
    ///     领取奖励的玩家。
    /// </param>
    /// <param name="Reward">
    ///     Reward that was selected.
    ///     被选择的奖励。
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     When the event was raised.
    ///     事件触发的时间。
    /// </param>
    public readonly record struct RewardTakenEvent(
        IRunState RunState,
        Player Player,
        Reward Reward,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
