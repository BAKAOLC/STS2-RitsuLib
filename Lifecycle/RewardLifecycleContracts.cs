#if STS2_V_0_103_2
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
    /// </summary>
    /// <param name="RunState">Current run state.</param>
    /// <param name="Player">Player that gained gold.</param>
    /// <param name="GoldTotal">New gold total after the change.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct GoldGainedEvent(
        IRunState RunState,
        Player Player,
        int GoldTotal,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Player gold decreased.
    /// </summary>
    /// <param name="Player">Player that lost gold.</param>
    /// <param name="Amount">Amount lost.</param>
    /// <param name="LossType">Reason category.</param>
    /// <param name="GoldTotal">New gold total after the change.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct GoldLostEvent(
        Player Player,
        decimal Amount,
        GoldLossType LossType,
        int GoldTotal,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Potion added to inventory.
    /// </summary>
    /// <param name="RunState">Current run state.</param>
    /// <param name="CombatState">Combat state when in combat.</param>
    /// <param name="Potion">Potion model.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct PotionProcuredEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        PotionModel Potion,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Potion removed from inventory.
    /// </summary>
    /// <param name="RunState">Current run state.</param>
    /// <param name="CombatState">Combat state when in combat.</param>
    /// <param name="Potion">Potion model.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct PotionDiscardedEvent(
        IRunState RunState,
        CombatStateCompat? CombatState,
        PotionModel Potion,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Relic added to the player.
    /// </summary>
    /// <param name="Player">Receiving player.</param>
    /// <param name="Relic">Relic model.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct RelicObtainedEvent(
        Player Player,
        RelicModel Relic,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     Relic removed from the player.
    /// </summary>
    /// <param name="Player">Affected player.</param>
    /// <param name="Relic">Relic model.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct RelicRemovedEvent(
        Player Player,
        RelicModel Relic,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;

    /// <summary>
    ///     A reward option was taken.
    /// </summary>
    /// <param name="RunState">Current run state.</param>
    /// <param name="Player">Player taking the reward.</param>
    /// <param name="Reward">Reward that was selected.</param>
    /// <param name="OccurredAtUtc">When the event was raised.</param>
    public readonly record struct RewardTakenEvent(
        IRunState RunState,
        Player Player,
        Reward Reward,
        DateTimeOffset OccurredAtUtc
    ) : IFrameworkLifecycleEvent;
}
