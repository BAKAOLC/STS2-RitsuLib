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
    ///     <para xml:lang="en">A player's Gold total increased.</para>
    ///     <para xml:lang="zh-CN">一名玩家的金币总数已增加。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Current run state.</para>
    ///     <para xml:lang="zh-CN">当前局内状态。</para>
    /// </param>
    /// <param name="Player">
    ///     <para xml:lang="en">Player that gained gold.</para>
    ///     <para xml:lang="zh-CN">获得金币的玩家。</para>
    /// </param>
    /// <param name="GoldTotal">
    ///     <para xml:lang="en">New gold total after the change.</para>
    ///     <para xml:lang="zh-CN">变更后的新金币总数。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct GoldGainedEvent(
        IRunState RunState,
        Player Player,
        int GoldTotal,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player's Gold total decreased.</para>
    ///     <para xml:lang="zh-CN">一名玩家的金币总数已减少。</para>
    /// </summary>
    /// <param name="Player">
    ///     <para xml:lang="en">Player that lost gold.</para>
    ///     <para xml:lang="zh-CN">失去金币的玩家。</para>
    /// </param>
    /// <param name="Amount">
    ///     <para xml:lang="en">Amount lost.</para>
    ///     <para xml:lang="zh-CN">失去的数量。</para>
    /// </param>
    /// <param name="LossType">
    ///     <para xml:lang="en">Reason category.</para>
    ///     <para xml:lang="zh-CN">原因类别。</para>
    /// </param>
    /// <param name="GoldTotal">
    ///     <para xml:lang="en">New gold total after the change.</para>
    ///     <para xml:lang="zh-CN">变更后的新金币总数。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct GoldLostEvent(
        Player Player,
        decimal Amount,
        GoldLossType LossType,
        int GoldTotal,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A potion was added to a player's inventory.</para>
    ///     <para xml:lang="zh-CN">一瓶药水已加入玩家的药水栏。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Current run state.</para>
    ///     <para xml:lang="zh-CN">当前局内状态。</para>
    /// </param>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Combat state when in combat.</para>
    ///     <para xml:lang="zh-CN">处于战斗中时的战斗状态。</para>
    /// </param>
    /// <param name="Potion">
    ///     <para xml:lang="en">Potion model.</para>
    ///     <para xml:lang="zh-CN">药水模型。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct PotionProcuredEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        PotionModel Potion,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A potion was removed from a player's inventory.</para>
    ///     <para xml:lang="zh-CN">一瓶药水已从玩家的药水栏移除。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Current run state.</para>
    ///     <para xml:lang="zh-CN">当前局内状态。</para>
    /// </param>
    /// <param name="CombatState">
    ///     <para xml:lang="en">Combat state when in combat.</para>
    ///     <para xml:lang="zh-CN">处于战斗中时的战斗状态。</para>
    /// </param>
    /// <param name="Potion">
    ///     <para xml:lang="en">Potion model.</para>
    ///     <para xml:lang="zh-CN">药水模型。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct PotionDiscardedEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        PotionModel Potion,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player obtained a relic.</para>
    ///     <para xml:lang="zh-CN">一名玩家已获得遗物。</para>
    /// </summary>
    /// <param name="Player">
    ///     <para xml:lang="en">Receiving player.</para>
    ///     <para xml:lang="zh-CN">接收遗物的玩家。</para>
    /// </param>
    /// <param name="Relic">
    ///     <para xml:lang="en">Relic that was obtained.</para>
    ///     <para xml:lang="zh-CN">获得的遗物。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct RelicObtainedEvent(
        Player Player,
        RelicModel Relic,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A relic was removed from a player.</para>
    ///     <para xml:lang="zh-CN">一名玩家的遗物已被移除。</para>
    /// </summary>
    /// <param name="Player">
    ///     <para xml:lang="en">Affected player.</para>
    ///     <para xml:lang="zh-CN">受影响的玩家。</para>
    /// </param>
    /// <param name="Relic">
    ///     <para xml:lang="en">Relic that was removed.</para>
    ///     <para xml:lang="zh-CN">被移除的遗物。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct RelicRemovedEvent(
        Player Player,
        RelicModel Relic,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     <para xml:lang="en">A player claimed a reward.</para>
    ///     <para xml:lang="zh-CN">一名玩家已领取奖励。</para>
    /// </summary>
    /// <param name="RunState">
    ///     <para xml:lang="en">Current run state.</para>
    ///     <para xml:lang="zh-CN">当前局内状态。</para>
    /// </param>
    /// <param name="Player">
    ///     <para xml:lang="en">Player taking the reward.</para>
    ///     <para xml:lang="zh-CN">领取奖励的玩家。</para>
    /// </param>
    /// <param name="Reward">
    ///     <para xml:lang="en">Reward that was selected.</para>
    ///     <para xml:lang="zh-CN">被选择的奖励。</para>
    /// </param>
    /// <param name="OccurredAtUtc">
    ///     <para xml:lang="en">When the event was raised.</para>
    ///     <para xml:lang="zh-CN">事件触发的时间。</para>
    /// </param>
    public readonly record struct RewardTakenEvent(
        IRunState RunState,
        Player Player,
        Reward Reward,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
